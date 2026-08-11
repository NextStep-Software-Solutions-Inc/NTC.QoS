import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type MIMO } from "../types/MIMO";

const endpoint = "/mimos";

type GetMIMOsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  cellIds?: string[];
  configuration?: string[];
  antennaCountMin?: number;
  antennaCountMax?: number;
  maxThroughputMin?: number;
  maxThroughputMax?: number;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getMIMOsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<MIMO>>,
  GetMIMOsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-mimos", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<MIMO>>>(finalUrl, { signal });
  },
};

export const getMIMODetailsRequest: QueryBlueprintFromTemplate<"/mimos/:id", APIResponse<MIMO>, { id: string }> = {
  endpoint: "/mimos/:id",
  key: ({ url, pathParams, queryParams }) => ["get-mimo-details", url, "/mimos/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<MIMO>>(finalUrl, { signal });
  },
};

type CreateMIMOPayload = {
  name: string;
  description?: string;
  configuration: string;
  antennaCount: number;
  maxThroughput: number;
  cellId: string;
};

export const createMIMORequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<MIMO>,
  undefined,
  CreateMIMOPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<MIMO>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editMIMORequest: MutationBlueprintFromTemplate<
  "/mimos/:id",
  APIResponse<MIMO>,
  { id: string },
  CreateMIMOPayload
> = {
  endpoint: "/mimos/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<MIMO>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteMIMOsRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<string>,
  undefined,
  { ids: string[] }
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.delete<APIResponse<string>>(finalUrl, {
      body: JSON.stringify(payload),
      headers: { "Content-Type": "application/json" },
    });
  },
};
