import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type CellularCoverage } from "../types/CellularCoverage";

const endpoint = "/cellular-coverages";

type GetCellularCoveragesQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  cellIds?: string[];
  coverageType?: ("indoor" | "outdoor" | "both")[];
  signalStrengthMin?: number;
  signalStrengthMax?: number;
  latitudeMin?: number;
  latitudeMax?: number;
  longitudeMin?: number;
  longitudeMax?: number;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getCellularCoveragesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<CellularCoverage>>,
  GetCellularCoveragesQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-cellular-coverages", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<CellularCoverage>>>(finalUrl, { signal });
  },
};

export const getCellularCoverageDetailsRequest: QueryBlueprintFromTemplate<
  "/cellular-coverages/:id",
  APIResponse<CellularCoverage>,
  { id: string }
> = {
  endpoint: "/cellular-coverages/:id",
  key: ({ url, pathParams, queryParams }) =>
    ["get-cellular-coverage-details", url, "/cellular-coverages/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<CellularCoverage>>(finalUrl, { signal });
  },
};

type CreateCellularCoveragePayload = {
  name: string;
  description?: string;
  cellId: string;
  coverageType: "indoor" | "outdoor" | "both";
  signalStrength: number;
  coordinates: { latitude: number; longitude: number }[];
};

export const createCellularCoverageRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellularCoverage>,
  undefined,
  CreateCellularCoveragePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<CellularCoverage>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editCellularCoverageRequest: MutationBlueprintFromTemplate<
  "/cellular-coverages/:id",
  APIResponse<CellularCoverage>,
  { id: string },
  CreateCellularCoveragePayload
> = {
  endpoint: "/cellular-coverages/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<CellularCoverage>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteCellularCoveragesRequest: MutationBlueprintFromTemplate<
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
