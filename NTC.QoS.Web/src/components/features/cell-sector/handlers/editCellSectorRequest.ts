import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSector } from "../types/CellSector";

const endpoint = "/cell-sectors/:id";

type UpdateCellSectorPayload = {
  cellId?: string;
  antennaType?: string;
  antennaHeight?: number;
  azimuth?: number;
  beamTilt?: number;
};

export const editCellSectorRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellSector>,
  undefined,
  UpdateCellSectorPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<CellSector>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
