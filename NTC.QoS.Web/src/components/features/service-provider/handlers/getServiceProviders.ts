import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceProviderDetails } from "../types/serviceProvider";

const endpoint = "/service-providers";
type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  providerIds?: string[];
  serviceIds?: string[];
  serviceProviderIds?: string[];
  serviceTypes?: string[];
  search?: string;
};

export const getServiceProvidersRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<ServiceProviderDetails>,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-service-providers", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<ServiceProviderDetails>> => {
    return await request.get<PaginationResponse<ServiceProviderDetails>>(finalUrl, {
      signal,
      cache: "no-store",
    });
  },
};
