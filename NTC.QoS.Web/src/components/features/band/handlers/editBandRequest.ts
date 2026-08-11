import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type BandDetails } from "../types/Band";

const endpoint = "/bands/:id";

type EditBandPayload = { name?: string; description?: string };

export const editBandRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BandDetails>,
  undefined,
  EditBandPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<BandDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
