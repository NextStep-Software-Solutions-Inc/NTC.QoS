import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Cell } from "../types/Cell";

const endpoint = "/cells/public";

type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  status?: string;
  barangayIds?: string[];
  municipalityIds?: string[];
  provinceIds?: string[];
  providerIds?: string;
  regionIds?: string[];
  serviceIds?: string[];
  serviceProviderIds?: string[];
  longitude?: number;
  latitude?: number;
  distance?: number;
};

export const getPublicCellsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Cell>,
  QueryParams
> = {
  endpoint,
  key: ({ queryParams, pathParams, url }) => ["get-cells", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<PaginationResponse<Cell>> => {
    return await request.get<PaginationResponse<Cell>>(finalUrl, { signal });
  },
};
