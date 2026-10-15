import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type Network } from "../types/Network";

const endpoint = "/networks";

type GetNetworksQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  networkType?: ("2G" | "3G" | "4G" | "5G")[];
  providerIds?: string[];
  frequencyMin?: number;
  frequencyMax?: number;
  bandwidthMin?: number;
  bandwidthMax?: number;
  coverage?: string[];
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getNetworksRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<Network>>,
  GetNetworksQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-networks", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<Network>>>(finalUrl, { signal });
  },
};

export const getNetworkDetailsRequest: QueryBlueprintFromTemplate<
  "/networks/:id",
  APIResponse<Network>,
  { id: string }
> = {
  endpoint: "/networks/:id",
  key: ({ url, pathParams, queryParams }) =>
    ["get-network-details", url, "/networks/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Network>>(finalUrl, { signal });
  },
};

type CreateNetworkPayload = {
  name: string;
  description?: string;
  networkType: "2G" | "3G" | "4G" | "5G";
  providerId: string;
  frequency: number;
  bandwidth: number;
  coverage: string;
};

export const createNetworkRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Network>,
  undefined,
  CreateNetworkPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Network>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editNetworkRequest: MutationBlueprintFromTemplate<
  "/networks/:id",
  APIResponse<Network>,
  { id: string },
  CreateNetworkPayload
> = {
  endpoint: "/networks/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Network>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteNetworksRequest: MutationBlueprintFromTemplate<
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
