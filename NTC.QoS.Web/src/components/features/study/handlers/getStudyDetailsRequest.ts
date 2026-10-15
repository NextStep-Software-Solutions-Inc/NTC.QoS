import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type Study } from "../types/Study";

const endpoint = "/studies/:id";

export const getStudyDetailsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<Study>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-study-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Study>>(finalUrl, { signal });
  },
};
