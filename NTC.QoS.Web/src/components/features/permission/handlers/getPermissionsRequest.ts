import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Permission } from "../types/Permission";

const endpoint = "/permissions";

type GetPermissionsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getPermissionsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Permission>,
  GetPermissionsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-permissions", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Permission>>(finalUrl, { signal });
  },
};
