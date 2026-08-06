import { request } from "@/services/fetch";
import { type InfiniteQueryBlueprintFromTemplate, type PaginationResponse } from "@/types/reusable/api";
import { type CellViewport } from "../types/Cell";

const endpoint = "/v2/cells/viewport/public";

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
  generationIds?: string[];
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

export const getPublicCellsInfiniteRequest: InfiniteQueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<CellViewport>,
  QueryParams
> = {
  endpoint,
  key: ({ pathParams, queryParams }) => ["get-cells-infinite", endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<CellViewport>> => {
    return await request.get<PaginationResponse<CellViewport>>(finalUrl, { signal });
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
