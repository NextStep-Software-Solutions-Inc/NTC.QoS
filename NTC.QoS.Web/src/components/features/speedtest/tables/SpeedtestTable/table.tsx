"use client";

import React, { memo, useCallback, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  Activity,
  FileSpreadsheet,
  LayoutGrid,
  LineChart,
  List,
  Plus,
  RefreshCw,
  SlidersHorizontal,
} from "lucide-react";
import { toast } from "sonner";

import type { SpeedtestFilters } from "@/components/features/speedtest/ManageSpeedtests";
import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { useFilterOptions } from "@/hooks/filters";
import { cn } from "@/lib/utils";
import { BatchActions } from "./components/BatchActions";
import { FilterPanel } from "./components/FilterPanel";
import { Pagination } from "./components/Pagination";
import { ProviderPerformanceChart } from "./components/ProviderPerformanceChart";
// Import components
import { SearchBar } from "./components/SearchBar";
import { SpeedtestCard } from "./components/SpeedtestCard";
import { SpeedtestCardsSkeleton } from "./components/SpeedtestCardsSkeleton";
import { StatsBar } from "./components/StatsBar";
import { useSelection } from "./hooks/useSelection";

type ViewMode = "grid" | "list" | "chart";

export interface SpeedtestTableProps {
  data: SpeedTest[];
  isLoading?: boolean;
  totalCount?: number;
  currentPage?: number;
  pageSize?: number;
  totalPages?: number;
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  onRefresh?: () => void;
  onEdit?: (speedtest: SpeedTest) => void;
  onDelete?: (speedtest: SpeedTest) => void;
  onDeleteMultiple?: (speedtests: SpeedTest[]) => void;
  onViewOnMap?: (speedtest: SpeedTest) => void;
  onAddNew?: () => void;
  onExport?: (speedtests: SpeedTest[]) => void;
  readonly?: boolean;
  regionId?: string;
  filters?: SpeedtestFilters;
  onFilterChange?: (filters: Partial<SpeedtestFilters>) => void;
  className?: string;
}

function SpeedtestTable({
  data,
  isLoading = false,
  totalCount = 0,
  currentPage = 1,
  pageSize = 20,
  totalPages = 1,
  onPageChange,
  onPageSizeChange,
  onRefresh,
  onEdit,
  onDelete,
  onDeleteMultiple,
  onViewOnMap,
  onAddNew,
  onExport,
  readonly = false,
  regionId,
  filters: externalFilters,
  onFilterChange,
  className,
}: SpeedtestTableProps) {
  // View mode
  const [viewMode, setViewMode] = useState<ViewMode>("grid");
  const [showFilters, setShowFilters] = useState(false);

  // Use external filters if provided, otherwise use local state
  const [localSearchTerm, setLocalSearchTerm] = useState("");
  const [localProviderFilter, setLocalProviderFilter] = useState("all");
  const [localProvinceFilter, setLocalProvinceFilter] = useState("all");
  const [localMunicipalityFilter, setLocalMunicipalityFilter] = useState("all");
  const [localBarangayFilter, setLocalBarangayFilter] = useState("all");
  const [localSignalQualityFilter, setLocalSignalQualityFilter] = useState("all");
  const [localNbcFilter, setLocalNbcFilter] = useState("all");
  const [localStartDate, setLocalStartDate] = useState<string | undefined>(undefined);
  const [localEndDate, setLocalEndDate] = useState<string | undefined>(undefined);

  // Derived filter values - use external if available, fallback to local
  const searchTerm = externalFilters?.search ?? localSearchTerm;
  const regionFilter = regionId || "all";
  const providerFilter = externalFilters?.provider ?? localProviderFilter;
  const provinceFilter = externalFilters?.province ?? localProvinceFilter;
  const municipalityFilter = externalFilters?.municipality ?? localMunicipalityFilter;
  const barangayFilter = externalFilters?.barangay ?? localBarangayFilter;
  const signalQualityFilter = externalFilters?.signalQuality ?? localSignalQualityFilter;
  const nbcFilter = externalFilters?.nbc ?? localNbcFilter;
  const startDate = externalFilters?.startDate ?? localStartDate;
  const endDate = externalFilters?.endDate ?? localEndDate;

  // Helper to update filters - uses external handler if available
  const updateFilter = useCallback(
    (key: keyof SpeedtestFilters, value: string) => {
      if (onFilterChange) {
        onFilterChange({ [key]: value === "all" ? undefined : value });
      } else {
        // Fallback to local state
        switch (key) {
          case "search":
            setLocalSearchTerm(value);
            break;
          case "provider":
            setLocalProviderFilter(value);
            break;
          case "province":
            setLocalProvinceFilter(value);
            break;
          case "municipality":
            setLocalMunicipalityFilter(value);
            break;
          case "barangay":
            setLocalBarangayFilter(value);
            break;
          case "signalQuality":
            setLocalSignalQualityFilter(value);
            break;
          case "nbc":
            setLocalNbcFilter(value);
            break;
        }
      }
    },
    [onFilterChange],
  );

  // Wrapper setters that use updateFilter
  const setSearchTerm = useCallback((v: string) => updateFilter("search", v), [updateFilter]);
  const setProviderFilter = useCallback((v: string) => updateFilter("provider", v), [updateFilter]);
  const setProvinceFilter = useCallback((v: string) => updateFilter("province", v), [updateFilter]);
  const setMunicipalityFilter = useCallback((v: string) => updateFilter("municipality", v), [updateFilter]);
  const setBarangayFilter = useCallback((v: string) => updateFilter("barangay", v), [updateFilter]);
  const setSignalQualityFilter = useCallback((v: string) => updateFilter("signalQuality", v), [updateFilter]);
  const setNbcFilter = useCallback((v: string) => updateFilter("nbc", v), [updateFilter]);
  // Keep regionFilter as read-only (comes from regionId prop)
  const setRegionFilter = useCallback((_v: string) => {
    /* no-op, region is from prop */
  }, []);

  const handleDateRangeChange = useCallback(
    (newStartDate?: string, newEndDate?: string) => {
      if (onFilterChange) {
        onFilterChange({ startDate: newStartDate, endDate: newEndDate });
      } else {
        setLocalStartDate(newStartDate);
        setLocalEndDate(newEndDate);
      }
    },
    [onFilterChange],
  );

  // Selection
  const { isSelected, toggleSelection, toggleSelectAll, clearSelection, selectedItems, selectedCount } =
    useSelection(data);

  // Filter options from hook - only wireless providers for speedtest
  const { regions, provinces, municipalities, barangays, providers } = useFilterOptions({
    regionId: regionFilter !== "all" ? regionFilter : regionId || "",
    provinceFilter: provinceFilter !== "all" ? provinceFilter : undefined,
    municipalityFilter: municipalityFilter !== "all" ? municipalityFilter : undefined,
    serviceType: "wireless",
  });

  // Calculate active filter count
  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (regionFilter !== "all" && !regionId) count++;
    if (providerFilter !== "all") count++;
    if (provinceFilter !== "all") count++;
    if (municipalityFilter !== "all") count++;
    if (barangayFilter !== "all") count++;
    if (signalQualityFilter !== "all") count++;
    if (nbcFilter !== "all") count++;
    if (startDate || endDate) count++;
    return count;
  }, [regionFilter, providerFilter, provinceFilter, municipalityFilter, barangayFilter, signalQualityFilter, nbcFilter, startDate, endDate, regionId]);

  // Filter data locally (for search)
  const filteredData = useMemo(() => {
    if (!searchTerm) return data;
    const term = searchTerm.toLowerCase();
    
    const results = data.filter((speedtest) => {
      const providerName = speedtest.serviceProvider?.provider?.name?.toLowerCase() || "";
      const address = speedtest.address?.toLowerCase() || "";
      const phoneModel = speedtest.phoneModel?.toLowerCase() || "";
      
      const matches = providerName.includes(term) || address.includes(term) || phoneModel.includes(term);
      
      // Debug: Log first few matches to help troubleshoot
      if (matches && process.env.NODE_ENV === "development") {
        console.log("Search match found:", {
          searchTerm: term,
          provider: speedtest.serviceProvider?.provider?.name,
          address: speedtest.address?.slice(0, 50),
          matched: providerName.includes(term) ? "provider" : address.includes(term) ? "address" : "phone"
        });
      }
      
      return matches;
    });
    
    // Debug: Log search results summary
    if (searchTerm && process.env.NODE_ENV === "development") {
      console.log(`Search for "${term}" returned ${results.length} of ${data.length} speedtests`);
    }
    
    return results;
  }, [data, searchTerm]);

  // Calculate average stats
  const stats = useMemo(() => {
    if (filteredData.length === 0) return { avgDownload: 0, avgUpload: 0, avgPing: 0 };
    const totals = filteredData.reduce(
      (acc, st) => ({
        download: acc.download + (st.downloadSpeed || 0),
        upload: acc.upload + (st.uploadSpeed || 0),
        ping: acc.ping + (st.ping || 0),
      }),
      { download: 0, upload: 0, ping: 0 },
    );
    return {
      avgDownload: totals.download / filteredData.length,
      avgUpload: totals.upload / filteredData.length,
      avgPing: totals.ping / filteredData.length,
    };
  }, [filteredData]);

  // Handlers
  const handleResetFilters = useCallback(() => {
    if (onFilterChange) {
      // Reset all filters via external handler
      onFilterChange({
        search: undefined,
        provider: undefined,
        province: undefined,
        municipality: undefined,
        barangay: undefined,
        signalQuality: undefined,
        nbc: undefined,
        startDate: undefined,
        endDate: undefined,
      });
    } else {
      // Reset local state
      setLocalSearchTerm("");
      setLocalProviderFilter("all");
      setLocalProvinceFilter("all");
      setLocalMunicipalityFilter("all");
      setLocalBarangayFilter("all");
      setLocalSignalQualityFilter("all");
      setLocalNbcFilter("all");
      setLocalStartDate(undefined);
      setLocalEndDate(undefined);
    }
  }, [onFilterChange]);

  const handleEdit = useCallback(
    (speedtest: SpeedTest) => {
      onEdit?.(speedtest);
    },
    [onEdit],
  );

  const handleDelete = useCallback(
    (speedtest: SpeedTest) => {
      onDelete?.(speedtest);
    },
    [onDelete],
  );

  const handleDeleteSelected = useCallback(() => {
    if (selectedItems.length > 0) {
      onDeleteMultiple?.(selectedItems);
    }
  }, [selectedItems, onDeleteMultiple]);

  const handleExportSelected = useCallback(() => {
    if (selectedItems.length > 0) {
      onExport?.(selectedItems);
      toast.success(`Exporting ${selectedItems.length} speedtests...`);
    }
  }, [selectedItems, onExport]);

  return (
    <Card className={cn("flex flex-col h-full overflow-hidden", className)}>
      {/* Header */}
      <CardHeader className="flex-shrink-0 px-4 py-3 border-b bg-gradient-to-r from-slate-50 to-gray-50 dark:from-gray-900 dark:to-gray-800">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          {/* Left: Title and stats */}
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center h-10 w-10 rounded-xl bg-gradient-to-br from-cyan-500 to-blue-600 shadow-lg shadow-cyan-500/25">
              <Activity className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-bold bg-gradient-to-r from-cyan-600 to-blue-600 bg-clip-text text-transparent">
                Speedtest Results
              </h2>
              <p className="text-xs text-muted-foreground">
                {totalCount.toLocaleString()} total tests • Avg: {stats.avgDownload.toFixed(1)} Mbps down
              </p>
            </div>
          </div>

          {/* Right: Actions */}
          <div className="flex items-center gap-2">
            {/* Search */}
            <SearchBar
              value={searchTerm}
              onChange={setSearchTerm}
              placeholder="Search provider, address, phone..."
              helpText="Searches: Provider name (e.g., SMART, Globe), Address, Phone model"
              className="w-64"
            />

            {/* Toggle filters */}
            <Button
              variant={showFilters ? "secondary" : "outline"}
              size="sm"
              onClick={() => setShowFilters(!showFilters)}
              className="gap-2">
              <SlidersHorizontal className="h-4 w-4" />
              Filters
              {activeFilterCount > 0 && (
                <Badge className="ml-1 h-5 w-5 p-0 flex items-center justify-center bg-cyan-500 text-white text-xs">
                  {activeFilterCount}
                </Badge>
              )}
            </Button>

            {/* View mode toggle */}
            <div className="flex items-center rounded-lg border bg-white dark:bg-gray-800 p-0.5">
              <Button
                variant={viewMode === "grid" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => setViewMode("grid")}
                className="h-8 w-8 p-0"
                title="Grid View">
                <LayoutGrid className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === "list" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => setViewMode("list")}
                className="h-8 w-8 p-0"
                title="List View">
                <List className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === "chart" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => setViewMode("chart")}
                className="h-8 w-8 p-0"
                title="Chart View">
                <LineChart className="h-4 w-4" />
              </Button>
            </div>

            {/* Refresh */}
            <Button variant="outline" size="sm" onClick={onRefresh} disabled={isLoading} className="gap-2">
              <RefreshCw className={cn("h-4 w-4", isLoading && "animate-spin")} />
            </Button>

            {/* Export */}
            <Button variant="outline" size="sm" onClick={() => onExport?.(data)} className="gap-2">
              <FileSpreadsheet className="h-4 w-4" />
              Export
            </Button>

            {/* Add new */}
            {!readonly && onAddNew && (
              <Button
                size="sm"
                onClick={onAddNew}
                className="gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-600 hover:to-blue-700 text-white shadow-lg shadow-cyan-500/25">
                <Plus className="h-4 w-4" />
                Add Test
              </Button>
            )}
          </div>
        </div>
      </CardHeader>

      {/* Filters panel */}
      <AnimatePresence>
        {showFilters && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            className="overflow-hidden border-b">
            <div className="p-3">
              <FilterPanel
                regionId={regionId}
                regionFilter={regionFilter}
                onRegionFilterChange={setRegionFilter}
                regions={regions}
                providerFilter={providerFilter}
                provinceFilter={provinceFilter}
                municipalityFilter={municipalityFilter}
                barangayFilter={barangayFilter}
                signalQualityFilter={signalQualityFilter}
                nbcFilter={nbcFilter}
                startDate={startDate}
                endDate={endDate}
                onProviderFilterChange={setProviderFilter}
                onProvinceFilterChange={setProvinceFilter}
                onMunicipalityFilterChange={setMunicipalityFilter}
                onBarangayFilterChange={setBarangayFilter}
                onSignalQualityFilterChange={setSignalQualityFilter}
                onNbcFilterChange={setNbcFilter}
                onDateRangeChange={handleDateRangeChange}
                providers={providers}
                provinces={provinces}
                municipalities={municipalities}
                barangays={barangays}
                onResetFilters={handleResetFilters}
                activeFilterCount={activeFilterCount}
              />
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Content */}
      <CardContent className="flex-1 overflow-auto p-4">
        {isLoading ? (
          <SpeedtestCardsSkeleton />
        ) : filteredData.length === 0 ? (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="flex flex-col items-center justify-center py-20">
            <div className="w-20 h-20 rounded-full bg-gradient-to-br from-gray-100 to-gray-200 dark:from-gray-800 dark:to-gray-700 flex items-center justify-center mb-4">
              <Activity className="h-10 w-10 text-gray-400" />
            </div>
            <h3 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-2">No speedtests found</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 text-center max-w-md">
              {searchTerm
                ? `No speedtests match "${searchTerm}". The search looks for provider names (e.g., SMART, Globe), addresses, or phone models in the currently loaded data. Try clearing filters or adjusting your search.`
                : "Speedtest results will appear here once performance tests are conducted."}
            </p>
            {searchTerm && (
              <div className="mt-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800 max-w-md">
                <p className="text-sm text-blue-800 dark:text-blue-200 font-medium mb-2">💡 Search Tips:</p>
                <ul className="text-xs text-blue-700 dark:text-blue-300 space-y-1 list-disc list-inside">
                  <li>Try searching: "SMART", "Globe", "DITO", "Converge"</li>
                  <li>Search works on displayed data only</li>
                  <li>Clear other filters if no results appear</li>
                  <li>Partial matches work (e.g., "SM" finds "SMART")</li>
                </ul>
              </div>
            )}
            {!readonly && onAddNew && (
              <Button onClick={onAddNew} className="mt-6 gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 text-white">
                <Plus className="h-4 w-4" />
                Add Speedtest
              </Button>
            )}
          </motion.div>
        ) : viewMode === "chart" ? (
          <ProviderPerformanceChart data={filteredData} />
        ) : (
          <div
            className={cn(
              viewMode === "grid"
                ? "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-3 auto-rows-fr"
                : "flex flex-col gap-2",
            )}>
            <AnimatePresence mode="popLayout">
              {filteredData.map((speedtest, index) => (
                <SpeedtestCard
                  key={speedtest.id}
                  speedtest={speedtest}
                  isSelected={isSelected(speedtest.id)}
                  onToggleSelection={toggleSelection}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onViewOnMap={onViewOnMap}
                  index={index}
                  readonly={readonly}
                  viewMode={viewMode}
                />
              ))}
            </AnimatePresence>
          </div>
        )}
      </CardContent>

      {/* Footer with stats and pagination */}
      {!isLoading && filteredData.length > 0 && (
        <div className="flex-shrink-0 border-t">
          <StatsBar
            currentPage={currentPage}
            pageSize={pageSize}
            totalCount={totalCount}
            selectedCount={selectedCount}
            isLoading={isLoading}
            onToggleSelectAll={() => toggleSelectAll(filteredData)}
            dataLength={filteredData.length}
            avgDownloadSpeed={stats.avgDownload}
            avgUploadSpeed={stats.avgUpload}
            avgPing={stats.avgPing}
          />

          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              pageSize={pageSize}
              onPageChange={onPageChange || (() => {})}
              onPageSizeChange={onPageSizeChange || (() => {})}
              totalCount={totalCount}
            />
          )}
        </div>
      )}

      {/* Batch actions */}
      <BatchActions
        selectedCount={selectedCount}
        onDelete={handleDeleteSelected}
        onExport={handleExportSelected}
        onClearSelection={clearSelection}
      />
    </Card>
  );
}

export default memo(SpeedtestTable);
