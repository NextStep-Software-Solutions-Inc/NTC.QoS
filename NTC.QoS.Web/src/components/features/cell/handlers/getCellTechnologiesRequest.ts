import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type CellTechnology } from "../types/Cell";

const endpoint = "/cells/:id/technologies";

export const getCellTechnologiesRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellTechnology[]>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["cell-technologies", url, endpoint, pathParams, queryParams],
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<CellTechnology[]>>(finalUrl, { signal });
  },
};
