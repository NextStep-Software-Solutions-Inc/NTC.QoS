import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellTopProvince } from "../types/CellTopProvince";

const endpoint = "/dashboard/provinces/top-by-cell-count/region-id";
type QueryParams = { startDate?: string; endDate?: string; regionId?: string };

export const getCellsTopProvinceRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellTopProvince[]>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-cell-counts", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<CellTopProvince[]>>(finalUrl, { signal });
  },
};
