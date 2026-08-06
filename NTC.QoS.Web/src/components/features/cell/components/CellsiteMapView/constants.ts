import * as maptilersdk from "@maptiler/sdk";
import { Edit3, Eye, Plus, Trash2, type LucideIcon } from "lucide-react";

import { type MapInteractionMode } from "./types";

// Philippine geographic bounds
export const PH_CENTER: [number, number] = [122.0, 12.0];
export const PH_ZOOM = 5.5;
export const PH_MIN_ZOOM = 4.5;
export const PH_BOUNDS: [[number, number], [number, number]] = [
  [116.0, 4.5],
  [127.0, 21.5],
];

// Mode configurations
export interface ModeConfig {
  label: string;
  icon: LucideIcon;
  color: string;
  cursor: string;
  description: string;
}

export const MODE_CONFIG: Record<MapInteractionMode, ModeConfig> = {
  view: {
    label: "View",
    icon: Eye,
    color: "bg-slate-700 hover:bg-slate-800",
    cursor: "grab",
    description: "Pan & zoom, click cell sites to view details",
  },
  add: {
    label: "Add",
    icon: Plus,
    color: "bg-emerald-600 hover:bg-emerald-700",
    cursor: "crosshair",
    description: "Click on the map to add a new cell site",
  },
  edit: {
    label: "Edit",
    icon: Edit3,
    color: "bg-blue-600 hover:bg-blue-700",
    cursor: "pointer",
    description: "Click on a cell site marker to edit",
  },
  delete: {
    label: "Delete",
    icon: Trash2,
    color: "bg-red-600 hover:bg-red-700",
    cursor: "pointer",
    description: "Click on a cell site marker to delete",
  },
};

// Map styles with terrain and 3D options
export interface MapStyleConfig {
  id: string;
  label: string;
  style: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant;
  icon?: string;
  has3d?: boolean;
  hasTerrain?: boolean;
}

export const MAP_STYLES: MapStyleConfig[] = [
  { id: "streets", label: "Streets", style: maptilersdk.MapStyle.STREETS_V4, icon: "🏙️" },
  { id: "satellite", label: "Satellite", style: maptilersdk.MapStyle.SATELLITE, icon: "🛰️" },
  { id: "hybrid", label: "Hybrid", style: maptilersdk.MapStyle.HYBRID, icon: "🗺️" },
  { id: "topo", label: "Topographic", style: maptilersdk.MapStyle.TOPO, icon: "🏔️" },
  { id: "outdoor", label: "Outdoor", style: maptilersdk.MapStyle.OUTDOOR, icon: "🌲", hasTerrain: true },
  { id: "topo_dark", label: "Topo Dark", style: maptilersdk.MapStyle.TOPO.DARK, icon: "🌙" },
  { id: "dark", label: "Dark", style: maptilersdk.MapStyle.STREETS_V4.DARK, icon: "⬛" },
];

// Signal strength filter options for coverage layer
export const SIGNAL_STRENGTH_FILTERS = [
  { id: "all", label: "All Signals", color: "#6b7280", minRssi: -150, maxRssi: 0 },
  { id: "excellent", label: "Excellent (≥-60 dBm)", color: "#22c55e", minRssi: -60, maxRssi: 0 },
  { id: "good", label: "Good (-70 to -60 dBm)", color: "#84cc16", minRssi: -70, maxRssi: -60 },
  { id: "fair", label: "Fair (-80 to -70 dBm)", color: "#facc15", minRssi: -80, maxRssi: -70 },
  { id: "poor", label: "Poor (-90 to -80 dBm)", color: "#f97316", minRssi: -90, maxRssi: -80 },
  { id: "veryPoor", label: "Very Poor (≤-90 dBm)", color: "#ef4444", minRssi: -150, maxRssi: -90 },
] as const;

// Cluster settings - Reduced radius for tighter clusters
export const CLUSTER_RADIUS = 25;
export const CLUSTER_MAX_ZOOM = 14;

// Cluster colors - Different from individual cell site colors
export const CLUSTER_COLORS = {
  small: "#6366f1", // Indigo for small clusters (<10)
  medium: "#8b5cf6", // Purple for medium clusters (10-50)
  large: "#a855f7", // Violet for large clusters (50-100)
  huge: "#c026d3", // Fuchsia for huge clusters (>100)
};

// Popup settings
export const POPUP_MAX_WIDTH = "320px";

// Default provider color
export const DEFAULT_PROVIDER_COLOR = "#6b7280";
export const UNKNOWN_PROVIDER = "Unknown";

// Provider icon mappings for cell sites - used for symbol layer
export const PROVIDER_ICONS: Record<string, string> = {
  Globe: "/assets/images/cell-globe.png",
  Smart: "/assets/images/cell-smart.png",
  DITO: "/assets/images/cell-dito.png",
  default: "/assets/images/cell-site.jpg",
};

// Provider icon IDs for MapTiler (used for loading icons)
export const PROVIDER_ICON_IDS: Record<string, string> = {
  Globe: "cell-globe",
  Smart: "cell-smart",
  DITO: "cell-dito",
  default: "cell-default",
};

// Icon size for cell site markers (reduced from 0.6)
export const CELL_ICON_SIZE = 0.35;

/**
 * Get provider icon URL
 */
export function getProviderIconUrl(providerName: string): string {
  const normalizedName = providerName.toLowerCase();
  const defaultIcon = "/assets/images/cell-site.jpg";

  for (const [key, url] of Object.entries(PROVIDER_ICONS)) {
    if (key.toLowerCase() === normalizedName || normalizedName.includes(key.toLowerCase())) {
      return url;
    }
  }

  return PROVIDER_ICONS.default ?? defaultIcon;
}

/**
 * Get provider icon ID for MapTiler symbol layer
 */
export function getProviderIconId(providerName: string): string {
  const normalizedName = providerName.toLowerCase();
  const defaultId = "cell-default";

  for (const [key, id] of Object.entries(PROVIDER_ICON_IDS)) {
    if (key.toLowerCase() === normalizedName || normalizedName.includes(key.toLowerCase())) {
      return id;
    }
  }

  return PROVIDER_ICON_IDS.default ?? defaultId;
}

// Layer opacity defaults
export const DEFAULT_LAYER_OPACITY = { cellSites: 1.0, cellSiteHeatmap: 0.7, signalCoverage: 0.8, pointView: 0.8 };

// Signal coverage data path
export const SIGNAL_COVERAGE_DATA_PATH = "/data/all_telco_band_data_flat.json";

// Signal strength color scale (RSSI values in dBm)
// -60 or better = Excellent (green), -70 = Good (lime), -80 = Fair (yellow), -90 = Poor (orange), -90 or worse = Very Poor (red)
export const SIGNAL_STRENGTH_COLORS = {
  excellent: "#22c55e", // Green: -60 dBm or better
  good: "#84cc16", // Lime: -70 to -60 dBm
  fair: "#facc15", // Yellow: -80 to -70 dBm
  poor: "#f97316", // Orange: -90 to -80 dBm
  veryPoor: "#ef4444", // Red: worse than -90 dBm
};
