import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { auth } from "@clerk/nextjs/server";

import type { Region } from "@/components/features/region/types/Region";
import type { UserRegion } from "@/components/features/user-region/types/UserRegion";
import type { User } from "@/components/features/users/types/User";
import Timeout from "@/components/Timeout";
import Unauthorized from "@/components/Unauthorized";
import { tryCatch } from "@/lib/try-catch";
import { request } from "@/services/fetch";
import type { APIResponse, PaginationResponse } from "@/types/reusable/api";
import ErrorComponent from "./error";

export const metadata: Metadata = {
  title: "Region Dashboard",
  description: "NTC QoS Broadband Mapping - Region Dashboard",
};

type LayoutProps = { children: React.ReactNode; params: Promise<{ region: string }> };

export default async function RegionLayout({ children, params: paramsAsync }: LayoutProps) {
  const params = await paramsAsync;

  const { userId, getToken } = await auth();
  const token = await getToken();
  // Level 0 : Check for user authentication
  if (!userId || !token) return <Unauthorized />;

  // Level 1 : Check for region existence
  const [regionResponse, regionError] = await tryCatch(async () => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout
    return await request
      .get<
        APIResponse<Region>
      >(`/regions/${params.region}`, { headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) }, signal: controller.signal })
      .finally(() => clearTimeout(timeoutId));
  });

  if (regionError instanceof Error && regionError.name === "AbortError") return <Timeout />;
  if (regionError) return <ErrorComponent error={regionError} />;
  if (!regionResponse.succeeded) notFound();

  // Level 2 : Check for user role
  const [userResponse, userError] = await tryCatch(async () => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout
    return await request
      .get<
        APIResponse<User>
      >(`/users/auth-user/${userId}`, { headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) }, signal: controller.signal })
      .finally(() => clearTimeout(timeoutId));
  });

  if (userError instanceof Error && userError.name === "AbortError") return <Timeout />;
  if (userError) return <ErrorComponent error={userError} />;
  if (userResponse.data.role.key === "super_admin") return children;

  // Level 3 : Check for user-region access
  const [userRegionResponse, userRegionError] = await tryCatch(async () => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout

    return await request
      .get<
        PaginationResponse<UserRegion>
      >(`/user-regions?authUserIds=${userId}&statuses=active`, { headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) }, signal: controller.signal })
      .finally(() => clearTimeout(timeoutId));
  });

  if (userRegionError instanceof Error && userRegionError.name === "AbortError") return <Timeout />;
  if (userRegionError) return <Unauthorized />;
  if (!userRegionResponse.succeeded) return <Unauthorized />;

  return children;
}
