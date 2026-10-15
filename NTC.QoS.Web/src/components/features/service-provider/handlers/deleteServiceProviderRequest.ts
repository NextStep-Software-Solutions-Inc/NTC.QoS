import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceProvider } from "../types/serviceProvider";

const endpoint = "/service-providers/:id";

export const deleteServiceProviderRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ServiceProvider>
> = {
  endpoint,
  invalidateQueries: ["get-service-providers", "get-service-provider-details", "get-providers"],
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<ServiceProvider>>(finalUrl);
  },
};
