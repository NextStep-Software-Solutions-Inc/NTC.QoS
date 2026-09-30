import { request } from "@/services/fetch";
import type { PaginationResponse, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import type { Role } from "../types/Role";

const endpoint = "/roles" as const;

type GetRolesQueryParams = {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  scope?: string;
  regionId?: string;
};

export const getAllRolesRequest: QueryBlueprintFromTemplate<typeof endpoint, PaginationResponse<Role>, GetRolesQueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-all-roles", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<PaginationResponse<Role>>(finalUrl);
  },
};
