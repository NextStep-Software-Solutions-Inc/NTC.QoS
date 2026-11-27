import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/signal-levels/:signalLevelId/entries/:id";

export const deleteSignalLevelEntryRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<boolean>> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl, signal ? { signal } : undefined);
  },
};
