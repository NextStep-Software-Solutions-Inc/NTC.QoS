import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Region } from "../types/Region";

const endpoint = "/regions/:id";

type EditRegionPayload = { name?: string; code?: string; description?: string; area?: number; boundariesFile?: File };

export const editRegionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Region>,
  undefined,
  EditRegionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    if (payload.name) formData.append("name", payload.name);
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined && payload.area !== null) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return await request.patch<APIResponse<Region>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
