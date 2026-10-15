import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Provider } from "../types/Provider";
import { type PaginationResponse } from "./../../../../types/reusable/api";

const endpoint = "/providers";

type QueryParams = { pageSize?: number; pageNumber?: number; search?: string; serviceIds?: string[]; names?: string[] };
export const getProvidersRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Provider>,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-providers", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<Provider>> => {
    return await request.get<PaginationResponse<Provider>>(finalUrl, { signal });
  },
};
