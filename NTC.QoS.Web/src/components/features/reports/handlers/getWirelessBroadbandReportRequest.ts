import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ReportFormat, type ServiceStatusEnum } from "../types/Report";

const endpoint = "/reports/wireless-broadband/download";

type GetWirelessBroadbandReportQueryParams = {
  regionId?: string;
  provinceId?: string;
  municipalityId?: string;
  filterByStatus?: ServiceStatusEnum;
  format?: ReportFormat;
  includeScores?: boolean;
};

export const getWirelessBroadbandReportRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  Blob,
  GetWirelessBroadbandReportQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-wireless-broadband-report-download", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<Blob>(finalUrl, { signal });
  },
};
