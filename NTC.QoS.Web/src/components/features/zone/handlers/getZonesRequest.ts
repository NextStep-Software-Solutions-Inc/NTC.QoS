import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type Zone } from "../types/Zone";

const endpoint = "/zones";

type GetZonesQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  regionIds?: string[];
  latitudeMin?: number;
  latitudeMax?: number;
  longitudeMin?: number;
  longitudeMax?: number;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getZonesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<Zone>>,
  GetZonesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-zones", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<Zone>>>(finalUrl, { signal });
  },
};

export const getZoneDetailsRequest: QueryBlueprintFromTemplate<"/zones/:id", APIResponse<Zone>, { id: string }> = {
  endpoint: "/zones/:id",
  key: ({ url, pathParams, queryParams }) => ["get-zone-details", url, "/zones/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Zone>>(finalUrl, { signal });
  },
};

type CreateZonePayload = {
  name: string;
  description?: string;
  coordinates: { latitude: number; longitude: number }[];
  regionId: string;
};

export const createZoneRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Zone>,
  undefined,
  CreateZonePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Zone>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editZoneRequest: MutationBlueprintFromTemplate<
  "/zones/:id",
  APIResponse<Zone>,
  { id: string },
  CreateZonePayload
> = {
  endpoint: "/zones/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Zone>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteZonesRequest: MutationBlueprintFromTemplate<
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
