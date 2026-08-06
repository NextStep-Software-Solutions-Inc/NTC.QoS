import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSector } from "./../types/CellSector";

const endpoint = "/cell-sectors";

type CellSectorPayload = {
  cellId: string;
  antennaType: string;
  antennaHeight: number;
  azimuth: number;
  beamTilt: number;
};

export const createCellSectorHandlerRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellSector>,
  undefined,
  CellSectorPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload }) => {
    return await request.post<APIResponse<CellSector>>(finalUrl, payload);
  },
};
