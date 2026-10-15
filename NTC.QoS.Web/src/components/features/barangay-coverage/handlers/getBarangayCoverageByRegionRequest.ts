import { request } from "@/services/fetch";
import { APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type BarangayCoverageLightDto, type BarangayCoverageFilterParams } from "../types/BarangayCoverage";

// Changed from /barangay-coverages/regions/:regionId to /barangay-coverages with query params
const endpoint = "/barangay-coverages";

// Get all coverages with optional filters (returns light response without relationship data)
export const getBarangayCoveragesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BarangayCoverageLightDto[]>,
  BarangayCoverageFilterParams
> = {
  endpoint,
  key: ({ url, queryParams }) => ["barangay-coverages", url, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<BarangayCoverageLightDto[]>>(finalUrl, { signal });
  },
  defaultOptions: {
    staleTime: 5 * 60 * 1000, // 5 minutes - coverage data doesn't change frequently
    gcTime: 10 * 60 * 1000, // 10 minutes
  },
};
