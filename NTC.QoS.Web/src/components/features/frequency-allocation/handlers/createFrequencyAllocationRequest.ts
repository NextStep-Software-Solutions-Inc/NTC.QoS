import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CreateFrequencyAllocationPayload, type FrequencyAllocationDetails } from "../types/FrequencyAllocation";

const endpoint = "/frequency-allocations";

export const createFrequencyAllocationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<FrequencyAllocationDetails>,
  undefined,
  CreateFrequencyAllocationPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<FrequencyAllocationDetails>>(
      finalUrl,
      payload,
      signal ? { signal } : undefined,
    );
  },
  invalidateQueries: "get-frequency-allocations",
};
