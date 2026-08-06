import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type NAPCount } from "./../types/NAPCount";

const endpoint = "/dashboard/NAPs-count";
type QueryParams = {
  startDate?: string; // Optional, if you want to filter by start date
  endDate?: string; // Optional, if you want to filter by end date
  daysInPast?: number; // Optional, if you want to filter by days in the past
};

export const getNAPCountsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<NAPCount>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-nap-counts", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<NAPCount>>(finalUrl, { signal });
  },
};
