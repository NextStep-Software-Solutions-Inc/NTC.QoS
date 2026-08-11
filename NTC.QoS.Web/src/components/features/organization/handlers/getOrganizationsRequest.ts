import { request } from "@/services/fetch";
import type { PaginationResponse, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { Organization } from "../types/Organization";

const endpoint = "/organizations" as const;
type QueryParams = { pageNumber?: number; pageSize?: number; search?: string };

export const getOrganizationsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Organization>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-organizations", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Organization>>(finalUrl, { signal });
  },
};
