import { type FileDetails } from "@/components/features/file/types/File";
import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/files/:id" as const;

export const getFileRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<FileDetails>, undefined> = {
  endpoint,
  key: ({ url }) => ["get-file", url],
  fetcher: async ({ finalUrl }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.get<APIResponse<FileDetails>>(finalUrl);
  },
};
