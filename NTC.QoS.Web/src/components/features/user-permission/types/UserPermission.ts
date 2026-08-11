import type { Permission } from "../../permission/types/Permission";
import type { User } from "../../users/types/User";

export type UserPermission = {
  id: string;
  user: User;
  permission: Permission;
  status: string;
  createdAt?: string;
  updatedAt?: string;
};

export type UserPermissionDetails = UserPermission;
