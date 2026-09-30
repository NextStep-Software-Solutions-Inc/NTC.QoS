import { request } from "@/services/fetch";
import type { PaginationResponse, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users" as const;
type QueryParams = { pageNumber?: number; pageSize?: number; search?: string; regionIds?: string[]; roleId?: string };

export const getAllUsersRequest: QueryBlueprintFromTemplate<typeof endpoint, PaginationResponse<User>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-all-users", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<User>>(finalUrl, { signal });
  },
};
