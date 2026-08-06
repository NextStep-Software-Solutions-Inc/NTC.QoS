import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Sector } from "../types/Sector";

const endpoint = "/sectors/:id";

export const deleteSectorRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Sector>
> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Sector>>(finalUrl);
  },
};
