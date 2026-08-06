import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type BandDetails } from "../types/Band";

const endpoint = "/bands/:id";

type GetBandDetailsQueryParams = { id: string };

export const getBandDetailsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BandDetails>,
  GetBandDetailsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-band-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<BandDetails>>(finalUrl, { signal });
  },
};
