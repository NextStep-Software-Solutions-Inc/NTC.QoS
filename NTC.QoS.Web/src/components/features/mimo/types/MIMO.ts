export type MIMO = {
  id: string;
  name: string;
  description?: string;
  configuration: string; // e.g., "2x2", "4x4", "8x8"
  antennaCount: number;
  maxThroughput: number;
  cellId: string;
  createdAt: string;
  updatedAt: string;
};

export type MIMODetails = MIMO;
