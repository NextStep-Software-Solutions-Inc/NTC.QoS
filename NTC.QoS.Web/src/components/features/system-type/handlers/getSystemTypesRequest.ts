import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type SystemType } from "../types/SystemType";

const endpoint = "/system-types";

type GetSystemTypesQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  category?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getSystemTypesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<SystemType>,
  GetSystemTypesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-system-types", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<SystemType>>(finalUrl, { signal });
  },
};
