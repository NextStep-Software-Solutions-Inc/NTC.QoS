import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type AreaStudy } from "../types/AreaStudy";

const endpoint = "/area-studies";
type QueryParams = { pageSize?: number; pageNumber?: number; search?: string };

export const getAreaStudiesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<AreaStudy>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-area-studies", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<AreaStudy>>(finalUrl, { signal });
  },
};
