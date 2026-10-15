import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Provider } from "../types/Provider";

const endpoint = "/providers/:id";

type EditProviderPayload = { name: string; licenseNumber: string; color: string; logo?: string };

export const editProviderRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Provider>,
  undefined,
  EditProviderPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Provider>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: "get-providers",
};
