export type Role = {
  id: string;
  name: string;
  key: string;
  description?: string;
  isSystemRole?: boolean;
  scope?: "Platform" | "Regional";
  regionId?: string | null;
  permissions?: string[];
};

export type RoleDetails = Role & {};
