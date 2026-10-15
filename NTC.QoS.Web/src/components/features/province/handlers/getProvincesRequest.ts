import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Province } from "../types/Province";

const endpoint = "/provinces";
type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  regionIds?: string[];
  isWiredCovered?: boolean;
  isWirelessCovered?: boolean;
};

export const getProvincesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Province>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-provinces", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Province>>(finalUrl, { signal });
  },
};
