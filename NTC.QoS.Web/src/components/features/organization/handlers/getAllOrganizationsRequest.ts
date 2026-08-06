import { request } from "@/services/fetch";
import type { PaginationResponse, QueryBlueprintFromTemplate, QueryParams } from "@/types/reusable/api";
import type { Organization } from "../types/Organization";

const endpoint = "/organizations" as const;

export const getAllOrganizationsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Organization>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-all-organizations", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<PaginationResponse<Organization>>(finalUrl);
  },
};
