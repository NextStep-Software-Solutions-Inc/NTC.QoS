import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type GenerationDetails } from "../types/Generation";

const endpoint = "/generations/:id";

type GetGenerationDetailsQueryParams = { id: string };

export const getGenerationDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<GenerationDetails>,
  GetGenerationDetailsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) =>
    ["get-generation-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<GenerationDetails>>(finalUrl, { signal });
  },
};
