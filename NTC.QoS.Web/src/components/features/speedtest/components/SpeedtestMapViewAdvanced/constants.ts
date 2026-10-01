import * as maptilersdk from "@maptiler/sdk";
import { Circle, Edit3, Eye, Flame, Plus, Radar, Radio, Signal, Trash2, type LucideIcon } from "lucide-react";

import { type MapInteractionMode, type VisualizationMode } from "./types";

// Philippine geographic bounds
export const PH_CENTER: [number, number] = [122.0, 12.0];
export const PH_ZOOM = 5;
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
    description: "Pan & zoom, click speedtests to view details",
  },
  add: {
    label: "Add",
    icon: Plus,
    color: "bg-emerald-600 hover:bg-emerald-700",
    cursor: "crosshair",
    description: "Click on the map to add a new speedtest",
  },
  edit: {
    label: "Edit",
    icon: Edit3,
    color: "bg-blue-600 hover:bg-blue-700",
    cursor: "pointer",
    description: "Click on a speedtest to edit",
  },
  delete: {
    label: "Delete",
    icon: Trash2,
    color: "bg-red-600 hover:bg-red-700",
    cursor: "pointer",
    description: "Click on a speedtest to delete",
  },
};

// Visualization mode configurations
export interface VisualizationConfig {
  label: string;
  icon: LucideIcon;
  color: string;
  description: string;
}

export const VISUALIZATION_CONFIG: Record<VisualizationMode, VisualizationConfig> = {
  heatmap: {
    label: "Heatmap",
    icon: Flame,
    color: "text-orange-500",
    description: "Show density and speed distribution as heat gradient",
  },
  "speed-circles": {
    label: "Speed Circles",
    icon: Radio,
    color: "text-cyan-500",
    description: "Circle radius based on download speed",
  },
  points: { label: "Points", icon: Circle, color: "text-blue-500", description: "Simple colored dots by provider" },
  "signal-quality": {
    label: "Signal",
    icon: Signal,
    color: "text-green-500",
    description: "Points colored by signal quality",
  },
  coverage: {
    label: "Coverage",
    icon: Radar,
    color: "text-purple-500",
    description: "Coverage quality gradient overlay",
  },
};

// Map styles
export interface MapStyleConfig {
  id: string;
  label: string;
  style: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant;
}

export const MAP_STYLES: MapStyleConfig[] = [
  { id: "streets", label: "Streets", style: maptilersdk.MapStyle.STREETS_V4 },
  { id: "satellite", label: "Satellite", style: maptilersdk.MapStyle.SATELLITE },
  { id: "hybrid", label: "Hybrid", style: maptilersdk.MapStyle.HYBRID },
  { id: "topo", label: "Topographic", style: maptilersdk.MapStyle.TOPO },
  { id: "topo_dark", label: "Topographic Dark", style: maptilersdk.MapStyle.TOPO.DARK },
  { id: "dark", label: "Dark", style: maptilersdk.MapStyle.STREETS_V4.DARK },
];

// Default colors
export const DEFAULT_PROVIDER_COLOR = "#6b7280";
export const UNKNOWN_PROVIDER = "Unknown";
export const NBC_COLOR = "#9ca3af"; // grey for No Broadband Coverage
export const NBC_COLOR_COVERAGE = "rgba(156, 163, 175, 0.6)"; // semi-transparent NBC grey for coverage overlay

// Signal quality colors
export const SIGNAL_QUALITY_COLORS: Record<string, string> = {
  Excellent: "#10b981",
  Good: "#22c55e",
  Average: "#f59e0b",
  Weak: "#ef4444",
  VeryWeak: "#dc2626",
  Unknown: "#6b7280",
};

// Signal quality order for legend
export const SIGNAL_QUALITY_ORDER = ["Excellent", "Good", "Average", "Weak", "VeryWeak", "Unknown"];

// Speed thresholds for color coding (Mbps)
export const SPEED_THRESHOLDS = {
  excellent: 100, // >= 100 Mbps
  good: 50, // >= 50 Mbps
  average: 25, // >= 25 Mbps
  fair: 10, // >= 10 Mbps
  poor: 0, // < 10 Mbps
};

// Speed colors for visualization
export const SPEED_COLORS = {
  excellent: "#10b981", // emerald
  good: "#22c55e", // green
  average: "#f59e0b", // amber
  fair: "#f97316", // orange
  poor: "#ef4444", // red
};
