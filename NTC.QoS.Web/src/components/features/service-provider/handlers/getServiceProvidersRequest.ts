import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceProvider } from "../types/serviceProvider";

const endpoint = "/service-providers";

type QueryParams = { pageSize?: number; pageNumber?: number; search?: string; providerIds?: string[]; serviceIds?: string[]; serviceTypes?: string[] };

export const getServiceProvidersRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ServiceProvider[]>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-service-providers", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<ServiceProvider[]>>(finalUrl, { signal, cache: "no-store" });
  },
};
