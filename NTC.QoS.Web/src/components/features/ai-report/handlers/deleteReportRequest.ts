import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { request } from "@/services/fetch";

const endpoint = "/genai/reports/:id" as const;

export const deleteReportRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<boolean>
> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl, signal ? { signal } : undefined);
  },
};
