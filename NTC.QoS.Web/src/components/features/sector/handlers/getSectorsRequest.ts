import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type Sector } from "../types/Sector";

const endpoint = "/sectors";

type GetSectorsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  cellIds?: string[];
  cellSectorId?: string;
  azimuthMin?: number;
  azimuthMax?: number;
  beamwidthMin?: number;
  beamwidthMax?: number;
  tiltMin?: number;
  tiltMax?: number;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getSectorsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<Sector>>,
  GetSectorsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-sectors", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<Sector>>>(finalUrl, { signal });
  },
};

export const getSectorDetailsRequest: QueryBlueprintFromTemplate<
  "/sectors/:id",
  APIResponse<Sector>,
  { id: string }
> = {
  endpoint: "/sectors/:id",
  key: ({ url, pathParams, queryParams }) =>
    ["get-sector-details", url, "/sectors/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Sector>>(finalUrl, { signal });
  },
};

type CreateSectorPayload = {
  name: string;
  description?: string;
  azimuth: number;
  beamwidth: number;
  tilt: number;
  cellId: string;
};

export const createSectorRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Sector>,
  undefined,
  CreateSectorPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Sector>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editSectorRequest: MutationBlueprintFromTemplate<
  "/sectors/:id",
  APIResponse<Sector>,
  { id: string },
  CreateSectorPayload
> = {
  endpoint: "/sectors/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Sector>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteSectorsRequest: MutationBlueprintFromTemplate<
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
