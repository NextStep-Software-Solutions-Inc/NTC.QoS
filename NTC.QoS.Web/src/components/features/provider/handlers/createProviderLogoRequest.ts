import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ProviderLogo } from "../types/ProviderLogo";

const endpoint = "/providers/:id/logo";

export const createProviderLogoRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ProviderLogo>,
  undefined,
  FormData
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<ProviderLogo>>(finalUrl, null, { body: payload, headers: {} });
  },
};
