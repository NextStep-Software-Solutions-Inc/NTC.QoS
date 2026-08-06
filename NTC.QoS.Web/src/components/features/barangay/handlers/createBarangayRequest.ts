import { type CreateBarangayPayload } from "@/components/features/barangay/types/Barangay";
import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Barangay } from "../types/Barangay";

const endpoint = "/barangays";

export const createBarangayRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Barangay>,
  undefined,
  CreateBarangayPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    formData.append("name", payload.name);
    formData.append("municipalityId", payload.municipalityId);
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return await request.post<APIResponse<Barangay>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
