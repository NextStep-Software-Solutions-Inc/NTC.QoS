import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type PaginationResponse } from "../../../../types/reusable/api";
import { type NAP } from "../types/NAP";

const endpoint = "/NAPs/public";

type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  regionIds?: string[];
  provinceIds?: string[];
  providerIds?: string;
  municipalityIds?: string[];
  barangayIds?: string[];
  serviceProviderIds?: string[];
  longitude?: number;
  latitude?: number;
  distance?: number;
};
export const getPublicNAPsRequest: QueryBlueprintFromTemplate<typeof endpoint, PaginationResponse<NAP>, QueryParams> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-NAPs", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<NAP>> => {
    return await request.get<PaginationResponse<NAP>>(finalUrl, { signal });
  },
};
