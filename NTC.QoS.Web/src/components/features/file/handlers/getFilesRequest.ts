import { type File } from "@/components/features/file/types/File";
import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/files" as const;
type QueryParams = {
  types?: "Speedtest" | "NAP" | "Cell" | "Cellular" | "CellularCoverage" | "WirelessValidation";
  pageSize?: number;
  pageNumber?: number;
  regionIds?: string[];
};

export const getFilesRequest: QueryBlueprintFromTemplate<typeof endpoint, PaginationResponse<File>, QueryParams> = {
  endpoint,
  key: ({ url }) => [url] as const,
  fetcher: async ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.get<PaginationResponse<File>>(finalUrl, { signal });
  },
};
