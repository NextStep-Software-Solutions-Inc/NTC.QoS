import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";

export type MapInteractionMode = "view" | "add" | "edit" | "delete";

// Visualization modes for speedtest data
export type VisualizationMode =
  | "heatmap" // Heatmap showing density and speed
  | "speed-circles" // Circles with radius based on download speed
  | "points" // Simple colored points
  | "signal-quality" // Points colored by signal quality
  | "coverage"; // Coverage quality gradient

export interface SpeedtestMapViewProps {
  speedtests: SpeedTest[];
  regionId?: string;
  focusedLocation?: { lat: number; lng: number } | null;
  className?: string;
  isExpanded?: boolean;
  onToggleExpand?: () => void;
  onAddSpeedtest?: (lat: number, lng: number) => void;
  onEditSpeedtest?: (speedtest: SpeedTest) => void;
  onDeleteSpeedtest?: (speedtest: SpeedTest) => void;
  onSelectSpeedtest?: (speedtest: SpeedTest | null) => void;
  selectedSpeedtestId?: string | null;
  isLoading?: boolean;
  loadingProgress?: { loaded: number; total: number };
  onBackToList?: () => void;
  regionName?: string;
}

export interface SpeedtestWithMeta extends SpeedTest {
  isPending?: boolean;
  hasError?: boolean;
}

export interface ProviderCount {
  count: number;
  color: string;
  visible: boolean;
  avgDownload: number;
  avgUpload: number;
}

export type ProviderCounts = Record<string, ProviderCount>;

export interface SignalQualityCount {
  count: number;
  color: string;
  visible: boolean;
}

export type SignalQualityCounts = Record<string, SignalQualityCount>;
