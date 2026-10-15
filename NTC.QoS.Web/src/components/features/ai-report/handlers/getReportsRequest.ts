import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { request } from "@/services/fetch";
import { type GeneratedReportSummaryDto } from "../types/Report";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const endpoint = "/genai/reports" as const;

interface GetReportsQueryParams {
  pageSize?: number;
  pageNumber?: number;
}

export const getReportsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<GeneratedReportSummaryDto[]>,
  GetReportsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-reports", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<GeneratedReportSummaryDto[]>>(finalUrl, { signal });
  },
};

// Create React Query hook
export const useGetReports = createQueryHookFromTemplate(getReportsRequest);
