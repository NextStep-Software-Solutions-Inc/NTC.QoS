import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ConnectivityGapReportDto } from "../types/Report";

const endpoint = "/reports/connectivity-gap";

type GetConnectivityGapReportQueryParams = {
  regionId?: string;
  provinceId?: string;
  municipalityId?: string;
  pageNumber?: number;
  pageSize?: number;
};

export const getConnectivityGapReportRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ConnectivityGapReportDto>,
  GetConnectivityGapReportQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-connectivity-gap-report", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<ConnectivityGapReportDto>>(finalUrl, { signal });
  },
};
