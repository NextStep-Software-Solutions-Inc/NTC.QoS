import { type CreateProvincePayload, type Province } from "@/components/features/province/types/Province";
import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/provinces" as const;

export const createProvinceRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Province>,
  undefined,
  CreateProvincePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    formData.append("name", payload.name);
    formData.append("regionId", payload.regionId);
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return await request.post<APIResponse<Province>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
