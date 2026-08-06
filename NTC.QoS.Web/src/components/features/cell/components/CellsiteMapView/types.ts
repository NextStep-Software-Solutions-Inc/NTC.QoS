import { type Cell } from "@/components/features/cell/types/Cell";

/**
 * Map interaction modes
 */
export type MapInteractionMode = "view" | "add" | "edit" | "delete";

/**
 * Layer visibility configuration
 */
export interface LayerConfig {
  cellSites: boolean;
  cellSiteHeatmap: boolean;
  signalCoverage: boolean;
  pointView: boolean;
}

/**
 * Layer opacity configuration
 */
export interface LayerOpacity {
  cellSites: number;
  cellSiteHeatmap: number;
  signalCoverage: number;
  pointView: number;
}

/**
 * Simplified cell details for map rendering
 */
export type CellMapDetails = {
  id: string;
  name: string;
  lat: number;
  lng: number;
  providerName: string;
  providerColor: string;
  status: string;
  siteId?: string;
  towerType?: string;
  elevation?: number;
  isVerified?: boolean;
  serviceProviderId?: string;
  generation?: string;
  band?: string;
  technology?: string;
};

/**
 * Optimistic cell for UI updates
 */
export interface OptimisticCell extends CellMapDetails {
  isPending?: boolean;
  hasError?: boolean;
}

/**
 * Cell with optimistic flags
 */
export type CellWithOptimistic = CellMapDetails & { isPending?: boolean; hasError?: boolean };

/**
 * Provider counts for legend
 */
export interface ProviderCounts {
  [providerName: string]: { count: number; color: string; visible: boolean };
}

/**
 * Filter state for cellsite map
 */
export interface CellsiteMapFilters {
  providers: string[];
  generations: string[];
  bands: string[];
  technologies: string[];
  search?: string;
}

export interface AdvancedMapFilterSelection {
  generations: string[];
  bands: string[];
  technologies: string[];
}

export interface CellsiteMapFilterOptions {
  generations?: string[];
  bands?: string[];
  technologies?: string[];
}

/**
 * Signal strength filter options
 */
export type SignalStrengthFilter = "all" | "excellent" | "good" | "fair" | "poor" | "veryPoor";

/**
 * Signal strength filter configuration
 */
export interface SignalStrengthFilterConfig {
  id: SignalStrengthFilter;
  label: string;
  color: string;
  minRssi: number;
  maxRssi: number;
}

/**
 * Cellular coverage data point (from all_telco_band_data_flat.json)
 */
export interface CellularCoveragePoint {
  lat: number;
  lng: number;
  strength: number;
  barangay?: string;
  azimuth?: string;
}

/**
 * Props for CellsiteMapView component
 */
export interface CellsiteMapViewProps {
  cells: CellMapDetails[];
  className?: string;
  isExpanded?: boolean;
  onToggleExpand?: () => void;
  onAddCell?: (lat: number, lng: number) => void;
  onEditCell?: (cell: CellMapDetails) => void;
  onDeleteCell?: (cell: CellMapDetails) => void;
  onSelectCell?: (cell: CellMapDetails | null) => void;
  onViewCellDetails?: (cellId: string) => void;
  onAdvancedFiltersChange?: (filters: AdvancedMapFilterSelection) => void;
  filterOptions?: CellsiteMapFilterOptions;
  selectedCellId?: string | null;
  optimisticCells?: OptimisticCell[];
  regionId?: string;
  focusedLocation?: { lat: number; lng: number } | null;
}

function extractString(value: unknown): string | undefined {
  if (typeof value === "string") {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }
  return undefined;
}

function extractFirstString(values: unknown[]): string | undefined {
  for (const value of values) {
    const extracted = extractString(value);
    if (extracted) return extracted;
  }
  return undefined;
}

function extractNestedName(obj: unknown): string | undefined {
  if (!obj || typeof obj !== "object") return undefined;
  const value = obj as Record<string, unknown>;
  return extractFirstString([value.name, value.standard, value.label]);
}

function extractFromCollection(collection: unknown): string | undefined {
  if (!Array.isArray(collection) || collection.length === 0) return undefined;

  const first = collection[0];
  if (typeof first === "string") {
    return extractString(first);
  }

  if (first && typeof first === "object") {
    const item = first as Record<string, unknown>;
    const nestedGeneration = extractNestedName(item.generation);
    const nestedBand = extractNestedName(item.band);
    const nestedTechnology = extractNestedName(item.technology);
    return extractFirstString([
      item.generationName,
      item.bandName,
      item.technologyName,
      item.standard,
      item.name,
      nestedGeneration,
      nestedBand,
      nestedTechnology,
    ]);
  }

  return undefined;
}

/**
 * Convert full Cell to CellMapDetails for map rendering
 */
export function cellToCellMapDetails(cell: Cell): CellMapDetails {
  const candidate = cell as unknown as Record<string, unknown>;

  const generation = extractFirstString([
    candidate.generationName,
    extractNestedName(candidate.generation),
    extractNestedName(candidate.systemType && typeof candidate.systemType === "object"
      ? (candidate.systemType as Record<string, unknown>).generation
      : undefined),
    extractNestedName(
      candidate.technology && typeof candidate.technology === "object"
        ? (candidate.technology as Record<string, unknown>).generation
        : undefined,
    ),
    extractFromCollection(candidate.technologies),
    extractFromCollection(candidate.cellTechnologies),
  ]);

  const technology = extractFirstString([
    candidate.technologyStandard,
    extractNestedName(candidate.technology),
    extractNestedName(candidate.systemType && typeof candidate.systemType === "object"
      ? (candidate.systemType as Record<string, unknown>).technology
      : undefined),
    extractFromCollection(candidate.technologies),
    extractFromCollection(candidate.cellTechnologies),
  ]);

  const band = extractFirstString([
    candidate.bandName,
    extractNestedName(candidate.band),
    extractFromCollection(candidate.bands),
    extractFromCollection(candidate.cellBands),
  ]);

  return {
    id: cell.id,
    name: cell.name || "Unknown",
    lat: cell.latitude,
    lng: cell.longitude,
    providerName: cell.serviceProvider?.provider?.name || "Unknown",
    providerColor: cell.serviceProvider?.provider?.color || "#6b7280",
    status: cell.status || "Unknown",
    siteId: cell.siteId,
    towerType: cell.towerType,
    elevation: cell.elevation,
    isVerified: cell.isVerified,
    serviceProviderId: cell.serviceProviderId,
    generation,
    technology,
    band,
  };
}
