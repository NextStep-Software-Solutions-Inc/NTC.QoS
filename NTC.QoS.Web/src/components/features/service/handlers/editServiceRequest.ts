import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Service } from "../types/Service";

const endpoint = "/services/:id";

type UpdateServicePayload = { name?: string; type?: string };

export const editServiceRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Service>,
  undefined,
  UpdateServicePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<Service>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: "get-services",
};
