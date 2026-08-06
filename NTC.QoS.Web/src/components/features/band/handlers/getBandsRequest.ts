import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Band } from "../types/Band";

const endpoint = "/bands";

type GetBandsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  technologies?: string;
  generationIds?: string[];
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getBandsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Band>,
  GetBandsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-bands", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Band>>(finalUrl, { signal });
  },
};
