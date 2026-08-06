import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type SpeedTest } from "../types/SpeedTest";

const endpoint = "/speedtests";

type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  status?: string;
  barangayIds?: string[];
  municipalityIds?: string[];
  provinceIds?: string[];
  regionIds?: string[];
  serviceProviderIds?: string[];
  providerIds?: string[];
  serviceIds?: string[];
  cellIds?: string[];
  generationIds?: string[];
  signalQuality?: string;
  minDownloadSpeed?: number;
  maxDownloadSpeed?: number;
  minUploadSpeed?: number;
  maxUploadSpeed?: number;
  startDate?: string;
  endDate?: string;
  longitude?: number;
  latitude?: number;
  distance?: number;
  isNoBroadbandCoverage?: boolean;
};
export const getSpeedtestsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<SpeedTest>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["speedtests", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<SpeedTest>>(finalUrl, { signal });
  },
};
