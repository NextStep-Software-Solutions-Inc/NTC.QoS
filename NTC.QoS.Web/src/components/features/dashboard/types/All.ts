import { type APIResponse } from "@/types/reusable/api";

export type CellCountEntity = { id: string; name: string; code: string; cellCount: number; entityType: string };

export type OverallCoverage = { technology: string; coveragePercentage: number; coverageCount: number };

export type TechnologiesCoverage = {
  technology: string;
  coveragePercentage: number;
  averageSignalStrength: number;
  coveragePointsCount: number;
};

export type RegionsCoverage = {
  regionId: string;
  regionName: string;
  regionCode: string;
  technologiesCoverage: TechnologiesCoverage[];
  overallCoveragePercentage: number;
};

export type WirelessCoverageData = { overallCoverage: OverallCoverage[]; regionsCoverage: RegionsCoverage[] };

export type DashboardAll = {
  totalCellsitesCount: APIResponse<{ verifiedCells: number; unverifiedCells: number; totalCells: number }>;
  totalNAPsCount: APIResponse<number>;
  topRegionsByCellCount: APIResponse<CellCountEntity[]>;
  topProvincesInRegionByCellCount: APIResponse<CellCountEntity[]>;
  topMunicipalitiesInProvinceByCellCount: APIResponse<CellCountEntity[]>;
  topBarangaysInMunicipalityByCellCount: APIResponse<CellCountEntity[]>;
  wirelessCoverage: APIResponse<WirelessCoverageData>;
};

export type All = CellCountEntity;
