import { type Cell } from "../../cell/types/Cell";

export type CellSector = {
  id: string;
  cell: Cell;
  antennaType: string;
  antennaHeight: number;
  azimuth: number;
  beamTilt: number;
};
export type CellSectorWithDetails = CellSector & {
  // Add any additional properties for detailed view here
};
