import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type SignalLevelEntry, type UpdateSignalLevelEntryPayload } from "../types/SignalLevelEntry";

const endpoint = "/signal-levels/:signalLevelId/entries/:id";

export const editSignalLevelEntryRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SignalLevelEntry>,
  undefined,
  UpdateSignalLevelEntryPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<SignalLevelEntry>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
