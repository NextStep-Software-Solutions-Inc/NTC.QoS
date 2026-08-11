import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type NAPAPIDetails } from "../types/NAP";

const endpoint = "/NAPs/:id";

export const getNAPDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<NAPAPIDetails>,
  QueryParams
> = {
  endpoint,
  key: ({ url }) => [url],
  fetcher: async ({ finalUrl, signal }): Promise<APIResponse<NAPAPIDetails>> => {
    return request.get<APIResponse<NAPAPIDetails>>(finalUrl, { signal });
  },
};
