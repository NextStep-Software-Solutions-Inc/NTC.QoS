import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type AntennaPattern } from "../types/AntennaPattern";

const endpoint = "/antenna-patterns/:id";

type GetAntennaPatternDetailsQueryParams = { id: string };

export const getAntennaPatternDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AntennaPattern>,
  GetAntennaPatternDetailsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-antenna-pattern-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<AntennaPattern>>(finalUrl, { signal });
  },
};
