/**
 * Speedtest Management Types
 * Comprehensive type definitions for speedtest data and analytics
 */

// Re-export the main SpeedTest type
export type { SpeedTest, SpeedTestDetails } from "./SpeedTest";

// Signal quality enum
export type SignalQuality = "Unknown" | "VeryWeak" | "Weak" | "Average" | "Good" | "Excellent";

// Signal quality configuration for UI
export const SIGNAL_QUALITY_CONFIG: Record<
  SignalQuality,
  { label: string; color: string; bgColor: string; icon: string }
> = {
  Unknown: { label: "Unknown", color: "#6b7280", bgColor: "bg-gray-100", icon: "help-circle" },
  VeryWeak: { label: "Very Weak", color: "#ef4444", bgColor: "bg-red-100", icon: "signal" },
  Weak: { label: "Weak", color: "#f97316", bgColor: "bg-orange-100", icon: "signal" },
  Average: { label: "Average", color: "#eab308", bgColor: "bg-yellow-100", icon: "signal" },
  Good: { label: "Good", color: "#22c55e", bgColor: "bg-green-100", icon: "signal" },
  Excellent: { label: "Excellent", color: "#06b6d4", bgColor: "bg-cyan-100", icon: "signal" },
};

// Speedtest viewport type for map rendering (simplified)
export type SpeedtestViewport = {
  id: string;
  lat: number;
  lng: number;
  downloadSpeed: number;
  uploadSpeed: number;
  ping: number;
  provider: string;
  providerColor: string;
  signalQuality: SignalQuality;
  isNoBroadbandCoverage?: boolean;
};

// Speedtest minified for list views
export type SpeedtestMinified = {
  id: string;
  latitude: number;
  longitude: number;
  downloadSpeed: number;
  uploadSpeed: number;
  ping: number;
  signalQuality: SignalQuality;
  dateTime: string;
  provider?: string;
  providerColor?: string;
  isNoBroadbandCoverage?: boolean;
};

// Create speedtest payload
export type CreateSpeedtestPayload = {
  cellIdentifier: string;
  latitude: number;
  longitude: number;
  mcc: string;
  mnc: string;
  phoneModel: string;
  tac: string;
  address?: string;
  serviceProviderId?: string;
  technologyId: string;
  generationId?: string;
  dateTime?: string;
  ping?: number;
  jitter?: number;
  rssi?: number;
  uploadSpeed?: number;
  downloadSpeed?: number;
  signalQuality?: SignalQuality;
  isFixedBroadband?: boolean;
  isNoBroadbandCoverage?: boolean;
};

// Update speedtest payload
export type UpdateSpeedtestPayload = {
  downloadSpeed?: number;
  uploadSpeed?: number;
  ping?: number;
  jitter?: number;
  latitude?: number;
  longitude?: number;
  serviceProviderId?: string;
  deviceInfo?: string;
  testDate?: string;
  isNoBroadbandCoverage?: boolean;
};

// Speedtest query parameters
export type SpeedtestQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  search?: string;
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
  providerIds?: string[];
  serviceIds?: string[];
  serviceProviderIds?: string[];
  generationIds?: string[];
  cellIds?: string[];
  signalQuality?: string;
  longitude?: number;
  latitude?: number;
  distance?: number;
  startDate?: string;
  endDate?: string;
  minDownloadSpeed?: number;
  maxDownloadSpeed?: number;
  minUploadSpeed?: number;
  maxUploadSpeed?: number;
  isNoBroadbandCoverage?: boolean;
};

// Analytics types
export interface SpeedtestAnalytics {
  summary: SpeedtestSummary;
  providerStats: ProviderSpeedStats[];
  qualityDistribution: QualityDistribution[];
  trendData: SpeedtestTrend[];
  locationStats: LocationStats;
  insights: SpeedtestInsights;
}

export interface SpeedtestSummary {
  totalTests: number;
  avgDownloadSpeed: number;
  avgUploadSpeed: number;
  avgPing: number;
  avgJitter: number;
  providerCount: number;
  bestPerformer: string;
  bestPerformerColor: string;
  worstPerformer: string;
  overallQuality: SignalQuality;
}

export interface ProviderSpeedStats {
  provider: string;
  color: string;
  logo?: string;
  totalTests: number;
  avgDownloadSpeed: number;
  avgUploadSpeed: number;
  avgPing: number;
  avgJitter: number;
  qualityScore: number; // 0-100
  trend: "up" | "down" | "stable";
}

export interface QualityDistribution {
  quality: SignalQuality;
  count: number;
  percentage: number;
  color: string;
}

export interface SpeedtestTrend {
  date: string;
  avgDownloadSpeed: number;
  avgUploadSpeed: number;
  avgPing: number;
  testCount: number;
}

export interface LocationStats {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
  coverageArea: number;
  testDensity: number;
  hotspots: Array<{ lat: number; lng: number; count: number; avgSpeed: number }>;
}

export interface SpeedtestInsights {
  performanceRating: "excellent" | "good" | "average" | "poor";
  competitionLevel: "high" | "medium" | "low";
  coverageQuality: "excellent" | "good" | "moderate" | "sparse";
  recommendations: string[];
}

// Table column configuration
export interface SpeedtestTableColumn {
  id: string;
  label: string;
  sortable: boolean;
  visible: boolean;
  width?: string;
}

// Filter configuration
export interface SpeedtestFilters {
  search: string;
  providers: string[];
  signalQualities: SignalQuality[];
  dateRange: { start: Date | null; end: Date | null };
  speedRange: {
    minDownload: number | null;
    maxDownload: number | null;
    minUpload: number | null;
    maxUpload: number | null;
  };
  locations: { regionIds: string[]; provinceIds: string[]; municipalityIds: string[]; barangayIds: string[] };
  isNoBroadbandCoverage?: boolean;
}

// Export configuration
export interface SpeedtestExportConfig {
  format: "csv" | "pdf" | "excel";
  columns: string[];
  includeAnalytics: boolean;
  dateRange?: { start: Date; end: Date };
}
