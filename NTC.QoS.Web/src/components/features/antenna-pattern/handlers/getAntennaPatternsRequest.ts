import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type AntennaPattern } from "../types/AntennaPattern";

const endpoint = "/antenna-patterns";

type GetAntennaPatternsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getAntennaPatternsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AntennaPattern[]>,
  GetAntennaPatternsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-antenna-patterns", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<AntennaPattern[]>>(finalUrl, { signal });
  },
};
