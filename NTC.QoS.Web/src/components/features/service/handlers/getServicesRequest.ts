import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Service } from "../types/Service";

type QueryParams = { pageSize?: number; pageNumber?: number; search?: string; providerIds?: string[]; types?: string[] };

const endpoint = "/services";
export const getServicesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Service>,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-services", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Service>>(finalUrl, { signal, cache: "no-store" });
  },
};
