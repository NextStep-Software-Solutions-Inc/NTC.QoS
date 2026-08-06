import * as maptilersdk from "@maptiler/sdk";
import { Edit3, Eye, Plus, Trash2, type LucideIcon } from "lucide-react";

import { type MapInteractionMode } from "./types";

// Philippine geographic bounds
export const PH_CENTER: [number, number] = [122.0, 12.0];
export const PH_ZOOM = 4.5;
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
    description: "Pan & zoom, click NAPs to view details",
  },
  add: {
    label: "Add",
    icon: Plus,
    color: "bg-emerald-600 hover:bg-emerald-700",
    cursor: "crosshair",
    description: "Click on the map to add a new NAP",
  },
  edit: {
    label: "Edit",
    icon: Edit3,
    color: "bg-blue-600 hover:bg-blue-700",
    cursor: "pointer",
    description: "Click on a NAP marker to edit",
  },
  delete: {
    label: "Delete",
    icon: Trash2,
    color: "bg-red-600 hover:bg-red-700",
    cursor: "pointer",
    description: "Click on a NAP marker to delete",
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

// Cluster settings
export const CLUSTER_RADIUS = 30;
export const CLUSTER_MAX_ZOOM = 14;

// Default provider color
export const DEFAULT_PROVIDER_COLOR = "#6b7280";
export const UNKNOWN_PROVIDER = "Unknown";
