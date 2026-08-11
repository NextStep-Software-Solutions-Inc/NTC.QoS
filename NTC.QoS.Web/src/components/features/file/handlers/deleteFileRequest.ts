import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/files/:id" as const;

export const deleteFileRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<boolean>,
  undefined,
  undefined
> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl, signal ? { signal } : undefined);
  },
};
