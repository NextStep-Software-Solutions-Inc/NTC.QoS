import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CreateReportTemplatePayload, type ReportTemplate } from "../types/ReportTemplate";

const endpoint = "/report-templates";

export const createReportTemplateRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ReportTemplate>,
  undefined,
  CreateReportTemplatePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<ReportTemplate>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
