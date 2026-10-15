import { request } from "@/services/fetch";
import { type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type UpdateBarangayCoverageDto } from "../types/BarangayCoverage";

// Changed from /barangay-coverages/barangays/:barangayId to /barangay-coverages/:id
const endpoint = "/barangay-coverages/:id";

// Update returns 204 No Content on success
export const updateBarangayCoverageRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  void,
  undefined,
  UpdateBarangayCoverageDto
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload }) => {
    // PUT instead of PATCH, returns void (204 No Content)
    await request.put<void>(finalUrl, payload);
  },
};
