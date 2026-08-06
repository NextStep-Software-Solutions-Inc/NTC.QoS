import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type NetworkTemplate } from "../types/NetworkTemplate";

const endpoint = "/network-templates";

type GetNetworkTemplatesQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  templateType?: ("standard" | "custom" | "premium")[];
  networkType?: ("2G" | "3G" | "4G" | "5G")[];
  isActive?: boolean;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getNetworkTemplatesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<NetworkTemplate>>,
  GetNetworkTemplatesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-network-templates", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<NetworkTemplate>>>(finalUrl, { signal });
  },
};

export const getNetworkTemplateDetailsRequest: QueryBlueprintFromTemplate<
  "/network-templates/:id",
  APIResponse<NetworkTemplate>,
  { id: string }
> = {
  endpoint: "/network-templates/:id",
  key: ({ url, pathParams, queryParams }) =>
    ["get-network-template-details", url, "/network-templates/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<NetworkTemplate>>(finalUrl, { signal });
  },
};

type CreateNetworkTemplatePayload = {
  name: string;
  description?: string;
  templateType: "standard" | "custom" | "premium";
  networkType: "2G" | "3G" | "4G" | "5G";
  configuration: Record<string, unknown>;
  isActive: boolean;
};

export const createNetworkTemplateRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<NetworkTemplate>,
  undefined,
  CreateNetworkTemplatePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<NetworkTemplate>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editNetworkTemplateRequest: MutationBlueprintFromTemplate<
  "/network-templates/:id",
  APIResponse<NetworkTemplate>,
  { id: string },
  CreateNetworkTemplatePayload
> = {
  endpoint: "/network-templates/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<NetworkTemplate>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteNetworkTemplatesRequest: MutationBlueprintFromTemplate<
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
