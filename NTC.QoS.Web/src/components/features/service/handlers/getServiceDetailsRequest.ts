import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Service } from "../types/Service";

const endpoint = "/services/:id";

type GetServiceDetailsQueryParams = { id: string };

export const getServiceDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Service>,
  GetServiceDetailsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-service-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Service>>(finalUrl, { signal });
  },
};
