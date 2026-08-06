import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSector } from "../types/CellSector";

const endpoint = "/cell-sectors/:id";

export const getCellSectorDetailsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<CellSector>> = {
  endpoint,
  key: ({ url }) => [url],
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<CellSector>>(finalUrl, { signal });
  },
};
