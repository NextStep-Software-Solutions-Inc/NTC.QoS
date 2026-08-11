import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Generation } from "../types/Generation";

const endpoint = "/generations";

type GetGenerationsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getGenerationsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Generation>,
  GetGenerationsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-generations", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Generation>>(finalUrl, { signal });
  },
};
