import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "../../../../types/reusable/api";
import { type CreateMunicipalityPayload, type Municipality } from "../types/Municipality";

const endpoint = "/municipalities";

export const createMunicipalityRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Municipality>,
  undefined,
  CreateMunicipalityPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) throw new Error("Payload is required");

    const formData = new FormData();
    formData.append("name", payload.name);
    formData.append("provinceId", payload.provinceId);
    formData.append("isCity", payload.isCity.toString());
    if (payload.code) formData.append("code", payload.code);
    if (payload.description) formData.append("description", payload.description);
    if (payload.area !== undefined) formData.append("area", payload.area.toString());
    if (payload.boundariesFile) formData.append("boundariesFile", payload.boundariesFile);

    return request.post<APIResponse<Municipality>>(finalUrl, formData, signal ? { signal } : undefined);
  },
};
