import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type TechnologyDetails } from "../types/Technology";

const endpoint = "/technologies";

type CreateTechnologyPayload = { standard: string; description?: string; generationId: string };

export const createTechnologyRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<TechnologyDetails>,
  undefined,
  CreateTechnologyPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<TechnologyDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
