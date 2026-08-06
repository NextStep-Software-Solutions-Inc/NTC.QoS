import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceProvider } from "../types/serviceProvider";

const endpoint = "/service-providers/:id/restore";

export const restoreServiceProviderRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<ServiceProvider>> =
  {
    endpoint,
    invalidateQueries: ["get-service-providers", "get-service-provider-details", "get-providers"],
    fetcher: async ({ finalUrl, signal }) => {
      return await request.post<APIResponse<ServiceProvider>>(finalUrl, undefined, signal ? { signal } : undefined);
    },
  };
