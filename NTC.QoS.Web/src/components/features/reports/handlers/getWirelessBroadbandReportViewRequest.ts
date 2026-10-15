import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ServiceStatusEnum, type WirelessBroadbandReportDto } from "../types/Report";

const endpoint = "/reports/wireless-broadband";

type GetWirelessBroadbandReportQueryParams = {
  regionId?: string;
  provinceId?: string;
  municipalityId?: string;
  filterByStatus?: ServiceStatusEnum;
  forceGenerateConnectivityAssessment?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export const getWirelessBroadbandReportRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<WirelessBroadbandReportDto>,
  GetWirelessBroadbandReportQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-wireless-broadband-report", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<WirelessBroadbandReportDto>>(finalUrl, { signal });
  },
};
