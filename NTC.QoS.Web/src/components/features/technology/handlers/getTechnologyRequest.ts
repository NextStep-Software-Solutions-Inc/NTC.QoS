import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type TechnologyDetails } from "../types/Technology";

const endpoint = "/technologies/:id";

type GetTechnologyDetailsQueryParams = { id: string };

export const getTechnologyDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<TechnologyDetails>,
  GetTechnologyDetailsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-technology-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<TechnologyDetails>>(finalUrl, { signal });
  },
};
