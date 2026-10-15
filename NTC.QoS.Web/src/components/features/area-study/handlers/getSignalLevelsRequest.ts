import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type SignalLevel } from "../types/SignalLevel";

const endpoint = "/signal-levels";

type QueryParams = { pageSize?: number; pageNumber?: number };

export const getSignalLevelsRequest: QueryBlueprintFromTemplate<typeof endpoint, PaginationResponse<SignalLevel>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-signal-levels", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<SignalLevel>>(finalUrl, { signal });
  },
};
