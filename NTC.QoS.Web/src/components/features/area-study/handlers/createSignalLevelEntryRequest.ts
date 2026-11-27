import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CreateSignalLevelEntryPayload, type SignalLevelEntry } from "../types/SignalLevelEntry";

const endpoint = "/signal-levels/:signalLevelId/entries";

export const createSignalLevelEntryRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SignalLevelEntry>,
  undefined,
  CreateSignalLevelEntryPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<SignalLevelEntry>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
