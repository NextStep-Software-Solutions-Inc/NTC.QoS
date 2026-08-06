import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { request } from "@/services/fetch";
import { type GeneratedReportDto } from "../types/Report";

const endpoint = "/genai/reports/:id" as const;

export const getReportByIdRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<GeneratedReportDto>
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-report-by-id", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<GeneratedReportDto>>(finalUrl, { signal });
  },
};
