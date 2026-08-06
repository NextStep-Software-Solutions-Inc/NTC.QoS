import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Provider } from "../types/Provider";

const endpoint = "/providers/:id/restore";

export const restoreProviderRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Provider>> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.post<APIResponse<Provider>>(finalUrl, undefined, signal ? { signal } : undefined);
  },
  invalidateQueries: "get-providers",
};
