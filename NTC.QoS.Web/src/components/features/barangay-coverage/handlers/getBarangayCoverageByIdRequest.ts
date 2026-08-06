import { request } from "@/services/fetch";
import { APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type BarangayCoverageDto } from "../types/BarangayCoverage";

// Changed from /barangay-coverages/barangays/:barangayId to /barangay-coverages/:id
const endpoint = "/barangay-coverages/:id";

// Get single coverage by coverage ID (includes barangayName and barangayCode)
export const getBarangayCoverageByIdRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BarangayCoverageDto>
> = {
  endpoint,
  key: ({ url }) => [url] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<BarangayCoverageDto>>(finalUrl, { signal });
  },
};
