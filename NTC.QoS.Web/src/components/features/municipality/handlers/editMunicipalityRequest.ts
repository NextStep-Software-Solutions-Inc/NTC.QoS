import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Municipality, type UpdateMunicipalityPayload } from "../types/Municipality";

const endpoint = "/municipalities/:id";

export const editMunicipalityRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Municipality>,
  undefined,
  UpdateMunicipalityPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    if (payload.name) formData.append("name", payload.name);
    if (payload.provinceId) formData.append("provinceId", payload.provinceId);
    if (payload.isCity !== undefined) formData.append("isCity", payload.isCity.toString());
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return await request.patch<APIResponse<Municipality>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
