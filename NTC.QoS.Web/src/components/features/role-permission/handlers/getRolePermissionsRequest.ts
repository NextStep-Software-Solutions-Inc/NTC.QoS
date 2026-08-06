import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type RolePermission } from "../types/RolePermission";

const endpoint = "/role-permissions";

type GetRolePermissionsQueryParams = {
  pageNumber?: number;
  pageSize?: number;
  roleIds?: string[];
  permissionIds?: string[];
};

export const getRolePermissionsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<RolePermission>,
  GetRolePermissionsQueryParams
> = {
  endpoint,
  key: ({ url, queryParams }) => ["get-role-permissions", url, endpoint, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<RolePermission>>(finalUrl, { signal });
  },
};
