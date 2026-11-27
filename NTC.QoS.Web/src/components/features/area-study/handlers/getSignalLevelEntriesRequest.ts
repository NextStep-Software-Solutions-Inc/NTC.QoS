import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type SignalLevelEntry } from "../types/SignalLevelEntry";

const endpoint = "/signal-levels/:signalLevelId/entries";

type QueryParams = { pageSize?: number; pageNumber?: number };

export const getSignalLevelEntriesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<SignalLevelEntry>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-signal-level-entries", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<SignalLevelEntry>>(finalUrl, { signal });
  },
};
