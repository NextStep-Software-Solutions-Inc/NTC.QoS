import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/cellulars";

export type CreateCellularPayload = {
  cellSectorId: string;
  serviceProviderId: string;
  technologyId: string;
  bandId: string;
  address: string;
};

export type CellularResponse = {
  id: string;
  cellSector: {
    id: string;
    antennaType: string;
    antennaHeight: number;
    azimuth: number;
    beamTilt: number;
  };
  serviceProvider: {
    id: string;
    provider: {
      id: string;
      name: string;
      color: string;
    };
  };
  technology: {
    id: string;
    standard: string;
    description: string;
  };
  band: {
    id: string;
    name: string;
    description: string;
  };
  address: string;
};

export const createCellularRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellularResponse>,
  undefined,
  CreateCellularPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    // Backend expects the payload wrapped in cellularDto property
    const wrappedPayload = { cellularDto: payload };
    return await request.post<APIResponse<CellularResponse>>(finalUrl, wrappedPayload, {
      signal,
    });
  },
  invalidateQueries: ["get-cell-technologies"],
};
