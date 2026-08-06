import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type SystemTypeDetails } from "../types/SystemType";

const endpoint = "/system-types/:id";

type EditSystemTypePayload = { name?: string };

export const editSystemTypeRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SystemTypeDetails>,
  undefined,
  EditSystemTypePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<SystemTypeDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
