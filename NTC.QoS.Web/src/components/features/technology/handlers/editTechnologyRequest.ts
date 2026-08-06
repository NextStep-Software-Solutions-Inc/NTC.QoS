import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type TechnologyDetails } from "../types/Technology";

const endpoint = "/technologies/:id";

type EditTechnologyPayload = { standard?: string; description?: string };

export const editTechnologyRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<TechnologyDetails>,
  undefined,
  EditTechnologyPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<TechnologyDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
