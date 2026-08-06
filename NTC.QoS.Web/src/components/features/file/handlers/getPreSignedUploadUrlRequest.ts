import { type PreSignedUrl } from "@/components/features/file/types/PreSignedUrl";
import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/files/presigned-upload-url" as const;

type QueryParams = {
  fileName: string;
  fileType: string;
  regionId: string;
};

export const getPreSignedUploadUrlRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PreSignedUrl>,
  QueryParams
> = {
  endpoint,
  key: ({ url }) => ["presigned-upload-url", url],
  fetcher: async ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.get<APIResponse<PreSignedUrl>>(finalUrl, { signal });
  },
};
