export type LocationType = "Barangay" | "Municipality" | "Provincial" | "Regional";

export type ConnectivityStatus = "Served" | "Underserved" | "Unserved";

export type CMTSScore = {
  subject: string;
  a: number;
  fullMark: number;
};

export type CMTSScores = CMTSScore[];

export type Infrastructure = {
  cellTowerCount: number;
  fiberNAPCount: number;
  wiFiHotspotCount: number;
  serviceProviderCount: number;
  dominantServiceProvider: string | null;
  technologyBandBreakdown: Record<string, string> | null;
  providerBreakdown: {
    averageDownloadSpeedMbps: number;
    averageUploadSpeedMbps: number;
    averageLatencyMs: number;
    medianDownloadSpeedMbps: number;
    medianUploadSpeedMbps: number;
    speedTestCount: number;
    meetsMinimumSpeedRequirement: boolean;
    averageSignalStrength: number;
    signalQualityRating: string;
  }[];
  totalAssets: number;
};

export type SpeedStats = {
  averageDownloadSpeedMbps: number;
  averageUploadSpeedMbps: number;
  averageLatencyMs: number;
  medianDownloadSpeedMbps: number;
  medianUploadSpeedMbps: number;
  speedTestCount: number;
  meetsMinimumSpeedRequirement: boolean;
  averageSignalStrength: number;
  signalQualityRating: string;
} | null;

export type Coverage = {
  mobileCoveragePercentage: number | null;
  fixedBroadbandCoveragePercentage: number | null;
  fiberCoveragePercentage: number | null;
  hasReliableConnectivity: boolean;
  estimatedPopulationCoveragePercent: number | null;
  estimatedHouseholdCoveragePercent: number | null;
  assetsPerThousandPeople: number | null;
  providersPerThousandPeople: number | null;
};

export type Location = {
  locationId: string;
  locationName: string;
  locationType: LocationType;
  connectivityStatus: ConnectivityStatus;
  totalScore: number;
  cmtsScores: CMTSScores;
  infrastructure: Infrastructure;
  speedStats: SpeedStats;
  coverage: Coverage;
  areaSizeKm2: number;
  centerPoint: { latitude: number; longitude: number } | null;
  population: number | null;
  householdCount: number | null;
  assessmentDate: string;
  lastUpdated: string;
  notes: string | null;
};

export type TotalInfrastructure = {
  cellTowerCount: number;
  fiberNAPCount: number;
  wiFiHotspotCount: number;
  serviceProviderCount: number;
  dominantServiceProvider: string | null;
  technologyBandBreakdown: any | null;
  providerBreakdown: any[];
  totalAssets: number;
};

export type Summary = {
  totalLocations: number;
  servedCount: number;
  underservedCount: number;
  unservedCount: number;
  servedPercentage: number;
  underservedPercentage: number;
  unservedPercentage: number;
  totalInfrastructure: TotalInfrastructure;
  totalAreaKm2: number;
  totalPopulation: number;
  totalHouseholds: number;
  averageScore: number;
  averageCMTSScores: CMTSScores;
  analysisDate: string;
};

export type ConnectivityAnalysisData = { locationType: LocationType; locations: Location[]; summary: Summary };

export type ConnectivityAnalysisResponse = {
  data: ConnectivityAnalysisData;
  succeeded: boolean;
  message: string;
  errors: Record<string, any>;
};

export type ConnectivityAnalysisRequest = {
  locationIds: string[];
  locationType: LocationType;
  filterByStatus?: ConnectivityStatus;
  includeSpeedTestData?: boolean;
  includeInfrastructureData?: boolean;
};
