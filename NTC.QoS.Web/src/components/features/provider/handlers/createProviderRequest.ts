import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Provider } from "../types/Provider";

const endpoint = "/providers";
type CreateProviderPayload = {
  name: string;
  description?: string;
  licenseNumber: string;
  color: string;
  logo?: string;
};

export const createProviderRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Provider>,
  undefined,
  CreateProviderPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Provider>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: "get-providers",
};
