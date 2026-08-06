import { auth } from "@clerk/nextjs/server";

import Unauthorized from "@/components/Unauthorized";
import { request } from "@/services/fetch";
import type { APIResponse } from "@/types/reusable/api";
import type { User } from "./features/users/types/User";

interface SuperAdminGuardProps {
  children: React.ReactNode;
}

export async function SuperAdminGuard({ children }: SuperAdminGuardProps) {
  const { userId, getToken } = await auth();

  if (!userId) {
    return <Unauthorized />;
  }

  let response;
  try {
    const token = await getToken();
    response = await request.get<APIResponse<User>>(`/users/auth-user/${userId}`, {
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    });
  } catch (error) {
    console.error("Error validating super admin access:", error);
    return <Unauthorized />;
  }

  if (!response.succeeded || !response.data) {
    return <Unauthorized />;
  }

  // Check if user has super_admin role
  if (response.data.role.key !== "super_admin") {
    return <Unauthorized />;
  }

  return children;
}
