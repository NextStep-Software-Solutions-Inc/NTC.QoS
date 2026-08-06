import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Municipality } from "../types/Municipality";

const endpoint = "/municipalities";
type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  provinceIds?: string[];
  provinceId?: string;
  regionIds?: string[];
  isWiredCovered?: boolean;
  isWirelessCovered?: boolean;
};


export const getMunicipalitiesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Municipality>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["municipalities", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Municipality>>(finalUrl, { signal });
  },
};
