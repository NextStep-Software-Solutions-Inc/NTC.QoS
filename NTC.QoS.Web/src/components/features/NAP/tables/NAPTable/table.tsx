"use client";

import React, { memo, useCallback, useMemo, useState } from "react";
import Image from "next/image";
import { AnimatePresence, motion } from "framer-motion";
import {
  ChevronDown,
  ChevronUp,
  Copy,
  Edit,
  Loader2,
  MapPin,
  MoreVertical,
  Navigation,
  Trash,
  Wifi,
  X,
} from "lucide-react";
import { toast } from "sonner";

import DeleteNAPDialog from "@/components/features/NAP/dialogs/DeleteNAPDialog";
import EditNAPDialog from "@/components/features/NAP/dialogs/EditNAPDialog";
import { type NAPDetails } from "@/components/features/NAP/types/NAP";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useFilterOptions } from "@/hooks/filters";
import { cn } from "@/lib/utils";
import { BatchActions } from "./components/BatchActions";
import { FilterPanel } from "./components/FilterPanel";
import { LoadingAnimation } from "./components/LoadingAnimation";
import { Pagination } from "./components/Pagination";
// Import refactored components and hooks
import { SearchBar } from "./components/SearchBar";
import { StatsBar } from "./components/StatsBar";
import { useSelection } from "./hooks/useSelection";

// Enhanced dense NAP Card component with gradient accents
interface NAPCardProps {
  nap: NAPDetails;
  isSelected: boolean;
  onToggleSelection: (id: string) => void;
  onEdit: (nap: NAPDetails) => void;
  onDelete: (nap: NAPDetails) => void;
  onViewOnMap?: (nap: NAPDetails) => void;
  index: number;
  readonly?: boolean;
}

const NAPCard = memo(function NAPCard({
  nap,
  isSelected,
  onToggleSelection,
  onEdit,
  onDelete,
  onViewOnMap,
  index,
  readonly = false,
}: NAPCardProps) {
  const handleCopyCoordinates = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      const coords = `${nap.lat}, ${nap.lng}`;
      void navigator.clipboard.writeText(coords);
      toast.success("Coordinates copied!", { duration: 1500 });
    },
    [nap.lat, nap.lng],
  );

  const provider = nap.serviceProvider;
  const providerData = provider?.provider;
  const ispName = nap.ISP || providerData?.name || "Unknown";
  const ispColor = nap.ISPColor || providerData?.color || "#6b7280";
  const providerLogo = providerData?.logo;
  const initials = ispName.substring(0, 2).toUpperCase();

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.97 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.12, delay: Math.min(index * 0.008, 0.1) }}
      whileHover={{ scale: 1.02, y: -1 }}
      whileTap={{ scale: 0.98 }}
      className="h-full">
      <div
        className={cn(
          "relative h-full group rounded-lg overflow-hidden transition-all duration-150 cursor-pointer",
          "bg-white dark:bg-gray-900",
          "border shadow-sm hover:shadow-md",
          isSelected
            ? "border-blue-500 ring-1 ring-blue-500/30 shadow-blue-100 dark:shadow-blue-900/20"
            : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600",
        )}>
        {/* Card content */}
        <div className="pl-3 pr-2 py-2">
          {/* Header row: checkbox + provider info + menu */}
          <div className="flex items-center gap-1.5">
            <Checkbox
              checked={isSelected}
              onCheckedChange={() => onToggleSelection(nap.id)}
              onClick={(e) => e.stopPropagation()}
              className="h-3.5 w-3.5 rounded-[3px] border-gray-300"
            />

            {/* Provider badge with logo/initials */}
            {providerLogo ? (
              <div
                className="flex h-5 w-5 items-center justify-center rounded border bg-white p-0.5 flex-shrink-0"
                style={{ borderColor: ispColor }}>
                <Image
                  src={providerLogo}
                  alt={ispName}
                  className="h-full w-full object-contain"
                  width={20}
                  height={20}
                />
              </div>
            ) : (
              <div
                className="flex h-5 w-5 items-center justify-center rounded text-[9px] font-bold text-white flex-shrink-0"
                style={{ backgroundColor: ispColor }}>
                {initials}
              </div>
            )}

            <span
              className="text-[11px] font-medium text-gray-700 dark:text-gray-300 truncate flex-1 leading-tight"
              title={ispName}>
              {ispName}
            </span>

            {!readonly && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-5 w-5 p-0 opacity-0 group-hover:opacity-100 transition-opacity rounded hover:bg-gray-100 dark:hover:bg-gray-800">
                    <MoreVertical className="h-3.5 w-3.5 text-gray-400" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-36 shadow-lg">
                  <DropdownMenuLabel className="text-[10px] text-gray-500">Actions</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => onEdit(nap)} className="text-[11px] gap-2">
                    <Edit className="h-3 w-3" />
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleCopyCoordinates} className="text-[11px] gap-2">
                    <Copy className="h-3 w-3" />
                    Copy Coords
                  </DropdownMenuItem>
                  {onViewOnMap && (
                    <DropdownMenuItem onClick={() => onViewOnMap(nap)} className="text-[11px] gap-2">
                      <Navigation className="h-3 w-3" />
                      View on Map
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={() => onDelete(nap)}
                    className="text-[11px] gap-2 text-red-600 focus:text-red-600 focus:bg-red-50">
                    <Trash className="h-3 w-3" />
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>

          {/* Coordinates row */}
          <div className="mt-1.5 flex items-center gap-1">
            <MapPin className="h-3 w-3 flex-shrink-0" style={{ color: ispColor }} />
            <code className="text-[10px] font-mono text-gray-500 dark:text-gray-400">
              {typeof nap.lat === "number" ? nap.lat.toFixed(5) : "N/A"},{" "}
              {typeof nap.lng === "number" ? nap.lng.toFixed(5) : "N/A"}
            </code>
          </div>

          {/* Quick action buttons - only show on hover */}
          {!readonly && (
            <div className="flex items-center gap-0.5 mt-1.5 pt-1.5 border-t border-gray-100 dark:border-gray-800 opacity-0 group-hover:opacity-100 transition-opacity">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-5 w-5 p-0 rounded hover:bg-blue-50 dark:hover:bg-blue-900/30"
                    onClick={(e) => {
                      e.stopPropagation();
                      onEdit(nap);
                    }}>
                    <Edit className="h-2.5 w-2.5 text-blue-600" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="bottom" className="text-[10px]">
                  Edit
                </TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-5 w-5 p-0 rounded hover:bg-emerald-50 dark:hover:bg-emerald-900/30"
                    onClick={handleCopyCoordinates}>
                    <Copy className="h-2.5 w-2.5 text-emerald-600" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="bottom" className="text-[10px]">
                  Copy
                </TooltipContent>
              </Tooltip>
              {onViewOnMap && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-5 w-5 p-0 rounded hover:bg-violet-50 dark:hover:bg-violet-900/30"
                      onClick={(e) => {
                        e.stopPropagation();
                        onViewOnMap(nap);
                      }}>
                      <Navigation className="h-2.5 w-2.5 text-violet-600" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent side="bottom" className="text-[10px]">
                    Map
                  </TooltipContent>
                </Tooltip>
              )}
              <div className="flex-1" />
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-5 w-5 p-0 rounded hover:bg-red-50 dark:hover:bg-red-900/30"
                    onClick={(e) => {
                      e.stopPropagation();
                      onDelete(nap);
                    }}>
                    <Trash className="h-2.5 w-2.5 text-red-500" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="bottom" className="text-[10px]">
                  Delete
                </TooltipContent>
              </Tooltip>
            </div>
          )}
        </div>
      </div>
    </motion.div>
  );
});

interface NAPTableProps {
  data: NAPDetails[];
  totalCount: number;
  regionId?: string;
  searchTerm?: string;
  providerFilter?: string;
  provinceFilter?: string;
  municipalityFilter?: string;
  barangayFilter?: string;
  regionFilter?: string;
  currentPage?: number;
  pageSize?: number;
  onSearchChange?: (value: string) => void;
  onProviderFilterChange?: (value: string) => void;
  onProvinceFilterChange?: (value: string) => void;
  onMunicipalityFilterChange?: (value: string) => void;
  onBarangayFilterChange?: (value: string) => void;
  onRegionFilterChange?: (value: string) => void;
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  onClearAllFilters?: () => void;
  onDeleteSelected?: (ids: string[]) => void;
  onUpdateStatusSelected?: (ids: string[], status: string) => void;
  onEditNAP?: (nap: NAPDetails) => void;
  onDeleteNAP?: (nap: NAPDetails) => void;
  onViewOnMap?: (nap: NAPDetails) => void;
  isLoading?: boolean;
  readonly?: boolean;
  toolbarCollapsed?: boolean;
  onToolbarCollapsedChange?: (collapsed: boolean) => void;
  onAddNAP?: () => void;
}

export function NAPTable({
  data,
  totalCount = 0,
  regionId,
  searchTerm = "",
  providerFilter = "all",
  provinceFilter = "all",
  municipalityFilter = "all",
  barangayFilter = "all",
  regionFilter = "all",
  currentPage = 1,
  pageSize = 20,
  onSearchChange,
  onProviderFilterChange,
  onProvinceFilterChange,
  onMunicipalityFilterChange,
  onBarangayFilterChange,
  onRegionFilterChange,
  onPageChange,
  onPageSizeChange,
  onClearAllFilters,
  onDeleteSelected,
  onUpdateStatusSelected,
  onEditNAP,
  onDeleteNAP,
  onViewOnMap,
  isLoading = false,
  readonly = false,
  toolbarCollapsed = false,
  onToolbarCollapsedChange,
}: NAPTableProps) {
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedNAP, setSelectedNAP] = useState<NAPDetails | null>(null);
  const [internalSearch, setInternalSearch] = useState("");

  const effectiveSearch = onSearchChange ? searchTerm : internalSearch;

  const filteredData = useMemo(() => {
    let result = data;
    if (effectiveSearch) {
      const searchLower = effectiveSearch.toLowerCase();
      result = result.filter(
        (nap) => nap.ISP?.toLowerCase().includes(searchLower) || nap.id.toLowerCase().includes(searchLower),
      );
    }
    return result;
  }, [data, effectiveSearch]);

  const { providers, provinces, municipalities, barangays, regions } = useFilterOptions({
    regionId: regionId || "",
    provinceFilter,
    municipalityFilter,
    serviceType: "wired",
  });

  const { selectedIds, selectedCount, toggleSelection, toggleSelectAll, clearSelection } = useSelection(filteredData);
  const totalPages = Math.ceil((totalCount || filteredData.length) / pageSize);

  const hasActiveFilters = useMemo(
    () =>
      providerFilter !== "all" ||
      provinceFilter !== "all" ||
      municipalityFilter !== "all" ||
      barangayFilter !== "all" ||
      effectiveSearch !== "",
    [providerFilter, provinceFilter, municipalityFilter, barangayFilter, effectiveSearch],
  );

  const handleSearchChange = useCallback(
    (value: string) => {
      onSearchChange ? onSearchChange(value) : setInternalSearch(value);
    },
    [onSearchChange],
  );
  const handleClearFilters = useCallback(() => {
    onClearAllFilters ? onClearAllFilters() : setInternalSearch("");
  }, [onClearAllFilters]);
  const handleBatchDelete = useCallback(() => {
    onDeleteSelected?.(Array.from(selectedIds));
    clearSelection();
  }, [selectedIds, onDeleteSelected, clearSelection]);
  const handleBatchStatusUpdate = useCallback(
    (status: string) => {
      onUpdateStatusSelected?.(Array.from(selectedIds), status);
      clearSelection();
    },
    [selectedIds, onUpdateStatusSelected, clearSelection],
  );

  const handleEdit = useCallback(
    (nap: NAPDetails) => {
      if (onEditNAP) {
        onEditNAP(nap);
      } else {
        setSelectedNAP(nap);
        setEditDialogOpen(true);
      }
    },
    [onEditNAP],
  );

  const handleDelete = useCallback(
    (nap: NAPDetails) => {
      if (onDeleteNAP) {
        onDeleteNAP(nap);
      } else {
        setSelectedNAP(nap);
        setDeleteDialogOpen(true);
      }
    },
    [onDeleteNAP],
  );

  const handlePageChange = useCallback(
    (page: number) => {
      onPageChange?.(page);
    },
    [onPageChange],
  );
  const handlePageSizeChange = useCallback(
    (size: number) => {
      onPageSizeChange?.(size);
    },
    [onPageSizeChange],
  );

  const paginatedData = useMemo(() => {
    if (onPageChange) return filteredData;
    const start = (currentPage - 1) * pageSize;
    return filteredData.slice(start, start + pageSize);
  }, [filteredData, currentPage, pageSize, onPageChange]);

  return (
    <div className="flex flex-col h-full">
      <div className="bg-white dark:bg-gray-900 border-b px-4 py-3 sticky top-0 z-30">
        <div className="flex items-center gap-2">
          <SearchBar
            value={effectiveSearch}
            onChange={handleSearchChange}
            placeholder="Search by provider or ID..."
            className="flex-1 max-w-sm"
          />
          {hasActiveFilters && (
            <Button variant="outline" size="sm" className="h-9" onClick={handleClearFilters}>
              <X className="h-3.5 w-3.5 mr-1.5" />
              Clear
            </Button>
          )}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-9"
                onClick={() => onToolbarCollapsedChange?.(!toolbarCollapsed)}>
                {toolbarCollapsed ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronUp className="h-3.5 w-3.5" />}
              </Button>
            </TooltipTrigger>
            <TooltipContent>{toolbarCollapsed ? "Show filters" : "Hide filters"}</TooltipContent>
          </Tooltip>
          <div className="flex-1" />
        </div>
        <AnimatePresence mode="wait">
          {!toolbarCollapsed && (
            <motion.div
              key="toolbar-content"
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: "auto" }}
              exit={{ opacity: 0, height: 0 }}
              transition={{ duration: 0.15 }}
              className="overflow-hidden">
              <div className="pt-3 space-y-3 pb-1">
                {selectedIds.size > 0 && !readonly ? (
                  <motion.div
                    key="batch-actions"
                    initial={{ opacity: 0, y: -5 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -5 }}
                    transition={{ duration: 0.15 }}>
                    <BatchActions
                      selectedCount={selectedCount}
                      onClearSelection={clearSelection}
                      onUpdateStatus={handleBatchStatusUpdate}
                      onDelete={handleBatchDelete}
                    />
                  </motion.div>
                ) : (
                  <motion.div
                    key="filters"
                    initial={{ opacity: 0, y: -5 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -5 }}
                    transition={{ duration: 0.15 }}>
                    <FilterPanel
                      regionId={regionId}
                      regionFilter={regionFilter}
                      onRegionFilterChange={onRegionFilterChange || (() => {})}
                      regions={regions}
                      providerFilter={providerFilter}
                      provinceFilter={provinceFilter}
                      municipalityFilter={municipalityFilter}
                      barangayFilter={barangayFilter}
                      onProviderFilterChange={onProviderFilterChange || (() => {})}
                      onProvinceFilterChange={onProvinceFilterChange || (() => {})}
                      onMunicipalityFilterChange={onMunicipalityFilterChange || (() => {})}
                      onBarangayFilterChange={onBarangayFilterChange || (() => {})}
                      providers={providers}
                      provinces={provinces}
                      municipalities={municipalities}
                      barangays={barangays}
                    />
                  </motion.div>
                )}
              </div>
            </motion.div>
          )}
        </AnimatePresence>
        <StatsBar
          currentPage={currentPage}
          pageSize={pageSize}
          totalCount={totalCount || filteredData.length}
          selectedCount={selectedCount}
          isLoading={isLoading}
          onToggleSelectAll={toggleSelectAll}
          dataLength={paginatedData.length}
        />
      </div>
      <div className="flex-1 overflow-y-auto px-4 py-4">
        <AnimatePresence mode="sync">
          {isLoading && paginatedData.length === 0 ? (
            <LoadingAnimation />
          ) : paginatedData.length > 0 ? (
            <div className="relative">
              {isLoading && (
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="absolute inset-0 bg-white/70 dark:bg-gray-900/70 backdrop-blur-[2px] z-10 flex items-center justify-center">
                  <div className="flex items-center gap-3 bg-white dark:bg-gray-800 px-4 py-2.5 rounded-xl shadow-xl border border-gray-200 dark:border-gray-700">
                    <div className="relative">
                      <Loader2 className="h-5 w-5 animate-spin text-blue-500" />
                      <div className="absolute inset-0 h-5 w-5 animate-ping rounded-full bg-blue-500/20" />
                    </div>
                    <span className="text-sm font-medium bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                      Loading NAPs...
                    </span>
                  </div>
                </motion.div>
              )}
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 xl:grid-cols-7 2xl:grid-cols-8 gap-2">
                {paginatedData.map((nap, index) => (
                  <NAPCard
                    key={nap.id}
                    nap={nap}
                    isSelected={selectedIds.has(nap.id)}
                    onToggleSelection={toggleSelection}
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                    onViewOnMap={onViewOnMap}
                    index={index}
                    readonly={readonly}
                  />
                ))}
              </div>
            </div>
          ) : (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex flex-col items-center justify-center py-20">
              <div className="relative">
                <div className="absolute inset-0 rounded-full bg-gradient-to-r from-blue-500/20 to-purple-500/20 blur-xl" />
                <div className="relative rounded-full bg-gradient-to-br from-gray-100 to-gray-50 dark:from-gray-800 dark:to-gray-900 p-8 shadow-inner">
                  <Wifi className="h-14 w-14 text-gray-400" />
                </div>
              </div>
              <p className="text-xl font-semibold mt-6 text-gray-700 dark:text-gray-300">No NAPs found</p>
              <p className="text-sm text-gray-500 mt-1">
                {effectiveSearch || hasActiveFilters
                  ? "Try adjusting your search or filters"
                  : "NAPs will appear here once registered"}
              </p>
              {hasActiveFilters && (
                <Button variant="outline" size="sm" className="mt-4 gap-2" onClick={handleClearFilters}>
                  <X className="h-3.5 w-3.5" />
                  Clear all filters
                </Button>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
      <div className="sticky bottom-0 z-30 bg-white dark:bg-gray-900 border-t shadow-lg">
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          pageSize={pageSize}
          onPageChange={handlePageChange}
          onPageSizeChange={handlePageSizeChange}
          totalCount={totalCount || filteredData.length}
        />
      </div>
      {selectedNAP && (
        <EditNAPDialog
          nap={selectedNAP}
          open={editDialogOpen}
          onOpenChange={setEditDialogOpen}
          onSuccess={() => {
            setSelectedNAP(null);
            setEditDialogOpen(false);
          }}
        />
      )}
      {selectedNAP && (
        <DeleteNAPDialog
          nap={selectedNAP}
          open={deleteDialogOpen}
          onOpenChange={setDeleteDialogOpen}
          onSuccess={() => {
            setSelectedNAP(null);
            setDeleteDialogOpen(false);
          }}
        />
      )}
    </div>
  );
}

export default NAPTable;
