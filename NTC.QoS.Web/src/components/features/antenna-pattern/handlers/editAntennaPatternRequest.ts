import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type AntennaPattern } from "../types/AntennaPattern";

const endpoint = "/antenna-patterns/:id";

type UpdateAntennaPatternPayload = { name?: string; url?: string };

export const editAntennaPatternRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AntennaPattern>,
  undefined,
  UpdateAntennaPatternPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<AntennaPattern>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
