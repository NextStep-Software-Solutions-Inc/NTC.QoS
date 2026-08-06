import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type FrequencyAllocation } from "../types/FrequencyAllocation";

const endpoint = "/frequency-allocations";

export type GetFrequencyAllocationsQueryParams = {
  pageNumber?: number;
  pageSize?: number;
  providerIds?: string[];
  isActive?: boolean;
  search?: string;
};

export const getFrequencyAllocationsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<FrequencyAllocation>,
  GetFrequencyAllocationsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-frequency-allocations", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<FrequencyAllocation>>(finalUrl, { signal });
  },
};
