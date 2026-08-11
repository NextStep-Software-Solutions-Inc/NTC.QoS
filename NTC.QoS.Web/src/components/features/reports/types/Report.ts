// Report Format Enum
export enum ReportFormat {
  Excel = "Excel",
  Pdf = "Pdf",
}

// Service Status Enum
export enum ServiceStatusEnum {
  Unserved = "Unserved",
  Underserved = "Underserved",
  Served = "Served",
}

// Connectivity Status Enum
export enum ConnectivityStatusEnum {
  Unserved = "Unserved",
  Underserved = "Underserved",
  Served = "Served",
}

// Wireless Broadband Report
export type WirelessBroadbandReportResult = {
  fileContent: string | null; // base64 encoded
  fileName: string | null;
  contentType: string | null;
};

// Provider Wireless Info
export type ProviderWirelessInfo = {
  has2G: boolean;
  has3G: boolean;
  has4G: boolean;
  has5G: boolean;
  highestGeneration: string | null;
  availableGenerations: string | null;
  averageDownloadSpeedMbps: number | null;
  averageUploadSpeedMbps: number | null;
};

// Provider Availability
export type ProviderAvailability = {
  providers: Record<string, ProviderWirelessInfo>;
  availableProviders: string[];
  hasAnyProvider: boolean;
  providerCount: number;
};

// Wireless Barangay DTO
export type WirelessBarangayDto = {
  id: string;
  name: string | null;
  code: string | null;
  providerAvailability: ProviderAvailability;
  connectivityScore: number;
  serviceStatus: ServiceStatusEnum;
  highestTechnology: string | null;
  averageDownloadSpeedMbps: number | null;
  averageUploadSpeedMbps: number | null;
  remarks: string | null;
};

// Wireless Municipality DTO
export type WirelessMunicipalityDto = {
  id: string;
  name: string | null;
  code: string | null;
  barangays: WirelessBarangayDto[] | null;
  totalBarangays: number;
  servedBarangays: number;
  underservedBarangays: number;
  unservedBarangays: number;
};

// Barangay Broadband DTO (for Wired)
export type BarangayBroadbandDto = {
  id: string;
  name: string | null;
  code: string | null;
  serviceStatus: ServiceStatusEnum;
  providerAvailability?: {
    providers: Record<string, boolean>;
    availableProviders: string[];
    hasAnyProvider: boolean;
    providerCount: number;
  };
  population: number | null;
  households: number | null;
  providers: string[] | null;
  remarks: string | null;
};

// Municipality Broadband DTO
export type MunicipalityBroadbandDto = {
  id: string;
  name: string | null;
  code: string | null;
  barangays: BarangayBroadbandDto[] | null;
  totalBarangays: number;
  servedBarangays: number;
  underservedBarangays: number;
  unservedBarangays: number;
};

// Wired Broadband Summary DTO
export type WiredBroadbandSummaryDto = {
  totalMunicipalities: number;
  totalBarangays: number;
  servedBarangays: number;
  underservedBarangays: number;
  unservedBarangays: number;
  servedPercentage: number;
  underservedPercentage: number;
  unservedPercentage: number;
  allProviders: string[] | null;
  providerCoverage: Record<string, number> | null;
  generatedAt: string; // ISO date string
};

// Wired Broadband Report DTO
export type WiredBroadbandReportDto = {
  municipalities: MunicipalityBroadbandDto[] | null;
  summary: WiredBroadbandSummaryDto;
  pageNumber: number;
  pageSize: number;
  totalMunicipalities: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
};

// Priority Area DTO
export type PriorityAreaDto = {
  locationId: string;
  locationName: string | null;
  municipalityName: string | null;
  provinceName: string | null;
  connectivityStatus: ConnectivityStatusEnum;
  population: number | null;
  totalScore: number;
  lastAssessed: string; // ISO date string
};

// Connectivity Gap Summary DTO
export type ConnectivityGapSummaryDto = {
  totalUnservedAreas: number;
  totalUnderservedAreas: number;
  unservedPopulation: number;
  underservedPopulation: number;
};

// Connectivity Gap Report DTO
export type ConnectivityGapReportDto = {
  summary: ConnectivityGapSummaryDto;
  priorityAreas: PriorityAreaDto[] | null;
  totalPriorityAreas: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  generatedAt: string; // ISO date string
};

// Area DTO (for Connectivity Report)
export type AreaDto = {
  locationId: string;
  locationName: string | null;
  municipalityName: string | null;
  provinceName: string | null;
  connectivityStatus: ConnectivityStatusEnum;
  population: number | null;
  fiberCoverageScore: number;
  internetAccessScore: number;
  technologyTypeScore: number;
  qualityOfServiceScore: number;
  coverageAvailabilityScore: number;
  totalScore: number;
  cellTowerCount: number;
  fiberNAPCount: number;
  wifiHotspotCount: number;
  serviceProviderCount: number;
  dominantServiceProvider: string | null;
  averageDownloadSpeedMbps: number | null;
  averageUploadSpeedMbps: number | null;
  speedTestCount: number;
  lastAssessed: string; // ISO date string
};

// Score Summary DTO
export type ScoreSummaryDto = {
  averageFiberCoverageScore: number;
  averageInternetAccessScore: number;
  averageTechnologyTypeScore: number;
  averageQualityOfServiceScore: number;
  averageCoverageAvailabilityScore: number;
  averageTotalScore: number;
  excellentCount: number;
  goodCount: number;
  moderateCount: number;
  fairCount: number;
  totalCellTowers: number;
  totalFiberNAPs: number;
  totalWifiHotspots: number;
  totalServiceProviders: number;
};

// Municipality with Barangays DTO
export type MunicipalityWithBarangoaysDto = {
  municipalityId: string;
  municipalityName: string;
  provinceName: string;
  totalBarangays: number;
  servedBarangays: number;
  underservedBarangays: number;
  unservedBarangays: number;
  totalPopulation: number;
  servedPopulation: number;
  underservedPopulation: number;
  unservedPopulation: number;
  averageTotalScore: number;
  overallConnectivityStatus: ConnectivityStatusEnum;
  assessedConnectivityStatus: ConnectivityStatusEnum | null;
  barangays: AreaDto[];
};

// Connectivity Report DTO
export type ConnectivityReportDto = {
  municipalities: MunicipalityWithBarangoaysDto[];
  totalMunicipalities: number;
  totalBarangays: number;
  servedPopulation: number;
  underservedPopulation: number;
  unservedPopulation: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  scoreSummary: ScoreSummaryDto;
  regionSummary: {
    name: string;
    parentName: string | null;
    level: string;
    totalBarangays: number;
    servedBarangays: number;
    underservedBarangays: number;
    unservedBarangays: number;
    totalPopulation: number;
    servedPopulation: number;
    underservedPopulation: number;
    unservedPopulation: number;
    averageFiberCoverageScore: number;
    averageInternetAccessScore: number;
    averageTechnologyTypeScore: number;
    averageQualityOfServiceScore: number;
    averageCoverageAvailabilityScore: number;
    averageTotalScore: number;
    excellentCount: number;
    goodCount: number;
    moderateCount: number;
    fairCount: number;
    overallConnectivityStatus: ConnectivityStatusEnum;
    assessedConnectivityStatus: ConnectivityStatusEnum;
  };
  municipalitySummaries: Array<{
    name: string;
    parentName: string;
    level: string;
    totalBarangays: number;
    servedBarangays: number;
    underservedBarangays: number;
    unservedBarangays: number;
    totalPopulation: number;
    servedPopulation: number;
    underservedPopulation: number;
    unservedPopulation: number;
    averageFiberCoverageScore: number;
    averageInternetAccessScore: number;
    averageTechnologyTypeScore: number;
    averageQualityOfServiceScore: number;
    averageCoverageAvailabilityScore: number;
    averageTotalScore: number;
    excellentCount: number;
    goodCount: number;
    moderateCount: number;
    fairCount: number;
    overallConnectivityStatus: ConnectivityStatusEnum;
    assessedConnectivityStatus: ConnectivityStatusEnum;
  }> | null;
  provinceSummaries: Array<{
    name: string;
    parentName: string | null;
    level: string;
    totalBarangays: number;
    servedBarangays: number;
    underservedBarangays: number;
    unservedBarangays: number;
    totalPopulation: number;
    servedPopulation: number;
    underservedPopulation: number;
    unservedPopulation: number;
    averageFiberCoverageScore: number;
    averageInternetAccessScore: number;
    averageTechnologyTypeScore: number;
    averageQualityOfServiceScore: number;
    averageCoverageAvailabilityScore: number;
    averageTotalScore: number;
    excellentCount: number;
    goodCount: number;
    moderateCount: number;
    fairCount: number;
    overallConnectivityStatus: ConnectivityStatusEnum;
    assessedConnectivityStatus: ConnectivityStatusEnum;
  }> | null;
  generatedAt: string; // ISO date string
};

// Wireless Broadband Summary DTO
export type WirelessBroadbandSummaryDto = {
  totalMunicipalities: number;
  totalBarangays: number;
  servedBarangays: number;
  underservedBarangays: number;
  unservedBarangays: number;
  servedPercentage: number;
  underservedPercentage: number;
  unservedPercentage: number;
  allProviders: string[] | null;
  providerCoverage: Record<string, number> | null;
  technologyDistribution: Record<string, number> | null;
  averageConnectivityScore: number;
  generatedAt: string; // ISO date string
};

// Wireless Broadband Report DTO
export type WirelessBroadbandReportDto = {
  municipalities: WirelessMunicipalityDto[] | null;
  summary: WirelessBroadbandSummaryDto;
  pageNumber: number;
  pageSize: number;
  totalMunicipalities: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
};
