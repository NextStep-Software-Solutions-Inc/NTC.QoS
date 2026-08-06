import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Link } from "../types/Link";

const endpoint = "/links";

type GetLinksQueryParams = { pageSize?: number; pageNumber?: number; search?: string; sortBy?: string; sortOrder?: "asc" | "desc" };

export const getLinksRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Link>,
  GetLinksQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-links", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Link>>(finalUrl, { signal });
  },
};
