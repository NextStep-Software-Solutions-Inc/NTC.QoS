import { request } from "@/services/fetch";
import { type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/barangay-coverages/:id";

// Delete returns 204 No Content on success
export const deleteBarangayCoverageRequest: MutationBlueprintFromTemplate<typeof endpoint, void> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    await request.delete<void>(finalUrl);
  },
};
