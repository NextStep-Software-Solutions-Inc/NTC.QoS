import { request } from "@/services/fetch";
import type { APIResponse, QueryBlueprintFromTemplate, QueryParams } from "@/types/reusable/api";
import type { CurrentUser } from "../types/User";

const endpoint = "/users/me" as const;

export const getCurrentUserRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<CurrentUser>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-current-user", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<APIResponse<CurrentUser>>(finalUrl);
  },
};
