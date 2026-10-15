import { request } from "@/services/fetch";
import { type InfiniteQueryBlueprintFromTemplate, type PaginationResponse } from "@/types/reusable/api";
import { type SpeedTest } from "../types/SpeedTest";

const endpoint = "/v2/speedtests/viewport/public";

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
  minDownloadSpeed?: number;
  maxDownloadSpeed?: number;
  minUploadSpeed?: number;
  maxUploadSpeed?: number;
  startDate?: string;
  endDate?: string;
  // Point-based search (returns items near a specific coordinate)
  lat?: number;
  lng?: number;
  radius?: number; // Optional search radius in kilometers
  // Viewport-based filtering (spatial query)
  minLat?: number;
  maxLat?: number;
  minLng?: number;
  maxLng?: number;
  zoom?: number; // Current zoom level for level-of-detail optimization
  isNoBroadbandCoverage?: boolean;
};

export const getPublicSpeedtestsInfiniteRequest: InfiniteQueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<SpeedTest>,
  QueryParams
> = {
  endpoint,
  key: ({ pathParams, queryParams }) => ["get-speedtests-infinite", endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<SpeedTest>> => {
    return await request.get<PaginationResponse<SpeedTest>>(finalUrl, { signal });
  },
  initialPageParam: 1,
  getNextPageParam: (lastPage) => {
    // If loadMore is true, return the next page number
    if (lastPage.loadMore && lastPage.pageNumber < lastPage.totalPageNumber) {
      return lastPage.pageNumber + 1;
    }
    // No more pages to load
    return undefined;
  },
  defaultOptions: {
    staleTime: 1000 * 60 * 5, // 5 minutes
    gcTime: 1000 * 60 * 30, // 30 minutes (formerly cacheTime)
  },
};
