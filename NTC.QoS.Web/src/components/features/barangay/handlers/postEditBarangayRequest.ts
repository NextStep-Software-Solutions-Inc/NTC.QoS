import { type Barangay } from "@/components/features/barangay/types/Barangay";
import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/barangays/:id";

type EditBarangayPayload = { name: string; description: string };

export const editBarangayRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Barangay>,
  undefined,
  EditBarangayPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Barangay>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
