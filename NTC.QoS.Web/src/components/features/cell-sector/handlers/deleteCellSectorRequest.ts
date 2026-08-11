import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSector } from "../types/CellSector";

const endpoint = "/cell-sectors/:id";

export const deleteCellSectorRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<CellSector>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<CellSector>>(finalUrl);
  },
};
