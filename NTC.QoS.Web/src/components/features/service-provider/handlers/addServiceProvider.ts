import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceProvider } from "../types/serviceProvider";

const endpoint = "/service-providers";
type AddServiceProviderPayload = { providerId: string; serviceId: string; mnc: string; mcc: string };

export const addServiceProviderRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ServiceProvider>,
  undefined,
  AddServiceProviderPayload
> = {
  endpoint,
  invalidateQueries: ["get-service-providers", "get-service-provider-details", "get-providers"],
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<ServiceProvider>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
