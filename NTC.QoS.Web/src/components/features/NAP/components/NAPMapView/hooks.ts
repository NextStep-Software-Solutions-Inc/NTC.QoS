import { useMemo } from "react";

import { type NAPDetails } from "@/components/features/NAP/types/NAP";
import { DEFAULT_PROVIDER_COLOR, UNKNOWN_PROVIDER } from "./constants";
import { type NAPWithOptimistic, type OptimisticNAP, type ProviderCounts } from "./types";

/**
 * Hook to merge real NAPs with optimistic NAPs
 */
export function useMergedNAPs(naps: NAPDetails[], optimisticNAPs: OptimisticNAP[]): NAPWithOptimistic[] {
  return useMemo(() => {
    const napMap = new Map<string, NAPWithOptimistic>();
    naps.forEach((nap) => napMap.set(nap.id, nap));
    optimisticNAPs.forEach((nap) => napMap.set(nap.id, nap as NAPWithOptimistic));
    return Array.from(napMap.values());
  }, [naps, optimisticNAPs]);
}

/**
 * Hook to filter NAPs by visible providers.
 * Search is coordinate-navigation-only; it does not filter the rendered NAPs.
 */
export function useFilteredNAPs(
  allNAPs: NAPWithOptimistic[],
  visibleProviders: Set<string>,
  _searchQuery: string,
): NAPWithOptimistic[] {
  return useMemo(() => {
    return allNAPs.filter((nap) => {
      const provider = nap.ISP || UNKNOWN_PROVIDER;
      return visibleProviders.has(provider);
    });
  }, [allNAPs, visibleProviders]);
}

/**
 * Hook to generate GeoJSON from NAPs
 */
export function useNAPsGeoJSON(
  filteredNAPs: NAPWithOptimistic[],
  selectedNAPId?: string | null,
): GeoJSON.FeatureCollection {
  return useMemo(() => {
    return {
      type: "FeatureCollection" as const,
      features: filteredNAPs.map((nap) => ({
        type: "Feature" as const,
        geometry: { type: "Point" as const, coordinates: [nap.lng, nap.lat] as [number, number] },
        properties: {
          id: nap.id,
          isp: nap.ISP || UNKNOWN_PROVIDER,
          ispColor: nap.ISPColor || DEFAULT_PROVIDER_COLOR,
          lat: nap.lat,
          lng: nap.lng,
          isSelected: nap.id === selectedNAPId,
          isPending: nap.isPending || false,
          hasError: nap.hasError || false,
        },
      })),
    };
  }, [filteredNAPs, selectedNAPId]);
}

/**
 * Hook to count NAPs by provider
 */
export function useProviderCounts(allNAPs: NAPWithOptimistic[], visibleProviders: Set<string>): ProviderCounts {
  return useMemo(() => {
    const counts: ProviderCounts = {};
    allNAPs.forEach((nap) => {
      const providerName = nap.ISP || UNKNOWN_PROVIDER;
      const color = nap.ISPColor || DEFAULT_PROVIDER_COLOR;
      if (!counts[providerName]) {
        counts[providerName] = { count: 0, color, visible: visibleProviders.has(providerName) };
      }
      counts[providerName].count++;
    });
    return counts;
  }, [allNAPs, visibleProviders]);
}
