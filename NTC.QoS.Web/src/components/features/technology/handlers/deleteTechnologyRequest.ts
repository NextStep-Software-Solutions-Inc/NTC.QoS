import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/technologies/:id";

export const deleteTechnologyRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<boolean>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl);
  },
};
