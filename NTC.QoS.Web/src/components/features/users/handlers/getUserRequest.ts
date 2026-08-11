import { request } from "@/services/fetch";
import type { APIResponse, QueryBlueprintFromTemplate, QueryParams } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users/:id" as const;

export const getUserRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<User>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-user", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<APIResponse<User>>(finalUrl);
  },
};
