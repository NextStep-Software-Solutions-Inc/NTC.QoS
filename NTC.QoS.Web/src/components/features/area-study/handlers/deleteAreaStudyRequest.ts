import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/area-studies/:id";

export const deleteAreaStudyRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<boolean>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl);
  },
};
