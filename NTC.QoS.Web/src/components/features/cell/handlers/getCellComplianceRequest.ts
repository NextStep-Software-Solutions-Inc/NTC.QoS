import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type CellCompliance } from "../types/Cell";

const endpoint = "/cells/:id/compliance";

export const getCellComplianceRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellCompliance>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-cell-compliance", url, endpoint, pathParams, queryParams],
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<CellCompliance>>(finalUrl, { signal });
  },
};
