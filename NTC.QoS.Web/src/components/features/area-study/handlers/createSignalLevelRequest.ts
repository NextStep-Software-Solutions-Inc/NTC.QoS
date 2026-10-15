import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CreateSignalLevelPayload, type SignalLevel } from "../types/SignalLevel";

const endpoint = "/signal-levels";

export const createSignalLevelRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SignalLevel>,
  undefined,
  CreateSignalLevelPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<SignalLevel>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
