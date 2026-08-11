import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type BandDetails } from "../types/Band";

const endpoint = "/bands";

type CreateBandPayload = { name: string; description?: string };

export const createBandRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BandDetails>,
  undefined,
  CreateBandPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<BandDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
