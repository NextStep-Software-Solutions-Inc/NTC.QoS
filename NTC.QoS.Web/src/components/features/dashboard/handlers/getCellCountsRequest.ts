import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellCount } from "../types/CellCount";

const endpoint = "/dashboard/cell-counts";
type QueryParams = {
  startDate?: string; // Optional, if you want to filter by start date
  endDate?: string; // Optional, if you want to filter by end date
  daysInPast?: number; // Optional, if you want to filter by days in the past
};

export const getCellCountsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<CellCount>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-cell-counts", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<CellCount>>(finalUrl, { signal });
  },
};
