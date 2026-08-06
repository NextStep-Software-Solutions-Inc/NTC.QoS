"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

import type { BroadbandFilterActions, BroadbandFilterState, UseBroadbandFiltersConfig } from "./types";

/**
 * Reusable hook for managing broadband filter state with URL persistence.
 * Can be used for Cellsites, NAPs, and other broadband features.
 */
export function useBroadbandFilters(config: UseBroadbandFiltersConfig): BroadbandFilterState & BroadbandFilterActions {
  const { regionId, basePath, defaultPageSize = 20 } = config;
  const router = useRouter();
  const searchParams = useSearchParams();

  // Initialize state from URL params with validation
  const [currentPage, setCurrentPage] = useState(() => {
    const page = parseInt(searchParams.get("page") || "1");
    return isNaN(page) || page < 1 ? 1 : page;
  });

  const [pageSize, setPageSize] = useState(() => {
    const size = parseInt(searchParams.get("pageSize") || String(defaultPageSize));
    const allowedSizes = [10, 20, 50, 100];
    return allowedSizes.includes(size) ? size : defaultPageSize;
  });

  const [searchTerm, setSearchTermState] = useState(() => searchParams.get("search") || "");

  const [statusFilter, setStatusFilterState] = useState(() => searchParams.get("status") || "all");

  const [verifiedFilter, setVerifiedFilterState] = useState(() => searchParams.get("verified") || "all");

  const [providerFilter, setProviderFilterState] = useState(() => searchParams.get("provider") || "all");

  const [provinceFilter, setProvinceFilterState] = useState(() => searchParams.get("province") || "all");

  const [municipalityFilter, setMunicipalityFilterState] = useState(() => searchParams.get("municipality") || "all");

  const [barangayFilter, setBarangayFilterState] = useState(() => searchParams.get("barangay") || "all");

  // Region filter is pinned to the URL param regionId
  const [regionFilter, setRegionFilter] = useState(regionId);

  // Debounced search term for API calls
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(searchTerm);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Update URL when filters change
  const updateURL = useCallback(() => {
    const params = new URLSearchParams();
    if (currentPage > 1) params.set("page", currentPage.toString());
    if (pageSize !== defaultPageSize) params.set("pageSize", pageSize.toString());
    if (debouncedSearchTerm) params.set("search", debouncedSearchTerm);
    if (statusFilter !== "all") params.set("status", statusFilter);
    if (verifiedFilter !== "all") params.set("verified", verifiedFilter);
    if (providerFilter !== "all") params.set("provider", providerFilter);
    if (provinceFilter !== "all") params.set("province", provinceFilter);
    if (municipalityFilter !== "all") params.set("municipality", municipalityFilter);
    if (barangayFilter !== "all") params.set("barangay", barangayFilter);

    const queryString = params.toString();
    router.push(`/${regionId}/${basePath}${queryString ? `?${queryString}` : ""}`, { scroll: false });
  }, [
    currentPage,
    pageSize,
    defaultPageSize,
    debouncedSearchTerm,
    statusFilter,
    verifiedFilter,
    providerFilter,
    provinceFilter,
    municipalityFilter,
    barangayFilter,
    regionId,
    basePath,
    router,
  ]);

  // Update URL when state changes
  useEffect(() => {
    updateURL();
  }, [updateURL]);

  // Action handlers with cascading reset
  const setSearchTerm = useCallback((value: string) => {
    setSearchTermState(value);
    setCurrentPage(1);
  }, []);

  const setStatusFilter = useCallback((value: string) => {
    setStatusFilterState(value);
    setCurrentPage(1);
  }, []);

  const setVerifiedFilter = useCallback((value: string) => {
    setVerifiedFilterState(value);
    setCurrentPage(1);
  }, []);

  const setProviderFilter = useCallback((value: string) => {
    setProviderFilterState(value);
    setCurrentPage(1);
  }, []);

  const setProvinceFilter = useCallback((value: string) => {
    setProvinceFilterState(value);
    setMunicipalityFilterState("all");
    setBarangayFilterState("all");
    setCurrentPage(1);
  }, []);

  const setMunicipalityFilter = useCallback((value: string) => {
    setMunicipalityFilterState(value);
    setBarangayFilterState("all");
    setCurrentPage(1);
  }, []);

  const setBarangayFilter = useCallback((value: string) => {
    setBarangayFilterState(value);
    setCurrentPage(1);
  }, []);

  const clearAllFilters = useCallback(() => {
    setSearchTermState("");
    setStatusFilterState("all");
    setVerifiedFilterState("all");
    setProviderFilterState("all");
    setProvinceFilterState("all");
    setMunicipalityFilterState("all");
    setBarangayFilterState("all");
    setCurrentPage(1);
  }, []);

  return {
    // State
    searchTerm,
    debouncedSearchTerm,
    statusFilter,
    verifiedFilter,
    providerFilter,
    provinceFilter,
    municipalityFilter,
    barangayFilter,
    regionFilter,
    currentPage,
    pageSize,
    // Actions
    setSearchTerm,
    setStatusFilter,
    setVerifiedFilter,
    setProviderFilter,
    setProvinceFilter,
    setMunicipalityFilter,
    setBarangayFilter,
    setRegionFilter,
    setCurrentPage,
    setPageSize,
    clearAllFilters,
  };
}
