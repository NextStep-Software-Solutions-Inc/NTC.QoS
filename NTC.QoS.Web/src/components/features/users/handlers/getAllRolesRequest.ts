import { request } from "@/services/fetch";
import type { PaginationResponse, QueryBlueprintFromTemplate, QueryParams } from "@/types/reusable/api";

export type Role = { id: string; name: string; key: string; description?: string };

const endpoint = "/roles" as const;

export const getAllRolesRequest: QueryBlueprintFromTemplate<typeof endpoint, PaginationResponse<Role>, QueryParams> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-all-roles", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<PaginationResponse<Role>>(finalUrl);
  },
};
