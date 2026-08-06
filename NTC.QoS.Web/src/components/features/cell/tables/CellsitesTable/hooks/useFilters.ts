import { useCallback } from "react";

interface FilterActions {
  onRegionFilterChange: (value: string) => void;
  onStatusFilterChange: (value: string) => void;
  onProviderFilterChange: (value: string) => void;
  onProvinceFilterChange: (value: string) => void;
  onMunicipalityFilterChange: (value: string) => void;
  onBarangayFilterChange: (value: string) => void;
}

export function useFilters(regionId: string | undefined, actions: FilterActions) {
  const isRegionPinned = !!regionId;

  const clearAllFilters = useCallback(() => {
    actions.onStatusFilterChange("all");
    actions.onProviderFilterChange("all");
    actions.onProvinceFilterChange("all");
    actions.onMunicipalityFilterChange("all");
    actions.onBarangayFilterChange("all");
    if (!isRegionPinned) {
      actions.onRegionFilterChange("all");
    }
  }, [actions, isRegionPinned]);

  return { clearAllFilters, isRegionPinned };
}
