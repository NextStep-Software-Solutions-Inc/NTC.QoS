export type CellularCoverage = {
  id: string;
  name: string;
  description?: string;
  cellId: string;
  coverageType: "indoor" | "outdoor" | "both";
  signalStrength: number;
  coordinates: { latitude: number; longitude: number }[];
  createdAt: string;
  updatedAt: string;
};

export type CellularCoverageDetails = CellularCoverage;
