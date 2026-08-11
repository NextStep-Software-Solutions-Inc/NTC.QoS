import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ReportTemplateListItem } from "../types/ReportTemplate";

const endpoint = "/report-templates";

type GetReportTemplatesQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
};

export const getReportTemplatesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<ReportTemplateListItem>,
  GetReportTemplatesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-report-templates", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<ReportTemplateListItem>>(finalUrl, { signal });
  },
};
