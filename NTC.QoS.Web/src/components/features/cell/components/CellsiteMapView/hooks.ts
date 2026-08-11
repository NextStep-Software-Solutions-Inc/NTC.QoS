import { useMemo } from "react";

import { DEFAULT_PROVIDER_COLOR, getProviderIconId, UNKNOWN_PROVIDER } from "./constants";
import {
  type CellMapDetails,
  type CellsiteMapFilters,
  type CellWithOptimistic,
  type OptimisticCell,
  type ProviderCounts,
} from "./types";

/**
 * Hook to merge real cells with optimistic cells
 */
export function useMergedCells(cells: CellMapDetails[], optimisticCells: OptimisticCell[]): CellWithOptimistic[] {
  return useMemo(() => {
    const cellMap = new Map<string, CellWithOptimistic>();
    cells.forEach((cell) => cellMap.set(cell.id, cell));
    optimisticCells.forEach((cell) => cellMap.set(cell.id, cell as CellWithOptimistic));
    return Array.from(cellMap.values());
  }, [cells, optimisticCells]);
}

/**
 * Hook to filter cells by search query and visible providers
 */
export function useFilteredCells(
  allCells: CellWithOptimistic[],
  visibleProviders: Set<string>,
  _searchQuery: string,
): CellWithOptimistic[] {
  return useMemo(() => {
    return allCells.filter((cell) => {
      const provider = cell.providerName || UNKNOWN_PROVIDER;
      return visibleProviders.has(provider);
    });
  }, [allCells, visibleProviders]);
}

/**
 * Hook to filter cells by advanced filters
 */
export function useAdvancedFilteredCells(
  cells: CellWithOptimistic[],
  filters: CellsiteMapFilters,
): CellWithOptimistic[] {
  return useMemo(() => {
    return cells.filter((cell) => {
      // Generation filter (if cell has generation data)
      if (filters.generations.length > 0 && cell.generation && !filters.generations.includes(cell.generation)) {
        return false;
      }
      // Band filter (if cell has band data)
      if (filters.bands.length > 0 && cell.band && !filters.bands.includes(cell.band)) {
        return false;
      }
      // Technology filter (if cell has technology data)
      if (filters.technologies.length > 0 && cell.technology && !filters.technologies.includes(cell.technology)) {
        return false;
      }
      // Search filter
      if (filters.search) {
        const query = filters.search.toLowerCase();
        return `${cell.lat}, ${cell.lng}`.includes(query);
      }
      return true;
    });
  }, [cells, filters]);
}

/**
 * Hook to generate GeoJSON from cells
 */
export function useCellsGeoJSON(
  filteredCells: CellWithOptimistic[],
  selectedCellId?: string | null,
): GeoJSON.FeatureCollection {
  return useMemo(() => {
    return {
      type: "FeatureCollection" as const,
      features: filteredCells.map((cell) => ({
        type: "Feature" as const,
        geometry: { type: "Point" as const, coordinates: [cell.lng, cell.lat] as [number, number] },
        properties: {
          id: cell.id,
          name: cell.name,
          providerName: cell.providerName || UNKNOWN_PROVIDER,
          providerColor: cell.providerColor || DEFAULT_PROVIDER_COLOR,
          providerIcon: getProviderIconId(cell.providerName || UNKNOWN_PROVIDER),
          status: cell.status,
          lat: cell.lat,
          lng: cell.lng,
          isSelected: cell.id === selectedCellId,
          isPending: cell.isPending || false,
          hasError: cell.hasError || false,
          siteId: cell.siteId,
          towerType: cell.towerType,
          isVerified: cell.isVerified,
        },
      })),
    };
  }, [filteredCells, selectedCellId]);
}

/**
 * Hook to count cells by provider
 */
export function useProviderCounts(allCells: CellWithOptimistic[], visibleProviders: Set<string>): ProviderCounts {
  return useMemo(() => {
    const counts: ProviderCounts = {};
    allCells.forEach((cell) => {
      const providerName = cell.providerName || UNKNOWN_PROVIDER;
      const color = cell.providerColor || DEFAULT_PROVIDER_COLOR;
      if (!counts[providerName]) {
        counts[providerName] = { count: 0, color, visible: visibleProviders.has(providerName) };
      }
      counts[providerName].count++;
    });
    return counts;
  }, [allCells, visibleProviders]);
}

/**
 * Hook to extract unique filter values from cells
 */
export function useAvailableFilters(allCells: CellWithOptimistic[]): {
  providers: string[];
  generations: string[];
  bands: string[];
  technologies: string[];
} {
  return useMemo(() => {
    const providers = new Set<string>();
    const generations = new Set<string>();
    const bands = new Set<string>();
    const technologies = new Set<string>();

    allCells.forEach((cell) => {
      if (cell.providerName) providers.add(cell.providerName);
      if (cell.generation) generations.add(cell.generation);
      if (cell.band) bands.add(cell.band);
      if (cell.technology) technologies.add(cell.technology);
    });

    return {
      providers: Array.from(providers).sort(),
      generations: Array.from(generations).sort(),
      bands: Array.from(bands).sort(),
      technologies: Array.from(technologies).sort(),
    };
  }, [allCells]);
}
