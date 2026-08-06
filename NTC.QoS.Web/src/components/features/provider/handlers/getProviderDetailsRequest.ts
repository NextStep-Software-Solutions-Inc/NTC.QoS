import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type ProviderDetails } from "../types/Provider";

const endpoint = "/providers/:id";

export const getProviderDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ProviderDetails>,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-providers", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<APIResponse<ProviderDetails>> => {
    return request.get<APIResponse<ProviderDetails>>(finalUrl, { signal, cache: "no-store" });
  },
};
