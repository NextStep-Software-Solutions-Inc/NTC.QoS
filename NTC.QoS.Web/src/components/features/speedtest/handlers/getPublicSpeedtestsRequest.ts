import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type SpeedTest } from "../types/SpeedTest";

const endpoint = "/speedtests/public";

type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
  providerIds?: string;
  serviceIds?: string[];
  serviceProviderIds?: string[];
  generationIds?: string[];
  cellIds?: string[];
  signalQuality?: string;
  longitude?: number;
  latitude?: number;
  distance?: number;
  isNoBroadbandCoverage?: boolean;
};
export const getPublicSpeedtestsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SpeedTest[]>,
  QueryParams
> = {
  endpoint,
  key: ({ url }) => [url],
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<SpeedTest[]>>(finalUrl, { signal });
  },
};
