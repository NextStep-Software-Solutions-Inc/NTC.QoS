"use client";

import React, { memo, useCallback, useMemo } from "react";
import Image from "next/image";
import { AnimatePresence, motion } from "framer-motion";
import {
  AlertTriangle,
  BadgeCheck,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Clock,
  Edit,
  Eye,
  Loader2,
  MapPin,
  MoreVertical,
  RadioTower,
  Settings2,
  Signal,
  Trash,
  Wrench,
  X,
  XCircle,
} from "lucide-react";

import { type Cell } from "@/components/features/cell/types/Cell";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useFilterOptions } from "@/hooks/filters";
import { BatchActions } from "./components/BatchActions";
import { FilterPanel } from "./components/FilterPanel";
import { LoadingAnimation } from "./components/LoadingAnimation";
import { Pagination } from "./components/Pagination";
// Import refactored components and hooks
import { SearchBar } from "./components/SearchBar";
import { StatsBar } from "./components/StatsBar";
import { useSelection } from "./hooks/useSelection";

// Status badge configuration - defined outside component to prevent recreating on each render
const statusConfig = {
  unknown: { className: "bg-amber-100 text-amber-800 border-amber-300", icon: "" },
  pending: { className: "bg-orange-100 text-orange-800 border-orange-300", icon: "" },
  licensed: { className: "bg-green-100 text-green-800 border-green-300", icon: "" },
};

function getStatusBadge(status: string, onClick?: (e: React.MouseEvent) => void) {
  const statusLower = status?.toLowerCase() || "unknown";
  const config = statusConfig[statusLower as keyof typeof statusConfig] || statusConfig.unknown;

  return (
    <Badge 
      className={`${config.className} ${onClick ? 'cursor-pointer hover:opacity-80 transition-opacity' : ''}`}
      onClick={onClick}>
      {status || "Unknown"}
    </Badge>
  );
}

// Memoized Cell Card component to prevent re-renders
interface CellCardProps {
  cell: Cell;
  isSelected: boolean;
  onToggleSelection: (id: string) => void;
  onViewDetails: (id: string, queryParams?: string) => void;
  onEdit: (id: string) => void;
  onDelete: (id: string) => void;
  onUpdateStatus: (id: string, status: string) => void;
  onToggleVerified: (id: string, isVerified: boolean) => void;
  index: number;
  readonly?: boolean;
}

const CellCard = memo(function CellCard({
  cell,
  isSelected,
  onToggleSelection,
  onViewDetails,
  onEdit,
  onDelete,
  onUpdateStatus,
  onToggleVerified,
  index,
  readonly = false,
}: CellCardProps) {
  const handleCardClick = useCallback(
    (e: React.MouseEvent) => {
      // Don't navigate if clicking on checkbox, dropdown, or buttons
      if (
        (e.target as HTMLElement).closest('[role="checkbox"]') ||
        (e.target as HTMLElement).closest('[role="menuitem"]') ||
        (e.target as HTMLElement).closest("button") ||
        (e.target as HTMLElement).closest("[data-radix-collection-item]")
      ) {
        return;
      }
      onViewDetails(cell.id);
    },
    [cell.id, onViewDetails],
  );

  const handleViewDetails = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onViewDetails(cell.id);
    },
    [cell.id, onViewDetails],
  );

  const handleEdit = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onEdit(cell.id);
    },
    [cell.id, onEdit],
  );

  const handleDelete = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onDelete(cell.id);
    },
    [cell.id, onDelete],
  );

  const handleStatusChange = useCallback(
    (status: string) => {
      onUpdateStatus(cell.id, status);
    },
    [cell.id, onUpdateStatus],
  );

  const handleToggleVerified = useCallback(() => {
    onToggleVerified(cell.id, !cell.isVerified);
  }, [cell.id, cell.isVerified, onToggleVerified]);

  const handleStatusClick = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onViewDetails(cell.id, "editStatus=true");
    },
    [cell.id, onViewDetails],
  );

  return (
    <motion.div
      layout="position"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.15, delay: Math.min(index * 0.02, 0.3) }}>
      <Card
        className={`hover:shadow-lg transition-shadow duration-150 cursor-pointer border-2 group ${
          isSelected
            ? "border-blue-500 bg-blue-50 dark:bg-blue-950/20"
            : "border-gray-200 dark:border-gray-700 hover:border-blue-300"
        }`}
        onClick={handleCardClick}>
        <CardHeader className="pb-3">
          <div className="flex items-start justify-between gap-2">
            <div className="flex items-center gap-2 flex-1 min-w-0">
              <Checkbox
                checked={isSelected}
                onCheckedChange={() => onToggleSelection(cell.id)}
                onClick={(e) => e.stopPropagation()}
              />
              <RadioTower className="h-5 w-5 flex-shrink-0 text-blue-600 dark:text-blue-400" />
              <CardTitle className="text-base truncate group-hover:text-blue-600 transition-colors">
                {cell.name || `Cell ${cell.id}`}
              </CardTitle>
            </div>
            <div className="flex items-center gap-2">
              {getStatusBadge(cell.status, handleStatusClick)}
              {/* Actions Dropdown */}
              {!readonly && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 w-8 p-0 opacity-0 group-hover:opacity-100 transition-opacity">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48">
                    <DropdownMenuLabel>Actions</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={handleViewDetails}>
                      <Eye className="mr-2 h-4 w-4" />
                      View Details
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={handleEdit}>
                      <Edit className="mr-2 h-4 w-4" />
                      Edit
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuSub>
                      <DropdownMenuSubTrigger>
                        <Settings2 className="mr-2 h-4 w-4" />
                        Update Status
                      </DropdownMenuSubTrigger>
                      <DropdownMenuSubContent>
                        <DropdownMenuItem onClick={() => handleStatusChange("Unknown")}>
                          <AlertTriangle className="mr-2 h-4 w-4 text-slate-500" />
                          Unknown
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleStatusChange("Pending")}>
                          <Clock className="mr-2 h-4 w-4 text-amber-500" />
                          Pending
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleStatusChange("Licensed")}>
                          <CheckCircle2 className="mr-2 h-4 w-4 text-emerald-500" />
                          Licensed
                        </DropdownMenuItem>
                      </DropdownMenuSubContent>
                    </DropdownMenuSub>
                    <DropdownMenuItem onClick={handleToggleVerified}>
                      <BadgeCheck className={`mr-2 h-4 w-4 ${cell.isVerified ? "text-gray-500" : "text-blue-500"}`} />
                      {cell.isVerified ? "Remove Verified" : "Mark as Verified"}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={handleDelete} className="text-red-600 focus:text-red-600">
                      <Trash className="mr-2 h-4 w-4" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          </div>
          {cell.siteId && <CardDescription className="font-mono text-xs">ID: {cell.siteId}</CardDescription>}
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          {cell.serviceProvider && (
            <div className="flex items-center gap-2">
              {cell.serviceProvider.provider?.logo ? (
                <div
                  className="flex h-6 w-6 items-center justify-center rounded border"
                  style={{ borderColor: cell.serviceProvider.provider?.color || "#e5e7eb" }}>
                  <Image
                    src={cell.serviceProvider.provider.logo}
                    alt={cell.serviceProvider.provider?.name || "Provider"}
                    className="h-full w-full object-contain p-0.5"
                    width={24}
                    height={24}
                  />
                </div>
              ) : (
                <div
                  className="flex h-6 w-6 items-center justify-center rounded text-xs font-bold text-white"
                  style={{ backgroundColor: cell.serviceProvider.provider?.color || "#6b7280" }}>
                  {cell.serviceProvider.provider?.name?.substring(0, 2).toUpperCase() || "??"}
                </div>
              )}
              <span className="font-medium truncate">{cell.serviceProvider.provider?.name || "Unknown Provider"}</span>
            </div>
          )}

          <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400">
            <MapPin className="h-4 w-4 flex-shrink-0" />
            <span className="font-mono text-xs">
              {typeof cell.latitude === "number" ? cell.latitude.toFixed(4) : "N/A"},{" "}
              {typeof cell.longitude === "number" ? cell.longitude.toFixed(4) : "N/A"}
            </span>
          </div>

          {!!cell.elevation && <div className="text-gray-600 dark:text-gray-400">Elevation: {cell.elevation}m</div>}

          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Signal className="h-4 w-4 text-gray-500" />
              <Badge variant={cell.isVerified ? "default" : "outline"} className="text-xs">
                {cell.isVerified ? "✓ Verified" : "Unverified"}
              </Badge>
            </div>
          </div>

          {/* Quick Action Buttons */}
          {!readonly && (
            <div className="flex items-center gap-1 pt-2 border-t mt-2 opacity-0 group-hover:opacity-100 transition-opacity">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-7 px-2" onClick={handleViewDetails}>
                    <Eye className="h-3.5 w-3.5" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>View Details</TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-7 px-2" onClick={handleEdit}>
                    <Edit className="h-3.5 w-3.5" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Edit</TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className={`h-7 px-2 ${cell.isVerified ? "text-blue-600" : ""}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleToggleVerified();
                    }}>
                    <BadgeCheck className="h-3.5 w-3.5" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>{cell.isVerified ? "Remove Verified" : "Mark Verified"}</TooltipContent>
              </Tooltip>
              <div className="flex-1" />
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-red-600 hover:text-red-700 hover:bg-red-50"
                    onClick={handleDelete}>
                    <Trash className="h-3.5 w-3.5" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Delete</TooltipContent>
              </Tooltip>
            </div>
          )}
        </CardContent>
      </Card>
    </motion.div>
  );
});

interface CellsitesTableProps {
  data: Cell[];
  totalCount: number;
  regionId?: string;
  // Filter state and handlers
  searchTerm: string;
  statusFilter: string;
  verifiedFilter: string;
  providerFilter: string;
  provinceFilter: string;
  municipalityFilter: string;
  barangayFilter: string;
  regionFilter: string;
  currentPage: number;
  pageSize: number;
  onSearchChange: (value: string) => void;
  onStatusFilterChange: (value: string) => void;
  onVerifiedFilterChange: (value: string) => void;
  onProviderFilterChange: (value: string) => void;
  onProvinceFilterChange: (value: string) => void;
  onMunicipalityFilterChange: (value: string) => void;
  onBarangayFilterChange: (value: string) => void;
  onRegionFilterChange: (value: string) => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  onClearAllFilters: () => void;
  // Batch operations
  onDeleteSelected: (ids: string[]) => void;
  onUpdateStatusSelected: (ids: string[], status: string) => void;
  // Individual cell actions
  onViewDetails: (id: string) => void;
  onEditCell: (id: string) => void;
  onDeleteCell: (id: string) => void;
  onUpdateCellStatus: (id: string, status: string) => void;
  onToggleCellVerified: (id: string, isVerified: boolean) => void;
  // Loading state
  isLoading?: boolean;
  readonly?: boolean;
  // Toolbar visibility
  toolbarCollapsed?: boolean;
  onToolbarCollapsedChange?: (collapsed: boolean) => void;
  // Add cell site component
  addCellSiteButton?: React.ReactNode;
}

export function CellsitesTable({
  data,
  totalCount,
  regionId,
  // Filter props
  searchTerm,
  statusFilter,
  verifiedFilter,
  providerFilter,
  provinceFilter,
  municipalityFilter,
  barangayFilter,
  regionFilter,
  currentPage,
  pageSize,
  onSearchChange,
  onStatusFilterChange,
  onVerifiedFilterChange,
  onProviderFilterChange,
  onProvinceFilterChange,
  onMunicipalityFilterChange,
  onBarangayFilterChange,
  onRegionFilterChange,
  onPageChange,
  onPageSizeChange,
  onClearAllFilters,
  // Action handlers
  onDeleteSelected,
  onUpdateStatusSelected,
  onViewDetails,
  onEditCell,
  onDeleteCell,
  onUpdateCellStatus,
  onToggleCellVerified,
  isLoading = false,
  readonly = false,
  toolbarCollapsed = false,
  onToolbarCollapsedChange,
  addCellSiteButton,
}: CellsitesTableProps) {
  // Use filter options hook for cascading dropdowns
  const { providers, provinces, municipalities, barangays, regions } = useFilterOptions({
    regionId: regionId || "",
    provinceFilter,
    municipalityFilter,
    serviceType: "wireless",
  });

  // Use the refactored hooks
  const { selectedIds, selectedCount, toggleSelection, toggleSelectAll, clearSelection } = useSelection(data);

  const totalPages = Math.ceil(totalCount / pageSize);

  // Region is pinned if regionId is present in URL (used for filtering context)
  const _isRegionPinned = !!regionId;

  const hasActiveFilters = useMemo(
    () =>
      statusFilter !== "all" ||
      verifiedFilter !== "all" ||
      providerFilter !== "all" ||
      provinceFilter !== "all" ||
      municipalityFilter !== "all" ||
      barangayFilter !== "all" ||
      searchTerm !== "",
    [statusFilter, verifiedFilter, providerFilter, provinceFilter, municipalityFilter, barangayFilter, searchTerm],
  );

  const handleBatchDelete = useCallback(() => {
    onDeleteSelected(Array.from(selectedIds));
    clearSelection();
  }, [selectedIds, onDeleteSelected, clearSelection]);

  const handleBatchStatusUpdate = useCallback(
    (status: string) => {
      onUpdateStatusSelected(Array.from(selectedIds), status);
      clearSelection();
    },
    [selectedIds, onUpdateStatusSelected, clearSelection],
  );

  return (
    <div className="flex flex-col h-full">
      {/* Fixed Toolbar - Always visible, never hidden during loading */}
      <div className="bg-white dark:bg-gray-900 border-b px-6 py-4 sticky top-0 z-30">
        {/* Search and Toggle Row */}
        <div className="flex items-center gap-3">
          <SearchBar
            value={searchTerm}
            onChange={onSearchChange}
            placeholder="Search by provider or site name..."
            className="flex-1 max-w-md"
          />

          {hasActiveFilters && (
            <Button variant="outline" size="sm" className="h-10" onClick={onClearAllFilters}>
              <X className="h-4 w-4 mr-2" />
              Clear Filters
            </Button>
          )}

          {/* Toggle button for filters */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-10"
                onClick={() => onToolbarCollapsedChange?.(!toolbarCollapsed)}>
                {toolbarCollapsed ? (
                  <>
                    <ChevronDown className="h-4 w-4 mr-2" />
                    Show Filters
                  </>
                ) : (
                  <>
                    <ChevronUp className="h-4 w-4 mr-2" />
                    Hide Filters
                  </>
                )}
              </Button>
            </TooltipTrigger>
            <TooltipContent>{toolbarCollapsed ? "Expand filters" : "Collapse filters"}</TooltipContent>
          </Tooltip>

          {/* Spacer to push Add button to right */}
          <div className="flex-1" />

          {/* Add Cell Site Button - Right side */}
          {!readonly && addCellSiteButton}
        </div>

        {/* Filters or Batch Actions - Collapsible */}
        <AnimatePresence mode="wait">
          {!toolbarCollapsed && (
            <motion.div
              key="toolbar-content"
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: "auto" }}
              exit={{ opacity: 0, height: 0 }}
              transition={{ duration: 0.2 }}
              className="overflow-hidden">
              <div className="pt-4 space-y-4 pb-2">
                {selectedIds.size > 0 && !readonly ? (
                  <motion.div
                    key="batch-actions"
                    initial={{ opacity: 0, y: -10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    transition={{ duration: 0.2 }}>
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
                    initial={{ opacity: 0, y: -10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    transition={{ duration: 0.2 }}>
                    <FilterPanel
                      regionId={regionId}
                      regionFilter={regionFilter}
                      onRegionFilterChange={onRegionFilterChange}
                      regions={regions}
                      statusFilter={statusFilter}
                      verifiedFilter={verifiedFilter}
                      providerFilter={providerFilter}
                      provinceFilter={provinceFilter}
                      municipalityFilter={municipalityFilter}
                      barangayFilter={barangayFilter}
                      onStatusFilterChange={onStatusFilterChange}
                      onVerifiedFilterChange={onVerifiedFilterChange}
                      onProviderFilterChange={onProviderFilterChange}
                      onProvinceFilterChange={onProvinceFilterChange}
                      onMunicipalityFilterChange={onMunicipalityFilterChange}
                      onBarangayFilterChange={onBarangayFilterChange}
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

        {/* Stats Bar */}
        <StatsBar
          currentPage={currentPage}
          pageSize={pageSize}
          totalCount={totalCount}
          selectedCount={selectedCount}
          isLoading={isLoading}
          onToggleSelectAll={toggleSelectAll}
          dataLength={data.length}
        />
      </div>

      {/* Card Grid - Scrollable */}
      <div className="flex-1 overflow-y-auto px-6 py-6">
        <AnimatePresence mode="sync">
          {isLoading && data.length === 0 ? (
            <LoadingAnimation />
          ) : data.length > 0 ? (
            <div className="relative">
              {/* Loading overlay - shows over existing data */}
              {isLoading && (
                <div className="absolute inset-0 bg-white/60 dark:bg-gray-900/60 z-10 flex items-center justify-center">
                  <div className="flex items-center gap-3 bg-white dark:bg-gray-800 px-4 py-2 rounded-lg shadow-lg">
                    <Loader2 className="h-5 w-5 animate-spin text-blue-500" />
                    <span className="text-sm font-medium">Updating...</span>
                  </div>
                </div>
              )}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {data.map((cell, index) => (
                  <CellCard
                    key={cell.id}
                    cell={cell}
                    isSelected={selectedIds.has(cell.id)}
                    onToggleSelection={toggleSelection}
                    onViewDetails={onViewDetails}
                    onEdit={onEditCell}
                    onDelete={onDeleteCell}
                    onUpdateStatus={onUpdateCellStatus}
                    onToggleVerified={onToggleCellVerified}
                    index={index}
                    readonly={readonly}
                  />
                ))}
              </div>
            </div>
          ) : (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              className="flex flex-col items-center justify-center py-20 text-gray-500">
              <div className="rounded-full bg-gray-100 dark:bg-gray-800 p-8 mb-6">
                <RadioTower className="h-20 w-20 text-gray-400" />
              </div>
              <p className="text-2xl font-semibold mb-2">No cell sites found</p>
              <p className="text-sm">
                {searchTerm || hasActiveFilters
                  ? "Try adjusting your filters or search terms"
                  : "Cell sites will appear here once they are registered"}
              </p>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Fixed Pagination Footer */}
      <div className="sticky bottom-0 z-30 bg-white dark:bg-gray-900 border-t shadow-lg">
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          pageSize={pageSize}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
          totalCount={totalCount}
        />
      </div>
    </div>
  );
}

export default CellsitesTable;
