import { type PreSignedUrl } from "@/components/features/file/types/PreSignedUrl";
import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/files/:id/presigned-download-url" as const;

export const getPreSignedDownloadUrlRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PreSignedUrl>,
  undefined
> = {
  endpoint,
  key: ({ url }) => ["presigned-download-url", url],
  fetcher: async ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.get<APIResponse<PreSignedUrl>>(finalUrl, { signal });
  },
};
