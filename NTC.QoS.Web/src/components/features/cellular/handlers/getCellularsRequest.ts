import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type Cellular } from "../types/Cellular";

const endpoint = "/cellulars";

type GetCellularsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  cellId?: string;
  cellSectorId?: string;
  serviceProviderId?: string;
  technologyId?: string;
  bandId?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
  frequencyMin?: number;
  frequencyMax?: number;
  bandwidthMin?: number;
  bandwidthMax?: number;
  signalStrengthMin?: number;
  signalStrengthMax?: number;
  latitudeMin?: number;
  latitudeMax?: number;
  longitudeMin?: number;
  longitudeMax?: number;
};

export const getCellularsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<Cellular>>,
  GetCellularsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-cellulars", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<Cellular>>>(finalUrl, { signal });
  },
};

export const getCellularDetailsRequest: QueryBlueprintFromTemplate<
  "/cellulars/:id",
  APIResponse<Cellular>,
  { id: string }
> = {
  endpoint: "/cellulars/:id",
  key: ({ url, pathParams, queryParams }) =>
    ["get-cellular-details", url, "/cellulars/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Cellular>>(finalUrl, { signal });
  },
};

type CreateCellularPayload = {
  cellSectorId: string;
  serviceProviderId: string;
  technologyId: string;
  bandId: string;
  address?: string;
};

export const createCellularRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Cellular>,
  undefined,
  CreateCellularPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Cellular>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editCellularRequest: MutationBlueprintFromTemplate<
  "/cellulars/:id",
  APIResponse<Cellular>,
  { id: string },
  CreateCellularPayload
> = {
  endpoint: "/cellulars/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Cellular>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteCellularsRequest: MutationBlueprintFromTemplate<
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
