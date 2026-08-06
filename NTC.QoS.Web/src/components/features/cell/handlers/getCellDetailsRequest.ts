import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate, type QueryParams } from "@/types/reusable/api";
import { type Cell } from "../types/Cell";

const endpoint = "/cells/:id";

export const getCellDetailsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<Cell>, QueryParams> = {
  endpoint,
  key: ({ url }) => [url],
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Cell>>(finalUrl, { signal });
  },
};
