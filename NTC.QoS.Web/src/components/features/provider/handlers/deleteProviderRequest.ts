import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Provider } from "../types/Provider";

const endpoint = "/providers/:id";
export const deleteProvidersRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Provider>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Provider>>(finalUrl);
  },
  invalidateQueries: "get-providers",
};
