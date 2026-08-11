import { request } from "@/services/fetch";
import { type InfiniteQueryBlueprintFromTemplate, type PaginationResponse } from "@/types/reusable/api";
import { type NAPViewport } from "../types/NAP";

const endpoint = "/v2/NAPs/viewport/public";

type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  regionIds?: string[];
  provinceIds?: string[];
  providerIds?: string;
  municipalityIds?: string[];
  barangayIds?: string[];
  serviceProviderIds?: string[];
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
};

export const getPublicNAPsInfiniteRequest: InfiniteQueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<NAPViewport>,
  QueryParams
> = {
  endpoint,
  key: ({ pathParams, queryParams }) => ["get-NAPs-infinite", endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<NAPViewport>> => {
    return await request.get<PaginationResponse<NAPViewport>>(finalUrl, { signal });
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
