import { request } from "@/services/fetch";
import type { APIResponse, QueryBlueprintFromTemplate, QueryParams } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users/auth-user/:id" as const;

export const getAuthUserRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<User>, QueryParams> = {
  endpoint,
  defaultOptions: {
    enabled: (q) => {
      const w = q.queryKey.at(3);
      return w !== null && typeof w === "object" && "id" in w && w.id !== "";
    },
  },
  key: ({ url, pathParams, queryParams }) => ["get-auth-user", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<APIResponse<User>>(finalUrl);
  },
};
