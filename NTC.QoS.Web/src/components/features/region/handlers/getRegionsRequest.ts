import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Region } from "../types/Region";
import { type PaginationResponse } from "./../../../../types/reusable/api";

const endpoint = "/regions";

type RegionPQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  isWiredCovered?: boolean;
  isWirelessCovered?: boolean;
};

export const getRegionsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Region>,
  RegionPQueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-regions", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<Region>> => {
    return await request.get<PaginationResponse<Region>>(finalUrl, { signal });
  },
};
