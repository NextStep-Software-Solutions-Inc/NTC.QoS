import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type Municipality } from "../types/Municipality";

const endpoint = "/municipalities/:id" as const;

export const getMunicipalityRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Municipality>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["municipality", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Municipality>>(finalUrl, { signal });
  },
};
