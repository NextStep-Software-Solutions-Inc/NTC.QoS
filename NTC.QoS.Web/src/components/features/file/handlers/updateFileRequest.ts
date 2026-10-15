import { type File } from "@/components/features/file/types/File";
import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

type UpdateFilePayload = { name: string };

const endpoint = "/files/:id" as const;

export const updateFileRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<File>,
  undefined,
  UpdateFilePayload
> = {
  endpoint,
  fetcher: async ({
    finalUrl,
    payload,
    signal,
  }: {
    finalUrl: string;
    payload?: UpdateFilePayload;
    signal?: AbortSignal;
  }) => {
    return await request.put<APIResponse<File>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
