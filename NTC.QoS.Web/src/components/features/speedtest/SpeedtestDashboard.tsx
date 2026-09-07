"use client";

import React, { useCallback, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { ChevronDown, Download, FileSpreadsheet, FileText, Map, Plus, RefreshCw } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { cn } from "@/lib/utils";
import { SpeedtestAnalyticsPanel } from "./analytics/SpeedtestAnalyticsPanel";
import { SpeedtestAnalyticsPanelSkeleton } from "./analytics/SpeedtestAnalyticsPanelSkeleton";
import { SpeedtestHero } from "./components/SpeedtestHero";
import { SpeedtestHeroSkeleton } from "./components/SpeedtestHeroSkeleton";
import { SpeedtestSeeder } from "./dev/SpeedtestSeeder";
import { CreateSpeedtestDialog, DeleteSpeedtestDialog, EditSpeedtestDialog } from "./dialogs";
import { deleteSpeedtestRequest } from "./handlers/deleteSpeedtestRequest";
import { type SpeedtestFilters } from "./ManageSpeedtests";
import { SpeedtestTable } from "./tables/SpeedtestTable";
import { type SpeedTest } from "./types/SpeedTest";

interface SpeedtestDashboardProps {
  speedtests: SpeedTest[];
  totalCount?: number;
  currentPage?: number;
  pageSize?: number;
  totalPages?: number;
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  regionId?: string;
  regionName?: string;
  isLoading?: boolean;
  onRefresh?: () => void;
  filters?: SpeedtestFilters;
  onFilterChange?: (filters: Partial<SpeedtestFilters>) => void;
  className?: string;
}

// Create deletion hook
const useDeleteSpeedtest = createMutationHookFromTemplate(deleteSpeedtestRequest);

/**
 * SpeedtestDashboard - Main management component for speedtest data
 *
 * Features:
 * - Animated hero section with stats
 * - Analytics dashboard with provider performance charts
 * - Filterable and searchable speedtest table
 * - CRUD operations via modal dialogs
 * - Export functionality (CSV/PDF)
 * - Batch operations support
 * - Full-width layout
 */
export function SpeedtestDashboard({
  speedtests,
  totalCount = 0,
  currentPage = 1,
  pageSize = 50,
  totalPages = 1,
  onPageChange,
  onPageSizeChange,
  regionId,
  regionName = "All Regions",
  isLoading = false,
  onRefresh,
  filters,
  onFilterChange,
  className,
}: SpeedtestDashboardProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  
  // Deletion mutation
  const deleteSpeedtestMutation = useDeleteSpeedtest();
  
  // Dialog states
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  // Selected speedtest for edit/delete
  const [selectedSpeedtest, setSelectedSpeedtest] = useState<SpeedTest | null>(null);
  const [selectedSpeedtests, setSelectedSpeedtests] = useState<SpeedTest[]>([]);

  // Hero collapse state
  const [heroCollapsed, setHeroCollapsed] = useState(false);

  // Analytics collapse state - default to collapsed for consistent UX with cell site and NAP dashboards
  const [analyticsCollapsed, setAnalyticsCollapsed] = useState(true);

  // Scroll container ref for hero auto-collapse
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  // Refresh state
  const [isRefreshing, setIsRefreshing] = useState(false);

  // Memoize queryParams for analytics to prevent unnecessary re-renders and stabilize query keys
  const analyticsQueryParams = React.useMemo(
    () => ({
      regionIds: regionId ? [regionId] : undefined,
      provinceIds: filters?.province && filters.province !== "all" ? [filters.province] : undefined,
      municipalityIds: filters?.municipality && filters.municipality !== "all" ? [filters.municipality] : undefined,
      barangayIds: filters?.barangay && filters.barangay !== "all" ? [filters.barangay] : undefined,
    }),
    [regionId, filters?.province, filters?.municipality, filters?.barangay],
  );

  // Calculate statistics for hero
  const statistics = React.useMemo(() => {
    if (speedtests.length === 0) return undefined;

    const avgDownload = speedtests.reduce((sum, st) => sum + (st.downloadSpeed || 0), 0) / speedtests.length;
    const avgUpload = speedtests.reduce((sum, st) => sum + (st.uploadSpeed || 0), 0) / speedtests.length;
    const avgPing = speedtests.reduce((sum, st) => sum + (st.ping || 0), 0) / speedtests.length;

    // Count by signal quality - using actual API values
    const excellent = speedtests.filter((st) => st.signalQuality === "Excellent").length;
    const good = speedtests.filter((st) => st.signalQuality === "Good").length;
    const average = speedtests.filter((st) => st.signalQuality === "Average").length;
    const weak = speedtests.filter((st) => st.signalQuality === "Weak" || st.signalQuality === "VeryWeak").length;

    return {
      totalCount: totalCount || speedtests.length,
      avgDownload,
      avgUpload,
      avgPing,
      excellent,
      good,
      average,
      weak,
    };
  }, [speedtests, totalCount]);

  // Handlers
  const handleRefresh = useCallback(() => {
    setIsRefreshing(true);
    onRefresh?.();
    setTimeout(() => setIsRefreshing(false), 500);
  }, [onRefresh]);

  const handleEdit = useCallback((speedtest: SpeedTest) => {
    setSelectedSpeedtest(speedtest);
    setEditDialogOpen(true);
  }, []);

  const handleDelete = useCallback((speedtest: SpeedTest) => {
    setSelectedSpeedtest(speedtest);
    setSelectedSpeedtests([speedtest]);
    setDeleteDialogOpen(true);
  }, []);

  const handleBulkDelete = useCallback((speedtests: SpeedTest[]) => {
    setSelectedSpeedtests(speedtests);
    setDeleteDialogOpen(true);
  }, []);

  const handleViewOnMap = useCallback(
    (speedtest: SpeedTest) => {
      const params = new URLSearchParams();
      if (typeof speedtest.latitude === "number") params.set("lat", String(speedtest.latitude));
      if (typeof speedtest.longitude === "number") params.set("lng", String(speedtest.longitude));
      const query = params.toString();
      const mapPath = regionId ? `/${regionId}/broadband/speedtests/map` : "/speedtests/map";
      router.push(`${mapPath}${query ? `?${query}` : ""}`);
    },
    [regionId, router],
  );

  const handleCreateSuccess = useCallback(async () => {
    setCreateDialogOpen(false);
    // Force immediate refetch of all speedtest queries (remove stale data)
    await Promise.all([
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "speedtests",
        refetchType: "all"
      }),
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "get-speedtests-infinite",
        refetchType: "all"
      }),
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "speedtest-analytics",
        refetchType: "all"
      })
    ]);
    onRefresh?.();
  }, [onRefresh, queryClient]);

  const handleEditSuccess = useCallback(async () => {
    setEditDialogOpen(false);
    setSelectedSpeedtest(null);
    // Force immediate refetch of all speedtest queries (remove stale data)
    await Promise.all([
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "speedtests",
        refetchType: "all"
      }),
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "get-speedtests-infinite",
        refetchType: "all"
      }),
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "speedtest-analytics",
        refetchType: "all"
      })
    ]);
    onRefresh?.();
  }, [onRefresh, queryClient]);

  // Handler called by progress dialog to delete chunks
  const handleDeleteConfirm = useCallback(async (idsChunk: string[]) => {
    // Delete this chunk of IDs
    await deleteSpeedtestMutation.mutateAsync({
      pathParams: {},
      queryParams: { ids: idsChunk },
    });
    
    // Optimistically update cache by removing deleted items - instant UI update
    queryClient.setQueriesData(
      { predicate: (query) => query.queryKey[0] === "speedtests" },
      (oldData: unknown) => {
        if (!oldData || typeof oldData !== "object" || !("data" in oldData)) return oldData;
        
        const data = oldData as { data?: SpeedTest[]; totalRows?: number };
        const idsSet = new Set(idsChunk);
        
        return {
          ...data,
          data: (data.data || []).filter((st) => !idsSet.has(st.id)),
          totalRows: Math.max(0, (data.totalRows || 0) - idsSet.size),
        };
      },
    );
    
    // Invalidate analytics immediately after each chunk to keep dashboard updated
    await queryClient.invalidateQueries({ 
      predicate: (query) => query.queryKey[0] === "speedtest-analytics",
      refetchType: "all"
    });
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-speedtests-infinite", refetchType: "all" });
  }, [deleteSpeedtestMutation, queryClient]);
  
  // Handler called after all deletions complete
  const handleDeleteComplete = useCallback(async () => {
    // Clean up state
    setDeleteDialogOpen(false);
    setSelectedSpeedtest(null);
    setSelectedSpeedtests([]);
    
    // Force immediate refetch of all speedtest queries (remove stale data)
    await Promise.all([
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "speedtests",
        refetchType: "all" // Changed from "active" to "all" to force refetch
      }),
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "get-speedtests-infinite",
        refetchType: "all"
      }),
      queryClient.invalidateQueries({ 
        predicate: (query) => query.queryKey[0] === "speedtest-analytics",
        refetchType: "all" // Changed from "active" to "all" to force refetch
      })
    ]);
    
    // Trigger manual refresh
    onRefresh?.();
  }, [onRefresh, queryClient]);

  // Export handlers
  const handleExportCSV = useCallback(() => {
    if (speedtests.length === 0) return;

    const headers = [
      "ID",
      "Date/Time",
      "Download (Mbps)",
      "Upload (Mbps)",
      "Ping (ms)",
      "Jitter (ms)",
      "RSSI (dBm)",
      "Signal Quality",
      "Provider",
      "Technology",
      "Generation",
      "Latitude",
      "Longitude",
      "Address",
      "Phone Model",
    ];

    const rows = speedtests.map((st) => [
      st.id,
      st.dateTime,
      st.downloadSpeed?.toFixed(2) || "",
      st.uploadSpeed?.toFixed(2) || "",
      st.ping?.toFixed(2) || "",
      st.jitter?.toFixed(2) || "",
      st.rssi?.toString() || "",
      st.signalQuality || "",
      st.serviceProvider?.provider?.name || "",
      st.technology?.standard || "",
      st.generation?.name || "",
      st.latitude?.toString() || "",
      st.longitude?.toString() || "",
      st.address || "",
      st.phoneModel || "",
    ]);

    const csvContent = [
      headers.join(","),
      ...rows.map((row) => row.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(",")),
    ].join("\n");

    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `speedtests_export_${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }, [speedtests]);

  const handleExportPDF = useCallback(() => {
    // TODO: Implement PDF export with a library like jspdf
    alert("PDF export coming soon!");
  }, []);

  return (
    <div className={cn("flex flex-col h-full w-full overflow-hidden", className)}>
      {/* Animated Hero Section */}
      {isLoading && speedtests.length === 0 ? (
        <SpeedtestHeroSkeleton />
      ) : (
        <SpeedtestHero
          regionName={regionName}
          totalCount={totalCount}
          statistics={statistics}
          isCollapsed={heroCollapsed}
          showMap={false}
          onToggleCollapse={() => setHeroCollapsed(!heroCollapsed)}
          onToggleMap={() => {
            const mapPath = regionId ? `/${regionId}/broadband/speedtests/map` : "/speedtests/map";
            router.push(mapPath);
          }}
          scrollContainerRef={scrollContainerRef}
        />
      )}

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col overflow-hidden p-4 gap-4">
        {/* Action Bar */}
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            {/* DEV: Seed Data Button */}
            {(process.env.NODE_ENV === "development" || process.env.VERCEL_ENV === "development") && (
              <SpeedtestSeeder onComplete={onRefresh} />
            )}

            {/* Refresh Button */}
            <Button
              variant="outline"
              size="sm"
              onClick={handleRefresh}
              disabled={isRefreshing || isLoading}
              className="gap-2 border-gray-300 dark:border-gray-600">
              <RefreshCw className={cn("h-4 w-4", (isRefreshing || isLoading) && "animate-spin")} />
              Refresh
            </Button>

            {/* Navigate to Map View */}
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                const mapPath = regionId ? `/${regionId}/broadband/speedtests/map` : "/speedtests/map";
                router.push(mapPath);
              }}
              className="gap-2 border-cyan-500/50 text-cyan-600 hover:bg-cyan-50 dark:text-cyan-400 dark:hover:bg-cyan-950/30">
              <Map className="h-4 w-4" />
              Show Map
            </Button>
          </div>

          <div className="flex items-center gap-2">
            {/* Export Dropdown */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-2 border-gray-300 dark:border-gray-600"
                  disabled={speedtests.length === 0}>
                  <Download className="h-4 w-4" />
                  Export
                  <ChevronDown className="h-3 w-3 opacity-50" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-48">
                <DropdownMenuItem onClick={handleExportCSV}>
                  <FileSpreadsheet className="h-4 w-4 mr-2" />
                  Export as CSV
                </DropdownMenuItem>
                <DropdownMenuItem onClick={handleExportPDF}>
                  <FileText className="h-4 w-4 mr-2" />
                  Export as PDF
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Create Button */}
            <Button
              size="sm"
              onClick={() => setCreateDialogOpen(true)}
              className="gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-600 hover:to-blue-700 text-white shadow-lg shadow-cyan-500/25">
              <Plus className="h-4 w-4" />
              Add Speedtest
            </Button>
          </div>
        </motion.div>

        {/* Analytics Panel */}
        {isLoading && speedtests.length === 0 ? (
          <SpeedtestAnalyticsPanelSkeleton />
        ) : (
          <SpeedtestAnalyticsPanel
            queryParams={analyticsQueryParams}
            isCollapsed={analyticsCollapsed}
            onCollapsedChange={setAnalyticsCollapsed}
          />
        )}

        {/* Speedtest Table with pagination - fills remaining space with independent scroll */}
        <div ref={scrollContainerRef} className="flex-1 min-h-0">
          <SpeedtestTable
            data={speedtests}
            isLoading={isLoading}
            totalCount={totalCount}
            currentPage={currentPage}
            pageSize={pageSize}
            totalPages={totalPages}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
            onEdit={handleEdit}
            onDelete={handleDelete}
            onDeleteMultiple={handleBulkDelete}
            onViewOnMap={handleViewOnMap}
            onRefresh={onRefresh}
            regionId={regionId}
            filters={filters}
            onFilterChange={onFilterChange}
          />
        </div>
      </div>

      {/* Dialogs */}
      <CreateSpeedtestDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        onSuccess={handleCreateSuccess}
      />

      <EditSpeedtestDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        speedtest={selectedSpeedtest || undefined}
        onSuccess={handleEditSuccess}
      />

      <DeleteSpeedtestDialog
        open={deleteDialogOpen}
        onOpenChange={(open) => {
          setDeleteDialogOpen(open);
          if (!open) handleDeleteComplete();
        }}
        idsToDelete={selectedSpeedtests.map((st) => st.id)}
        onConfirmDelete={handleDeleteConfirm}
      />
    </div>
  );
}

export default SpeedtestDashboard;
