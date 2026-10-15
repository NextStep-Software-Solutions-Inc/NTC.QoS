import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type Province } from "../types/Province";

const endpoint = "/provinces/:id";

export const getProvinceRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<Province>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-provinces", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Province>>(finalUrl, { signal });
  },
};
