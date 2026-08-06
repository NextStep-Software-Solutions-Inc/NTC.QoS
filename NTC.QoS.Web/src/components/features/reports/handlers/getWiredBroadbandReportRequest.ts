import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceStatusEnum, type WiredBroadbandReportDto } from "../types/Report";

const endpoint = "/reports/wired-broadband";

type GetWiredBroadbandReportQueryParams = {
  regionId?: string;
  provinceId?: string;
  municipalityId?: string;
  filterByStatus?: ServiceStatusEnum;
  forceGenerateConnectivityAssessment?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export const getWiredBroadbandReportRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<WiredBroadbandReportDto>,
  GetWiredBroadbandReportQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-wired-broadband-report", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<WiredBroadbandReportDto>>(finalUrl, { signal });
  },
};
