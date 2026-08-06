/**
 * NAP Analytics API Response Types
 * Endpoint: /analytics/nap-analytics
 */

export interface NAPAnalyticsSummary {
  totalNAPs: number;
  activeProviders: number;
  avgNAPsPerProvider: number;
  densityPerKm2: number;
}

export interface NAPMarketShareItem {
  providerName: string;
  napCount: number;
  marketSharePercentage: number;
  providerColor: string;
}

export interface NAPProviderComparisonItem {
  providerName: string;
  napCount: number;
  providerColor: string;
}

export interface NAPLeadingProvider {
  providerName: string;
  marketSharePercentage: number;
  napCount: number;
}

export interface NAPGeographicCoverage {
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
  totalAreaKm2: number;
  coverageString: string;
}

export interface NAPMarketAnalysis {
  market: string;
  competitionScore: number;
  coverage: string;
}

export interface NAPAnalyticsData {
  summary: NAPAnalyticsSummary;
  marketShareDistribution: NAPMarketShareItem[];
  providerComparison: NAPProviderComparisonItem[];
  leadingProvider: NAPLeadingProvider;
  geographicCoverage: NAPGeographicCoverage;
  marketAnalysis: NAPMarketAnalysis;
}

export interface NAPAnalyticsResponse {
  data: NAPAnalyticsData;
  succeeded: boolean;
  message: string;
  errors?: Record<string, string[]>;
}
