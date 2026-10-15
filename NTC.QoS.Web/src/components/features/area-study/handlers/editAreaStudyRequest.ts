import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type AreaStudyDetails } from "../types/AreaStudy";

const endpoint = "/area-studies/:id";

type UpdateAreaStudyPayload = { name?: string; description?: string };

export const editAreaStudyRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AreaStudyDetails>,
  undefined,
  UpdateAreaStudyPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<AreaStudyDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
