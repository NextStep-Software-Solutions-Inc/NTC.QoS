import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Technology } from "../types/Technology";

const endpoint = "/technologies";

type GetTechnologiesQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  type?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getTechnologiesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Technology>,
  GetTechnologiesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-technologies", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Technology>>(finalUrl, { signal });
  },
};
