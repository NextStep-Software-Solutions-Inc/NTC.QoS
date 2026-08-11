import { request } from "@/services/fetch";
import type { PaginationResponse, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import type { UserRegion } from "../types/UserRegion";

const endpoint = "/user-regions";
type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  userIds?: string[];
  authUserIds?: string[];
  regionIds?: string[];
  statuses?: string[];
};

export const getUserRegionsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<UserRegion>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-user-regions", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<UserRegion>>(finalUrl, { signal });
  },
};
