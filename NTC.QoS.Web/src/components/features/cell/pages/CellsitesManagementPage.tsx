"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

import AddCellSiteDialog from "@/components/Tables/AddCellSiteDialog";
import { CellsiteHero } from "@/components/features/cell/components/CellsiteHero";
import { EditCellDialog } from "@/components/features/cell/dialogs/EditCellDialog";
import { deleteCellsRequest } from "@/components/features/cell/handlers/deleteCellsRequest";
import { editCellStatusRequest } from "@/components/features/cell/handlers/editCellStatusRequest";
import { editCellRequest } from "@/components/features/cell/handlers/editCellRequest";
import { exportCellsRequest } from "@/components/features/cell/handlers/exportCellsRequest";
import { getCellsRequest } from "@/components/features/cell/handlers/getCellsRequest";
import { CellsitesTable } from "@/components/features/cell/tables/CellsitesTable";
import { type Cell } from "@/components/features/cell/types/Cell";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useBroadbandFilters } from "@/hooks/filters";
import type { BroadbandStatistics } from "@/hooks/filters/types";
import { useLocationMap } from "@/providers/LocationMapContext";

// Create hooks
const useGetCells = createQueryHookFromTemplate(getCellsRequest);
const useDeleteCells = createMutationHookFromTemplate(deleteCellsRequest);
const useUpdateCellStatus = createMutationHookFromTemplate(editCellStatusRequest);
const useEditCell = createMutationHookFromTemplate(editCellRequest);

interface CellsitesManagementPageProps {
  regionId: string;
}

export default function CellsitesManagementPage({ regionId }: CellsitesManagementPageProps) {
  const router = useRouter();
  const containerRef = useRef<HTMLDivElement>(null);
  const scrollPositionRef = useRef(0);

  // Hero state
  const [isHeroCollapsed, setIsHeroCollapsed] = useState(false);
  const [toolbarCollapsed, setToolbarCollapsed] = useState(false);

  // Handle scroll events for hero collapse
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const handleScroll = () => {
      const scrollTop = container.scrollTop;
      scrollPositionRef.current = scrollTop;
      // Auto collapse when scrolled more than 80px, expand when back at top (< 20px)
      if (scrollTop > 80 && !isHeroCollapsed) {
        setIsHeroCollapsed(true);
      } else if (scrollTop < 20 && isHeroCollapsed) {
        setIsHeroCollapsed(false);
      }
    };

    container.addEventListener("scroll", handleScroll, { passive: true });
    return () => {
      container.removeEventListener("scroll", handleScroll);
    };
  }, [isHeroCollapsed]);

  // Toggle hero collapse manually
  const handleToggleHeroCollapse = useCallback(() => {
    setIsHeroCollapsed((prev) => !prev);
    // Scroll to top if expanding
    if (isHeroCollapsed && containerRef.current) {
      containerRef.current.scrollTo({ top: 0, behavior: "smooth" });
    }
  }, [isHeroCollapsed]);

  // Use the reusable broadband filters hook
  const {
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
  } = useBroadbandFilters({ regionId: regionId, basePath: "broadband/cellsites" });

  // Batch delete confirmation state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [idsToDelete, setIdsToDelete] = useState<string[]>([]);

  // Edit cell dialog state
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [cellToEdit, setCellToEdit] = useState<Cell | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  // Fetch cells with all filters including region
  const {
    data: cellsResponse,
    isLoading: cellsLoading,
    refetch: refetchCells,
  } = useGetCells({
    pathParams: {},
    queryParams: {
      pageSize,
      pageNumber: currentPage,
      search: debouncedSearchTerm || undefined,
      status: statusFilter !== "all" ? statusFilter : undefined,
      isVerified: verifiedFilter !== "all" ? verifiedFilter === "true" : undefined,
      serviceProviderIds: providerFilter !== "all" ? [providerFilter] : undefined,
      provinceIds: provinceFilter !== "all" ? [provinceFilter] : undefined,
      municipalityIds: municipalityFilter !== "all" ? [municipalityFilter] : undefined,
      barangayIds: barangayFilter !== "all" ? [barangayFilter] : undefined,
      regionIds: [regionId], // Filter by region from URL param
    },
  });

  const { resolveUuid } = useLocationMap();

  // Mutations
  const deleteCellsMutation = useDeleteCells();
  const updateStatusMutation = useUpdateCellStatus();
  const editCellMutation = useEditCell();

  const cells = cellsResponse?.data || [];
  const totalCount = cellsResponse?.totalRows || 0;

  // Fetch all cells for statistics calculation (with large pageSize)
  const { data: allCellsResponse } = useGetCells({
    pathParams: {},
    queryParams: {
      pageSize: 10000, // Large number to get all cells for statistics
      pageNumber: 1,
      regionIds: [regionId], // Filter by region from URL param
    },
  });

  const allCells = allCellsResponse?.data || [];

  // Calculate statistics from all cells
  const statistics = useMemo((): BroadbandStatistics => {
    if (allCells.length === 0) {
      return {
        totalCount: 0,
        verifiedCount: 0,
        unverifiedCount: 0,
        topProviders: [],
        statusBreakdown: { pending: 0, unknown: 0, licensed: 0 },
      };
    }

    // Count verified/unverified
    const verifiedCount = allCells.filter((cell) => cell.isVerified).length;
    const unverifiedCount = allCells.length - verifiedCount;

    // Count by status (pending, unknown, licensed)
    const statusBreakdown = allCells.reduce(
      (acc, cell) => {
        const status = cell.status?.toLowerCase() || "unknown";
        if (status === "pending") acc.pending++;
        else if (status === "licensed") acc.licensed++;
        else acc.unknown++;
        return acc;
      },
      { pending: 0, unknown: 0, licensed: 0 },
    );

    // Count by provider and get top 3
    const providerCounts = allCells.reduce(
      (acc, cell) => {
        if (cell.serviceProvider) {
          const id = cell.serviceProvider.id;
          if (!acc[id]) {
            acc[id] = {
              id,
              name: cell.serviceProvider.provider.name,
              color: cell.serviceProvider.provider.color || "#3b82f6",
              count: 0,
            };
          }
          acc[id].count++;
        }
        return acc;
      },
      {} as Record<string, { id: string; name: string; color: string; count: number }>,
    );

    const topProviders = Object.values(providerCounts)
      .sort((a, b) => b.count - a.count)
      .slice(0, 3);

    return {
      totalCount: allCells.length,
      verifiedCount,
      unverifiedCount,
      topProviders,
      statusBreakdown,
    };
  }, [allCells]);

  // Find current region name
  const currentRegionName = resolveUuid(regionId);

  const handleDeleteSelected = (ids: string[]) => {
    setIdsToDelete(ids);
    setDeleteDialogOpen(true);
  };

  const confirmDelete = async () => {
    try {
      await deleteCellsMutation.mutateAsync({ pathParams: {}, queryParams: { ids: idsToDelete } });
      toast.success(`Successfully deleted ${idsToDelete.length} cell site(s)`);
      refetchCells();
      setDeleteDialogOpen(false);
      setIdsToDelete([]);
    } catch (error) {
      toast.error("Failed to delete cell sites");
      console.error("Delete error:", error);
    }
  };

  const handleUpdateStatusSelected = async (ids: string[], status: string) => {
    try {
      const results = await Promise.allSettled(
        ids.map((id) => updateStatusMutation.mutateAsync({ pathParams: { id }, payload: status })),
      );

      const successCount = results.filter((r) => r.status === "fulfilled").length;
      const failureCount = results.filter((r) => r.status === "rejected").length;

      if (successCount > 0) {
        toast.success(`Successfully updated status for ${successCount} cell site(s)`);
      }
      if (failureCount > 0) {
        toast.error(`Failed to update ${failureCount} cell site(s)`);
      }

      refetchCells();
    } catch (error) {
      toast.error("Failed to update cell status");
      console.error("Update status error:", error);
    }
  };

  // Individual cell action handlers
  const handleViewDetails = useCallback(
    (id: string, queryParams?: string) => {
      const url = `/${regionId}/broadband/cellsites/${id}`;
      router.push(queryParams ? `${url}?${queryParams}` : url);
    },
    [router, regionId],
  );

  const handleEditCell = useCallback(
    (id: string) => {
      router.push(`/${regionId}/broadband/cellsites/${id}?edit=true`);
    },
    [router, regionId],
  );

  const handleDeleteCell = useCallback((id: string) => {
    setIdsToDelete([id]);
    setDeleteDialogOpen(true);
  }, []);

  const handleUpdateCellStatus = useCallback(
    async (id: string, status: string) => {
      try {
        await updateStatusMutation.mutateAsync({ pathParams: { id }, payload: status });
        toast.success("Status updated successfully");
        refetchCells();
      } catch (error) {
        toast.error("Failed to update status");
        console.error("Update status error:", error);
      }
    },
    [updateStatusMutation, refetchCells],
  );

  const handleToggleCellVerified = useCallback(
    async (id: string, isVerified: boolean) => {
      try {
        await editCellMutation.mutateAsync({
          pathParams: { id },
          payload: { isVerified },
        });
        toast.success(isVerified ? "Marked as verified" : "Removed verified status");
        refetchCells();
      } catch (error) {
        toast.error("Failed to update verified status");
        console.error("Update verified error:", error);
      }
    },
    [editCellMutation, refetchCells],
  );

  // No longer needed - using dialog instead
  // const handleAddCellSite = useCallback(() => {
  //   router.push(`/${regionId}/broadband/cellsites/new`);
  // }, [router, regionId]);

  const handleOpenFullscreenMap = useCallback(() => {
    router.push(`/${regionId}/broadband/cellsites/map`);
  }, [router, regionId]);

  const handleExport = useCallback(
    async (format: "Excel" | "Pdf") => {
      try {
        setIsExporting(true);
        await exportCellsRequest({
          format,
          search: debouncedSearchTerm || undefined,
          status: statusFilter !== "all" ? statusFilter : undefined,
          isVerified: verifiedFilter !== "all" ? verifiedFilter === "true" : undefined,
          serviceProviderIds: providerFilter !== "all" ? [providerFilter] : undefined,
          provinceIds: provinceFilter !== "all" ? [provinceFilter] : undefined,
          municipalityIds: municipalityFilter !== "all" ? [municipalityFilter] : undefined,
          barangayIds: barangayFilter !== "all" ? [barangayFilter] : undefined,
          regionIds: [regionId],
        });
        toast.success(`Cell sites exported to ${format === "Excel" ? "Excel" : "PDF"} successfully`);
      } catch (error) {
        toast.error(`Failed to export cell sites to ${format === "Excel" ? "Excel" : "PDF"}`);
        console.error("Export error:", error);
      } finally {
        setIsExporting(false);
      }
    },
    [
      debouncedSearchTerm,
      statusFilter,
      verifiedFilter,
      providerFilter,
      provinceFilter,
      municipalityFilter,
      barangayFilter,
      regionId,
    ],
  );

  const handleExportExcel = useCallback(() => handleExport("Excel"), [handleExport]);
  const handleExportPdf = useCallback(() => handleExport("Pdf"), [handleExport]);

  return (
    <div ref={containerRef} className="flex flex-col h-screen overflow-auto">
      {/* Animated Hero Section */}
      <CellsiteHero
        regionName={currentRegionName || `Region ${regionId}`}
        totalCount={totalCount}
        statistics={statistics}
        isCollapsed={isHeroCollapsed}
        onToggleCollapse={handleToggleHeroCollapse}
        onOpenFullscreenMap={handleOpenFullscreenMap}
      />

      {/* Main Content Area */}
      <div className="flex-1 min-h-0">
        <CellsitesTable
          data={cells}
          totalCount={totalCount}
          regionId={regionId}
          // Filter state
          searchTerm={searchTerm}
          statusFilter={statusFilter}
          verifiedFilter={verifiedFilter}
          providerFilter={providerFilter}
          provinceFilter={provinceFilter}
          municipalityFilter={municipalityFilter}
          barangayFilter={barangayFilter}
          regionFilter={regionFilter}
          currentPage={currentPage}
          pageSize={pageSize}
          // Filter handlers
          onSearchChange={setSearchTerm}
          onStatusFilterChange={setStatusFilter}
          onVerifiedFilterChange={setVerifiedFilter}
          onProviderFilterChange={setProviderFilter}
          onProvinceFilterChange={setProvinceFilter}
          onMunicipalityFilterChange={setMunicipalityFilter}
          onBarangayFilterChange={setBarangayFilter}
          onRegionFilterChange={setRegionFilter}
          onPageChange={setCurrentPage}
          onPageSizeChange={setPageSize}
          onClearAllFilters={() => clearAllFilters()}
          // Action handlers
          onDeleteSelected={handleDeleteSelected}
          onUpdateStatusSelected={handleUpdateStatusSelected}
          onViewDetails={handleViewDetails}
          onEditCell={handleEditCell}
          onDeleteCell={handleDeleteCell}
          onUpdateCellStatus={handleUpdateCellStatus}
          onToggleCellVerified={handleToggleCellVerified}
          isLoading={cellsLoading}
          readonly={false}
          toolbarCollapsed={toolbarCollapsed}
          onToolbarCollapsedChange={setToolbarCollapsed}
          addCellSiteButton={<AddCellSiteDialog />}
          onExportExcel={handleExportExcel}
          onExportPdf={handleExportPdf}
          isExporting={isExporting}
        />
      </div>

      {/* Edit Cell Dialog */}
      {cellToEdit && (
        <EditCellDialog cell={cellToEdit} open={editDialogOpen} onOpenChange={setEditDialogOpen} />
      )}

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete {idsToDelete.length} cell site(s). This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
