import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type SystemTypeDetails } from "../types/SystemType";

const endpoint = "/system-types";

type CreateSystemTypePayload = { name: string; technologyId: string; generationId: string };

export const createSystemTypeRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SystemTypeDetails>,
  undefined,
  CreateSystemTypePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<SystemTypeDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
