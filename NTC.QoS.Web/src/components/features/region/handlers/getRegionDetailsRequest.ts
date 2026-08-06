import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type APIResponse } from "../../../../types/reusable/api";
import { type Region } from "../types/Region";

const endpoint = "/regions/:id";

type RegionPQueryParams = { pageSize?: number; pageNumber?: number };

export const getRegionsDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Region>,
  RegionPQueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-regions", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<APIResponse<Region>> => {
    return await request.get<APIResponse<Region>>(finalUrl, { signal });
  },
};
