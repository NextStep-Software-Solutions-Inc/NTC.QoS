import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import {
  type SpeedtestAnalyticsApiResponse,
  type SpeedtestAnalyticsQueryParams,
} from "../types/analytics-api";

const endpoint = "/analytics/speedtest-analytics" as const;

/**
 * Handler for fetching speedtest analytics data
 */
export const speedtestAnalyticsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  SpeedtestAnalyticsApiResponse,
  SpeedtestAnalyticsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["speedtest-analytics", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<SpeedtestAnalyticsApiResponse>(finalUrl);
  },
  defaultOptions: {
    staleTime: 0, // Data is always considered stale, ensuring fresh fetches
    refetchOnMount: true, // Always refetch when component mounts
    refetchOnWindowFocus: false, // Don't refetch on window focus to reduce API calls
  },
};
