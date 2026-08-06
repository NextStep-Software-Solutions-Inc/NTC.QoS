import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type GenerationDetails } from "../types/Generation";

const endpoint = "/generations";

type CreateGenerationPayload = { name: string; description?: string };

export const createGenerationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<GenerationDetails>,
  undefined,
  CreateGenerationPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<GenerationDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
