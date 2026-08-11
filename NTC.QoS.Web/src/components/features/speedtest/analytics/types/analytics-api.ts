/**
 * API Response types for /analytics/speedtest-analytics endpoint
 */

export interface SpeedtestAnalyticsApiResponse {
  data: SpeedtestAnalyticsData;
  succeeded: boolean;
  message: string;
  errors?: Record<string, string[]>;
}

export interface SpeedtestAnalyticsData {
  overview: AnalyticsOverview;
  providerPerformance: ProviderPerformance[];
  signalQualityDistribution: SignalQualityDistribution;
  providerRankings: ProviderRanking[];
}

export interface AnalyticsOverview {
  totalTests: number;
  averageDownloadMbps: number;
  averageUploadMbps: number;
  medianDownloadMbps: number;
  medianUploadMbps: number;
  averagePingMs: number;
  providerCount: number;
  coverageAreaKm2: number;
  performanceRating: string;
  bestProvider: string;
  competitionLevel: string;
}

export interface ProviderPerformance {
  providerName: string;
  providerColor: string;
  averageDownloadMbps: number;
  averageUploadMbps: number;
  medianDownloadMbps: number;
  medianUploadMbps: number;
  testCount: number;
}

export interface SignalQualityDistribution {
  totalTests: number;
  excellentCount: number;
  goodCount: number;
  averageCount: number;
  weakCount: number;
  veryWeakCount: number;
  excellentPercent: number;
  goodPercent: number;
  averagePercent: number;
  weakPercent: number;
  veryWeakPercent: number;
}

export interface ProviderRanking {
  rank: number;
  providerName: string;
  providerColor: string;
  testCount: number;
  downloadMbps: number;
  uploadMbps: number;
  averagePingMs: number;
  score: number;
  badge: string;
}

/**
 * Query parameters for the speedtest analytics endpoint
 */
export interface SpeedtestAnalyticsQueryParams {
  barangayIds?: string[];
  municipalityIds?: string[];
  provinceIds?: string[];
  regionIds?: string[];
  startDate?: string;
  endDate?: string;
}
