import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type BarangayDetails } from "../types/Barangay";

const endpoint = "/barangays/:id" as const;

export const getBarangayDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BarangayDetails>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-barangay", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<BarangayDetails>>(finalUrl, { signal });
  },
};
