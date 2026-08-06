import { request } from "@/services/fetch";
import { APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type BarangayCoverageLightDto, type BarangayCoverageFilterParams } from "../types/BarangayCoverage";

const endpoint = "/barangay-coverages";

export const getAllBarangayCoveragesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BarangayCoverageLightDto[]>,
  BarangayCoverageFilterParams
> = {
  endpoint,
  key: ({ queryParams }) => {
    // Include filter params in the query key for proper caching
    return [
      "barangay-coverages",
      queryParams?.regionIds,
      queryParams?.provinceIds,
      queryParams?.municipalityIds,
      queryParams?.barangayIds,
    ] as const;
  },
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<BarangayCoverageLightDto[]>>(finalUrl, { signal });
  },
};
