import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Barangay, type UpdateBarangayPayload } from "../types/Barangay";

const endpoint = "/barangays/:id";

export const editBarangayRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Barangay>,
  undefined,
  UpdateBarangayPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    if (payload.name) formData.append("name", payload.name);
    if (payload.municipalityId) formData.append("municipalityId", payload.municipalityId);
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return await request.patch<APIResponse<Barangay>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
