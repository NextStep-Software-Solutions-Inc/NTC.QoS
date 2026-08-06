import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type EditFrequencyAllocationPayload, type FrequencyAllocationDetails } from "../types/FrequencyAllocation";

const endpoint = "/frequency-allocations/:id";

export const editFrequencyAllocationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<FrequencyAllocationDetails>,
  undefined,
  EditFrequencyAllocationPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<FrequencyAllocationDetails>>(
      finalUrl,
      payload,
      signal ? { signal } : undefined,
    );
  },
  invalidateQueries: ["get-frequency-allocations", "get-frequency-allocation"],
};
