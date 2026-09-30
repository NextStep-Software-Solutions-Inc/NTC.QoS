export type Permission = {
  id: string;
  name: string;
  key: string;
  category?: string;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
};

export type PermissionDetails = Permission;
