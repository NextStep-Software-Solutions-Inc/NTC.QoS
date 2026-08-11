import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type ReportTemplate } from "../types/ReportTemplate";

const endpoint = "/report-templates/:id";

type GetReportTemplateResponse = APIResponse<ReportTemplate>;

export const getReportTemplateRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  GetReportTemplateResponse,
  undefined
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-report-template", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<GetReportTemplateResponse>(finalUrl, { signal });
  },
};
