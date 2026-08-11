import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Province, type UpdateProvincePayload } from "../types/Province";

const endpoint = "/provinces/:id";

export const editProvinceRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Province>,
  undefined,
  UpdateProvincePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    if (payload.name) formData.append("name", payload.name);
    if (payload.regionId) formData.append("regionId", payload.regionId);
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return await request.patch<APIResponse<Province>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
