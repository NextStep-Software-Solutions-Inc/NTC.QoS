import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Study } from "../types/Study";

const endpoint = "/studies";

type GetStudiesQueryParams = { pageSize?: number; pageNumber?: number; search?: string; areaStudyId?: string };

export const getStudiesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Study>,
  GetStudiesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-studies", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Study>>(finalUrl, { signal });
  },
};
