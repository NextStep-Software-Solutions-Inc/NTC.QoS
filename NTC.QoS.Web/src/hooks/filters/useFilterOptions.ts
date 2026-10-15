"use client";

import { useMemo } from "react";

import { getBarangaysRequest } from "@/components/features/barangay/handlers/getBarangaysRequest";
import { getMunicipalitiesRequest } from "@/components/features/municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "@/components/features/province/handlers/getProvincesRequest";
import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import type { BroadbandFilterOptions, FilterOption } from "./types";

// Create hooks
const useGetProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useGetMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
const useGetBarangays = createQueryHookFromTemplate(getBarangaysRequest);
const useGetRegions = createQueryHookFromTemplate(getRegionsRequest);

interface UseFilterOptionsConfig {
  regionId: string;
  provinceFilter?: string;
  municipalityFilter?: string;
  serviceType: "wired" | "wireless" | "all";
}

/**
 * Reusable hook for fetching cascading filter options.
 * Province → Municipality → Barangay
 */
export function useFilterOptions(
  config: UseFilterOptionsConfig,
): BroadbandFilterOptions & { isLoading: boolean; currentRegionName: string } {
  const { regionId, provinceFilter = "all", municipalityFilter = "all" } = config;

  // Fetch filter options
  const { data: providersResponse, isLoading: providersLoading } = useGetProviders({
    pathParams: {},
    queryParams: {
      pageSize: 1000,
      pageNumber: 1,
      serviceTypes: config.serviceType === "all" ? undefined : [config.serviceType],
    },
  });

  const { data: regionsResponse, isLoading: regionsLoading } = useGetRegions({
    enabled: !!regionId,
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1 },
  });

  const { data: provincesResponse, isLoading: provincesLoading } = useGetProvinces({
    enabled: !!regionId,
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, regionIds: regionId ? [regionId] : [] },
  });

  const { data: municipalitiesResponse, isLoading: municipalitiesLoading } = useGetMunicipalities({
    enabled: provinceFilter !== "all",
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, provinceId: provinceFilter !== "all" ? provinceFilter : undefined },
  });

  const { data: barangaysResponse, isLoading: barangaysLoading } = useGetBarangays({
    enabled: municipalityFilter !== "all",
    pathParams: {},
    queryParams: {
      pageSize: 1000,
      pageNumber: 1,
      municipalityIds: municipalityFilter !== "all" ? [municipalityFilter] : undefined,
    },
  });

  // Transform filter options
  const providers: FilterOption[] = useMemo(
    () => (providersResponse?.data || []).map((p) => ({ id: p.id, name: p.provider?.name || p.id })),
    [providersResponse],
  );

  const regions: FilterOption[] = useMemo(
    () => (regionsResponse?.data || []).map((r) => ({ id: r.id, name: r.name })),
    [regionsResponse],
  );

  const provinces: FilterOption[] = useMemo(
    () => (provincesResponse?.data || []).map((p) => ({ id: p.id, name: p.name })),
    [provincesResponse],
  );

  const municipalities: FilterOption[] = useMemo(
    () => (municipalitiesResponse?.data || []).map((m) => ({ id: m.id, name: m.name })),
    [municipalitiesResponse],
  );

  const barangays: FilterOption[] = useMemo(
    () => (barangaysResponse?.data || []).map((b) => ({ id: b.id, name: b.name })),
    [barangaysResponse],
  );

  // Find current region name
  const currentRegionName = useMemo(
    () => regions.find((r) => r.id === regionId)?.name || regionId,
    [regions, regionId],
  );

  const isLoading = providersLoading || regionsLoading || provincesLoading || municipalitiesLoading || barangaysLoading;

  return { providers, provinces, municipalities, barangays, regions, currentRegionName, isLoading };
}
