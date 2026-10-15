import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Study } from "../types/Study";

const endpoint = "/studies";

type CreateStudyPayload = { name: string; description?: string; areaStudyId: string; studyRadius: number };

export const createStudyRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Study>,
  undefined,
  CreateStudyPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Study>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
