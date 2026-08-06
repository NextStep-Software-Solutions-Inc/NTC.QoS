import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceProvider } from "../types/serviceProvider";

const endpoint = "/service-providers/:id";

type UpdateServiceProviderPayload = { providerId?: string; serviceId?: string; mnc?: string; mcc?: string };

export const editServiceProviderRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ServiceProvider>,
  undefined,
  UpdateServiceProviderPayload
> = {
  endpoint,
  invalidateQueries: ["get-service-providers", "get-service-provider-details", "get-providers"],
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<ServiceProvider>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
