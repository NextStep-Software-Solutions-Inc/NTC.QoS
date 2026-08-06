import { type Barangay } from "@/components/features/barangay/types/Barangay";
import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/barangays/:id";

export const deleteBarangayRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Barangay>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Barangay>>(finalUrl);
  },
};
