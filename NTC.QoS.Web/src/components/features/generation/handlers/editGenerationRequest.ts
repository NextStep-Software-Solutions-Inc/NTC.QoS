import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type GenerationDetails } from "../types/Generation";

const endpoint = "/generations/:id";

type EditGenerationPayload = { name?: string; description?: string };

export const editGenerationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<GenerationDetails>,
  undefined,
  EditGenerationPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<GenerationDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
