import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ConnectivityReportDto, type ConnectivityStatusEnum } from "../types/Report";

const endpoint = "/reports/connectivity";

type GetConnectivityReportQueryParams = {
  regionId?: string;
  provinceId?: string;
  municipalityId?: string;
  filterByStatus?: ConnectivityStatusEnum;
  pageNumber?: number;
  pageSize?: number;
};

export const getConnectivityReportRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ConnectivityReportDto>,
  GetConnectivityReportQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-connectivity-report", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<ConnectivityReportDto>>(finalUrl, { signal });
  },
};
