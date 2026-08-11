import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/user-regions/:id";

export const removeUserRegionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<{ message: string }>,
  undefined,
  undefined
> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.delete<APIResponse<{ message: string }>>(finalUrl, signal ? { signal } : undefined);
  },
};
