import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ReportTemplate, type UpdateReportTemplatePayload } from "../types/ReportTemplate";

const endpoint = "/report-templates/:id";

export const updateReportTemplateRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ReportTemplate>,
  undefined,
  UpdateReportTemplatePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<ReportTemplate>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
