import { request } from "@/services/fetch";
import {
  type APIResponse,
  type MutationBlueprintFromTemplate,
  type PaginationResponse,
  type QueryBlueprintFromTemplate,
} from "@/types/reusable/api";
import { type StudyNetwork } from "../types/StudyNetwork";

const endpoint = "/study-networks";

type GetStudyNetworksQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  studyIds?: string[];
  networkIds?: string[];
  priorityMin?: number;
  priorityMax?: number;
  status?: ("active" | "inactive" | "pending")[];
  sortBy?: string;
  sortOrder?: "asc" | "desc";
};

export const getStudyNetworksRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PaginationResponse<StudyNetwork>>,
  GetStudyNetworksQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-study-networks", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<PaginationResponse<StudyNetwork>>>(finalUrl, { signal });
  },
};

export const getStudyNetworkDetailsRequest: QueryBlueprintFromTemplate<
  "/study-networks/:id",
  APIResponse<StudyNetwork>,
  { id: string }
> = {
  endpoint: "/study-networks/:id",
  key: ({ url, pathParams, queryParams }) =>
    ["get-study-network-details", url, "/study-networks/:id", pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<StudyNetwork>>(finalUrl, { signal });
  },
};

type CreateStudyNetworkPayload = {
  name: string;
  description?: string;
  studyId: string;
  networkId: string;
  priority: number;
  status: "active" | "inactive" | "pending";
};

export const createStudyNetworkRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<StudyNetwork>,
  undefined,
  CreateStudyNetworkPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<StudyNetwork>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const editStudyNetworkRequest: MutationBlueprintFromTemplate<
  "/study-networks/:id",
  APIResponse<StudyNetwork>,
  { id: string },
  CreateStudyNetworkPayload
> = {
  endpoint: "/study-networks/:id",
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<StudyNetwork>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};

export const deleteStudyNetworksRequest: MutationBlueprintFromTemplate<
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
