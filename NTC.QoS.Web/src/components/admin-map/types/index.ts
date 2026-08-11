/**
 * Admin Map Types
 *
 * This module defines TypeScript types for the refactored admin map system.
 * Follows the Open/Closed principle - extend via new types rather than modifying existing ones.
 */

import type * as maptilersdk from "@maptiler/sdk";
import type { LucideIcon } from "lucide-react";

// ============================================
// LAYER TYPES
// ============================================

/**
 * Base layer configuration interface.
 * All layer types extend this interface.
 */
export interface LayerConfig {
  /** Unique identifier for the layer */
  id: string;
  /** Display label for the layer */
  label: string;
  /** Icon component for the layer */
  icon: LucideIcon;
  /** URL parameter key for visibility control */
  param: string;
  /** Tailwind color class for the layer icon */
  color: string;
  /** Optional description shown in the UI */
  description?: string;
  /** Whether the layer supports opacity control */
  hasOpacity?: boolean;
  /** Default opacity (0-100) */
  defaultOpacity?: number;
  /** Sub-layers that this layer controls */
  subLayers?: string[];
  /** Layer category for grouping */
  category?: LayerCategory;
}

/**
 * Layer categories for grouping in the UI
 */
export type LayerCategory = "cellular" | "fiber" | "speedtest" | "coverage" | "infrastructure";

/**
 * Layer visibility state map
 */
export type LayerVisibility = Record<string, boolean>;

/**
 * Layer opacity state map
 */
export type LayerOpacity = Record<string, number>;

/**
 * Combined layer state
 */
export interface LayerState {
  visibility: LayerVisibility;
  opacity: LayerOpacity;
}

// ============================================
// MAP STYLE TYPES
// ============================================

/**
 * Map style configuration
 */
export interface MapStyleConfig {
  /** Unique identifier */
  id: string;
  /** Display name */
  name: string;
  /** Thumbnail image URL */
  thumbnailUrl: string;
  /** MapTiler style reference */
  style: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant;
  /** Whether this is the default style */
  isDefault?: boolean;
}

// ============================================
// FILTER TYPES
// ============================================

/**
 * Provider/ISP filter option
 */
export interface ProviderOption {
  id: string;
  name: string;
  color?: string;
  logo?: string;
}

/**
 * Date range filter
 */
export interface DateRangeFilter {
  startDate?: Date;
  endDate?: Date;
}

/**
 * Signal strength filter range
 */
export interface SignalStrengthFilter {
  min: number; // dBm (e.g., -120)
  max: number; // dBm (e.g., -40)
}

/**
 * Speed test filter
 */
export interface SpeedTestFilter {
  downloadMin?: number;
  downloadMax?: number;
  uploadMin?: number;
  uploadMax?: number;
}

/**
 * Network type filter
 */
export type NetworkType = "2G" | "3G" | "4G" | "5G";

/**
 * Combined filter state for the admin map
 */
export interface AdminMapFilters {
  providers: string[];
  wirelessServiceProviderIds: string[];
  wiredServiceProviderIds: string[]; // Wired service provider IDs for NAP/fiber filtering
  fiberProviderNames: string[]; // Provider display names matching vector tile Providers property
  generationIds: string[]; // Generation IDs for network type filtering (replaces networkTypes)
  dateRange: DateRangeFilter;
  signalStrength: SignalStrengthFilter;
  speedTest: SpeedTestFilter;
  regionIds: string[];
  searchQuery?: string;
}

/**
 * Default filter values
 */
export const DEFAULT_ADMIN_MAP_FILTERS: AdminMapFilters = {
  providers: [],
  wirelessServiceProviderIds: [],
  wiredServiceProviderIds: [],
  fiberProviderNames: [],
  generationIds: [],
  dateRange: {},
  signalStrength: { min: -120, max: -40 },
  speedTest: {},
  regionIds: [],
  searchQuery: "",
};

// ============================================
// COMPONENT PROP TYPES
// ============================================

/**
 * Glass panel variants
 */
export type GlassPanelVariant = "default" | "dark" | "primary" | "transparent";

/**
 * Map control button props
 */
export interface MapControlButtonProps {
  tooltip?: string;
  active?: boolean;
  variant?: "default" | "rounded";
  size?: "sm" | "md" | "lg";
  disabled?: boolean;
  onClick?: () => void;
  className?: string;
  children?: React.ReactNode;
}

/**
 * Layer control panel props
 */
export interface LayerControlsProps {
  layers: LayerConfig[];
  onVisibilityChange?: (layerId: string, visible: boolean) => void;
  onOpacityChange?: (layerId: string, opacity: number) => void;
  className?: string;
}

/**
 * Filter panel props
 */
export interface FilterPanelProps {
  filters: AdminMapFilters;
  onFiltersChange: (filters: AdminMapFilters) => void;
  providers?: ProviderOption[];
  className?: string;
}

// ============================================
// CONTEXT TYPES
// ============================================

/**
 * Admin map context value
 */
export interface AdminMapContextValue {
  /** Current layer state */
  layerState: LayerState;
  /** Current filter state */
  filters: AdminMapFilters;
  /** Set layer visibility */
  setLayerVisibility: (layerId: string, visible: boolean) => void;
  /** Set layer opacity */
  setLayerOpacity: (layerId: string, opacity: number) => void;
  /** Update filters */
  updateFilters: (filters: Partial<AdminMapFilters>) => void;
  /** Reset all filters to defaults */
  resetFilters: () => void;
  /** Currently selected feature ID (for details panel) */
  selectedFeatureId?: string;
  /** Set selected feature */
  setSelectedFeatureId: (id?: string) => void;
  /** Whether the panel is collapsed */
  isPanelCollapsed: boolean;
  /** Toggle panel collapse state */
  togglePanelCollapsed: () => void;
}

// ============================================
// EVENT TYPES
// ============================================

/**
 * Map click event data
 */
export interface MapClickEventData {
  layerId: string;
  featureId: string | number;
  properties: Record<string, unknown>;
  coordinates: [number, number];
}

/**
 * Layer loading progress
 */
export interface LayerLoadingProgress {
  layerId: string;
  current: number;
  total: number;
  percentage: number;
  isComplete: boolean;
}

// ============================================
// UTILITY TYPES
// ============================================

/**
 * Viewport bounds for data queries
 */
export interface ViewportBounds {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
  zoom?: number;
}

/**
 * GeoJSON Feature type for map layers
 */
export type MapGeoJSONFeature = GeoJSON.Feature<GeoJSON.Geometry, Record<string, unknown>>;

/**
 * GeoJSON Feature Collection type
 */
export type MapGeoJSONFeatureCollection = GeoJSON.FeatureCollection<GeoJSON.Geometry, Record<string, unknown>>;
