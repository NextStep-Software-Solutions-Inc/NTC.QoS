import type { Permission } from "../../permission/types/Permission";
import type { Role } from "../../role/types/Role";

export type RolePermission = { id: string; role: Role; permission: Permission };

export type CreateRolePermissionPayload = { roleId: string; permissionId: string };

export type RolePermissionDetails = RolePermission;
