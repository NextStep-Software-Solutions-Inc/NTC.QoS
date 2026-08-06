import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type FrequencyAllocationDetails } from "../types/FrequencyAllocation";

const endpoint = "/frequency-allocations/:id";

export const getFrequencyAllocationRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<FrequencyAllocationDetails>
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-frequency-allocation", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<FrequencyAllocationDetails>>(finalUrl, { signal });
  },
};
