import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type PaginationResponse } from "../../../../types/reusable/api";
import { type Barangay } from "../types/Barangay";

const endpoint = "/barangays";
type QueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  municipalityIds?: string[];
  municipality?: string;
  name?: string;
  province?: string;
  isWiredCovered?: boolean;
  isWirelessCovered?: boolean;
};

export const getBarangaysRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<Barangay>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-barangays", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<Barangay>>(finalUrl, { signal });
  },
};
