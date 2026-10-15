import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSitesByProviderQueryParams, type CellSitesByProviderResponse } from "../types/CellSitesByProvider";

const endpoint = "/dashboard/cell-sites-by-provider";

export const getCellSitesByProviderRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  CellSitesByProviderResponse,
  CellSitesByProviderQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-cell-sites-by-provider", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<CellSitesByProviderResponse>(finalUrl, { signal });
  },
};
