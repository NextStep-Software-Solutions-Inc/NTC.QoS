import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Municipality } from "../types/Municipality";

const endpoint = "/municipalities/:id";

export const deleteMunicipalityRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Municipality>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Municipality>>(finalUrl);
  },
};
