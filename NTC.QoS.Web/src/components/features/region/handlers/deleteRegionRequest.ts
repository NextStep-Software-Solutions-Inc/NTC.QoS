import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Region } from "../types/Region";

const endpoint = "/regions/:id";

export const deleteRegionRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Region>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Region>>(finalUrl);
  },
};
