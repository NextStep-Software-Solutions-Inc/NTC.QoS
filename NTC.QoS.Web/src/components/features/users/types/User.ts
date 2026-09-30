import type { Role } from "@/components/features/role/types/Role";

export type User = {
  id: string;
  authUserId: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  role: Role;
};

export type CurrentUser = User & {
  permissions: string[];
  accessibleRegionIds: string[];
  isSuperAdmin: boolean;
  isPlatformUser: boolean;
};

export type UserDetails = User & {};
