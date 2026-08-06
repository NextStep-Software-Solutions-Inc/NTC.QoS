import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type ServiceProviderDetails } from "../types/serviceProvider";

const endpoint = "/service-providers/:id";

export const getServiceProviderRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ServiceProviderDetails>,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) =>
    ["get-service-provider-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<APIResponse<ServiceProviderDetails>> => {
    return await request.get<APIResponse<ServiceProviderDetails>>(finalUrl, { signal });
  },
};
