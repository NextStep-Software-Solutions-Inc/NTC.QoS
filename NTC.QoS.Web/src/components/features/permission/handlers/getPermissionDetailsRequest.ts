import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type PermissionDetails } from "../types/Permission";

const endpoint = "/permissions/:id";

export const getPermissionDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PermissionDetails>,
  undefined
> = {
  endpoint,
  key: ({ url, pathParams }) => ["get-permission-details", url, endpoint, pathParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PermissionDetails>>(finalUrl, { signal });
  },
};
