/**
 * Shared filter types for broadband features (Cellsites, NAPs, etc.)
 * These types are reusable across different broadband management pages.
 */

export interface FilterOption {
  id: string;
  name: string;
}

export interface BroadbandFilterState {
  searchTerm: string;
  debouncedSearchTerm: string;
  statusFilter: string;
  verifiedFilter: string;
  providerFilter: string;
  provinceFilter: string;
  municipalityFilter: string;
  barangayFilter: string;
  regionFilter: string;
  currentPage: number;
  pageSize: number;
}

export interface BroadbandFilterActions {
  setSearchTerm: (value: string) => void;
  setStatusFilter: (value: string) => void;
  setVerifiedFilter: (value: string) => void;
  setProviderFilter: (value: string) => void;
  setProvinceFilter: (value: string) => void;
  setMunicipalityFilter: (value: string) => void;
  setBarangayFilter: (value: string) => void;
  setRegionFilter: (value: string) => void;
  setCurrentPage: (page: number) => void;
  setPageSize: (size: number) => void;
  clearAllFilters: () => void;
}

export interface BroadbandFilterOptions {
  providers: FilterOption[];
  provinces: FilterOption[];
  municipalities: FilterOption[];
  barangays: FilterOption[];
  regions: FilterOption[];
}

export interface UseBroadbandFiltersConfig {
  regionId: string;
  basePath: string; // e.g., "broadband/cellsites" or "broadband/naps"
  defaultPageSize?: number;
}

/**
 * Provider statistics entry
 */
export interface ProviderStats {
  id: string;
  name: string;
  color: string;
  count: number;
}

/**
 * Mock statistics structure for backend API implementation
 * GET /api/v1/cells/statistics or GET /api/v1/naps/statistics
 */
export interface BroadbandStatistics {
  totalCount: number;
  verifiedCount: number;
  unverifiedCount: number;
  /** Top 3 providers by cell site count (data-driven, not hardcoded) */
  topProviders: ProviderStats[];
  statusBreakdown: { pending: number; unknown: number; licensed: number };
}
