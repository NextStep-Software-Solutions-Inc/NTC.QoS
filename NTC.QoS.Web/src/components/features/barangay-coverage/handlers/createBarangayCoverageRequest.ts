import { request } from "@/services/fetch";
import { APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CreateBarangayCoverageDto } from "../types/BarangayCoverage";

const endpoint = "/barangay-coverages";

// Create returns the GUID of the newly created coverage record
export const createBarangayCoverageRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<string>,
  undefined,
  CreateBarangayCoverageDto
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload }) => {
    return await request.post<APIResponse<string>>(finalUrl, payload);
  },
};
