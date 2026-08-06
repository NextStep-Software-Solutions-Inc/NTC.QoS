import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/cells/:id/technologies";

type UpdateCellTechnologiesPayload = {
  technologyIds: string[];
};

export const updateCellTechnologiesRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<unknown>,
  undefined,
  UpdateCellTechnologiesPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<unknown>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["cell-technologies"],
};
