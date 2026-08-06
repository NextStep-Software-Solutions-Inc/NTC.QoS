import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Study } from "../types/Study";

const endpoint = "/studies/:id";
type EditStudyPayload = { name?: string; description?: string; studyRadius?: number };

export const editStudyRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Study>,
  undefined,
  EditStudyPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Study>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
