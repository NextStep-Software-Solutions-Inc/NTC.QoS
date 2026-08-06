import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type AntennaPattern } from "../types/AntennaPattern";

const endpoint = "/antenna-patterns";

type CreateAntennaPatternPayload = { name: string; url: string };

export const createAntennaPatternRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AntennaPattern>,
  undefined,
  CreateAntennaPatternPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<AntennaPattern>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
