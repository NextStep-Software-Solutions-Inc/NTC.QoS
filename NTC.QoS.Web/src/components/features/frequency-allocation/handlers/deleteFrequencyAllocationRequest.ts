import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/frequency-allocations/:id";

export const deleteFrequencyAllocationRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<boolean>> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl, signal ? { signal } : undefined);
  },
  invalidateQueries: "get-frequency-allocations",
};
