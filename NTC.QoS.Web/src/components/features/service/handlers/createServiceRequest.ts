import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Service } from "../types/Service";

const endpoint = "/services";
type CreateServicePayload = { name: string; type: string };

export const createServiceRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Service>,
  undefined,
  CreateServicePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Service>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: "get-services",
};
