import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type NAPViewportAdmin } from "../types/NAP";

export const getNAPViewportEndpoint = "/v2/NAPs/viewport";

type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  minLat?: number;
  maxLat?: number;
  minLng?: number;
  maxLng?: number;
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
  serviceProviderIds?: string[];
};

export type NAPViewportResponse = PaginationResponse<NAPViewportAdmin> & {
  totalCount?: number;
};

export const getNAPViewportRequest: QueryBlueprintFromTemplate<
  typeof getNAPViewportEndpoint,
  NAPViewportResponse,
  QueryParams
> = {
  endpoint: getNAPViewportEndpoint,
  key: ({ url, pathParams, queryParams }) => ["get-NAPs-viewport", url, getNAPViewportEndpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }): Promise<NAPViewportResponse> => {
    return await request.get<NAPViewportResponse>(finalUrl, { signal });
  },
};
