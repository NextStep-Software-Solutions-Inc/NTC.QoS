"use client";

import React, { useCallback, useMemo } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { Activity } from "lucide-react";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useLocationMap } from "@/providers/LocationMapContext";
import { getSpeedtestsRequest } from "./handlers/getSpeedtestsRequest";
import { SpeedtestDashboard } from "./SpeedtestDashboard";
import { SpeedtestPageSkeleton } from "./SpeedtestPageSkeleton";

// Create the query hook for fetching speedtests
const useSpeedtestsQuery = createQueryHookFromTemplate(getSpeedtestsRequest);

interface ManageSpeedtestsProps {
  regionId: string;
}

// Filter state type for URL persistence
export interface SpeedtestFilters {
  provider?: string;
  province?: string;
  municipality?: string;
  barangay?: string;
  signalQuality?: string;
  search?: string;
  nbc?: string;
}

/**
 * ManageSpeedtests - Page wrapper component for speedtest management
 *
 * Handles data fetching with React Query and passes data to SpeedtestDashboard
 * Now with full-width layout and dynamic pagination
 */
export function ManageSpeedtests({ regionId }: ManageSpeedtestsProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();

  // Resolve region name from LocationMapContext
  const { resolveUuid } = useLocationMap();
  const regionName = resolveUuid(regionId) || "All Regions";

  // Parse pagination state from URL params (with defaults)
  const pageSize = parseInt(searchParams.get("pageSize") || "50", 10);
  const pageNumber = parseInt(searchParams.get("page") || "1", 10);

  // Parse filter state from URL params
  const filters: SpeedtestFilters = useMemo(
    () => ({
      provider: searchParams.get("provider") || undefined,
      province: searchParams.get("province") || undefined,
      municipality: searchParams.get("municipality") || undefined,
      barangay: searchParams.get("barangay") || undefined,
      signalQuality: searchParams.get("signalQuality") || undefined,
      search: searchParams.get("search") || undefined,
      nbc: searchParams.get("nbc") || undefined,
    }),
    [searchParams],
  );

  // Helper to update URL params
  const updateUrlParams = useCallback(
    (updates: Record<string, string | number | undefined>) => {
      const params = new URLSearchParams(searchParams.toString());
      Object.entries(updates).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== "" && value !== "all") {
          params.set(key, String(value));
        } else {
          params.delete(key);
        }
      });
      router.push(`?${params.toString()}`, { scroll: false });
    },
    [searchParams, router],
  );

  // Filter change handler - updates URL and resets to page 1
  const handleFilterChange = useCallback(
    (newFilters: Partial<SpeedtestFilters>) => {
      updateUrlParams({ ...newFilters, page: 1 });
    },
    [updateUrlParams],
  );

  // Build query params including filters
  // NOTE: search is NOT sent to backend - client-side filtering handles search to ensure reliability
  const queryParams = useMemo(
    () => ({
      pageSize,
      pageNumber,
      regionIds: regionId ? [regionId] : undefined,
      serviceProviderIds: filters.provider && filters.provider !== "all" ? [filters.provider] : undefined,
      provinceIds: filters.province && filters.province !== "all" ? [filters.province] : undefined,
      municipalityIds: filters.municipality && filters.municipality !== "all" ? [filters.municipality] : undefined,
      barangayIds: filters.barangay && filters.barangay !== "all" ? [filters.barangay] : undefined,
      signalQuality: filters.signalQuality && filters.signalQuality !== "all" ? filters.signalQuality : undefined,
      isNoBroadbandCoverage:
        filters.nbc === "nbc" ? true : filters.nbc === "non-nbc" ? false : undefined,
      // search: filters.search || undefined, // REMOVED - handled client-side for better UX
    }),
    [regionId, pageSize, pageNumber, filters],
  );

  // Fetch speedtests
  const { data: response, isLoading, isFetching, refetch, error } = useSpeedtestsQuery({ pathParams: {}, queryParams });

  // Extract speedtests and pagination from response
  const speedtests = response?.data ?? [];
  const totalCount = response?.totalRows ?? 0;
  const totalPages = response?.totalPageNumber ?? 1;

  // Derive if data has ever loaded based on response existing and having data OR not loading anymore
  // We show full-screen loading only when we haven't received any data yet AND we're currently loading
  const hasReceivedData = response !== undefined && (speedtests.length > 0 || totalCount > 0);

  // Show full-screen loading only on initial load (before any data has been fetched)
  const showFullscreenLoading = isLoading && !hasReceivedData;

  // Pagination handlers - update URL params
  const handlePageChange = useCallback(
    (page: number) => {
      updateUrlParams({ page });
    },
    [updateUrlParams],
  );

  const handlePageSizeChange = useCallback(
    (size: number) => {
      updateUrlParams({ pageSize: size, page: 1 }); // Reset to first page when changing page size
    },
    [updateUrlParams],
  );

  // Handle refresh
  const handleRefresh = useCallback(async () => {
    await refetch();
    // Also invalidate analytics queries to refresh analytics panel (matching NAP pattern)
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "speedtest-analytics", refetchType: "active" });
  }, [refetch, queryClient]);

  // Loading state - full width (only on initial load)
  if (showFullscreenLoading) {
    return <SpeedtestPageSkeleton />;
  }

  // Error state - full width
  if (error) {
    return (
      <div className="flex flex-col h-full w-full px-4">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="flex flex-col items-center justify-center min-h-[400px] gap-4">
          <div className="rounded-xl bg-red-100 dark:bg-red-900/20 p-6 text-center max-w-md">
            <div className="rounded-full bg-red-500 p-3 w-fit mx-auto mb-4">
              <Activity className="h-6 w-6 text-white" />
            </div>
            <h2 className="text-xl font-semibold text-red-800 dark:text-red-200 mb-2">Failed to Load Data</h2>
            <p className="text-sm text-red-600 dark:text-red-300 mb-4">
              {error instanceof Error ? error.message : "An error occurred while fetching speedtest data."}
            </p>
            <button
              onClick={() => refetch()}
              className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors">
              Try Again
            </button>
          </div>
        </motion.div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full w-full overflow-hidden">
      <SpeedtestDashboard
        speedtests={speedtests}
        totalCount={totalCount}
        currentPage={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        onPageChange={handlePageChange}
        onPageSizeChange={handlePageSizeChange}
        regionId={regionId}
        regionName={regionName}
        isLoading={isFetching}
        onRefresh={handleRefresh}
        filters={filters}
        onFilterChange={handleFilterChange}
      />
    </div>
  );
}

export default ManageSpeedtests;
