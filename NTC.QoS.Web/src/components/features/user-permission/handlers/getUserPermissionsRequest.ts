import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type UserPermission } from "../types/UserPermission";

const endpoint = "/user-permissions";

type GetUserPermissionsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  userId?: string;
  permissionId?: string;
};

export const getUserPermissionsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<UserPermission>,
  GetUserPermissionsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-user-permissions", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<UserPermission>>(finalUrl, { signal });
  },
};
