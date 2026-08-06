import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type NAPAnalyticsResponse } from "../types/analytics";

const endpoint = "/analytics/nap-analytics";

type QueryParams = {
  barangayIds?: string[];
  municipalityIds?: string[];
  provinceIds?: string[];
  regionIds?: string[];
  serviceProviderIds?: string[];
  includeGeographicCoverage?: boolean;
};

export const getNAPAnalyticsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  NAPAnalyticsResponse,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["nap-analytics", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<NAPAnalyticsResponse> => {
    return await request.get<NAPAnalyticsResponse>(finalUrl, { signal });
  },
};
