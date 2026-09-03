import { type File } from "@/components/features/file/types/File";
import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/files" as const;

export const uploadFileRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<File>,
  undefined,
  FormData
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<File>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

