import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSector } from "../types/CellSector";

const endpoint = "/cell-sectors";

type CreateCellSectorPayload = {
  cellId: string;
  antennaType: string;
  antennaHeight: number;
  azimuth: number;
  beamTilt: number;
};

export const createCellSectorRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellSector>,
  undefined,
  CreateCellSectorPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<CellSector>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
