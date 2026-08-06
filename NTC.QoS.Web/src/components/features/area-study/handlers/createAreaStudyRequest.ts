import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type AreaStudyDetails } from "../types/AreaStudy";

const endpoint = "/area-studies";

type CreateAreaStudyPayload = { name: string; description?: string };

export const createAreaStudyRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AreaStudyDetails>,
  undefined,
  CreateAreaStudyPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<AreaStudyDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
