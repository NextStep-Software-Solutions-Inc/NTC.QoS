"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import NAPAnalyticsPanel from "@/components/features/NAP/analytics/NAPAnalyticsPanel";
import { BulkDeleteDialog } from "@/components/features/NAP/components/BulkDeleteDialog";
import { NAPHero } from "@/components/features/NAP/components/NAPHero";
import AddNAPSDialog from "@/components/features/NAP/dialogs/AddNAPSDialog";
import EditNAPDialog from "@/components/features/NAP/dialogs/EditNAPDialog";
import { createNAPRequest } from "@/components/features/NAP/handlers/createNAPRequest";
import { deleteNAPRequest } from "@/components/features/NAP/handlers/deleteNAPRequest";
import { getNAPsRequest } from "@/components/features/NAP/handlers/getNAPsRequest";
import { NAPTable } from "@/components/features/NAP/tables/NAPTable";
import { type NAP, type NAPDetails } from "@/components/features/NAP/types/NAP";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useBroadbandFilters } from "@/hooks/filters";
import { useOptimisticNAPs } from "@/hooks/useOptimisticNAPs";
import { useLocationMap } from "@/providers/LocationMapContext";

// Create hooks
const useGetNAPs = createQueryHookFromTemplate(getNAPsRequest);
const useDeleteNAP = createMutationHookFromTemplate(deleteNAPRequest);
const useCreateNAP = createMutationHookFromTemplate(createNAPRequest);

interface NAPsManagementPageProps {
  regionId: string;
}

export default function NAPsManagementPage({ regionId }: NAPsManagementPageProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const containerRef = useRef<HTMLDivElement>(null);
  const scrollPositionRef = useRef(0);

  // State
  const [isHeroCollapsed, setIsHeroCollapsed] = useState(false);
  const [toolbarCollapsed, setToolbarCollapsed] = useState(false);
  const [analyticsCollapsed, setAnalyticsCollapsed] = useState(true);
  const [selectedNAP, setSelectedNAP] = useState<NAPDetails | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [addDialogOpen, setAddDialogOpen] = useState(false);

  // Handle scroll events for hero collapse
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const handleScroll = () => {
      const scrollTop = container.scrollTop;
      scrollPositionRef.current = scrollTop;
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
    if (isHeroCollapsed && containerRef.current) {
      containerRef.current.scrollTo({ top: 0, behavior: "smooth" });
    }
  }, [isHeroCollapsed]);

  // Use broadband filters hook
  const {
    searchTerm,
    providerFilter,
    provinceFilter,
    municipalityFilter,
    barangayFilter,
    regionFilter,
    currentPage,
    pageSize,
    setSearchTerm,
    setProviderFilter,
    setProvinceFilter,
    setMunicipalityFilter,
    setBarangayFilter,
    setRegionFilter,
    setCurrentPage,
    setPageSize,
    clearAllFilters,
  } = useBroadbandFilters({ regionId: regionId, basePath: "broadband/naps" });

  // Delete confirmation state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [idsToDelete, setIdsToDelete] = useState<string[]>([]);

  // Fetch NAPs
  const {
    data: napsResponse,
    isLoading: napsLoading,
    refetch: refetchNAPs,
  } = useGetNAPs({
    pathParams: {},
    queryParams: {
      pageSize,
      pageNumber: currentPage,
      regionIds: [regionId],
      serviceProviderIds: providerFilter !== "all" ? [providerFilter] : undefined,
      provinceIds: provinceFilter !== "all" ? [provinceFilter] : undefined,
      municipalityIds: municipalityFilter !== "all" ? [municipalityFilter] : undefined,
      barangayIds: barangayFilter !== "all" ? [barangayFilter] : undefined,
    },
  });

  const { resolveUuid } = useLocationMap();

  // Mutations
  const deleteNAPMutation = useDeleteNAP();
  const _createNAPMutation = useCreateNAP();

  // Optimistic updates
  const {
    optimisticNAPs: _optimisticNAPs,
    addNAPOptimistic: _addNAPOptimistic,
    deleteNAPOptimistic: _deleteNAPOptimistic,
    isPending: isOptimisticPending,
  } = useOptimisticNAPs({
    onAddSuccess: () => {
      void refetchNAPs();
      setAddDialogOpen(false);
    },
    onDeleteSuccess: () => {
      void refetchNAPs();
    },
  });

  // Transform NAP data
  const naps: NAPDetails[] = (napsResponse?.data || []).map((nap) => ({
    id: nap.id,
    lat: nap.lat,
    lng: nap.lng,
    ISP: nap.ISP || "Unknown",
    ISPColor: nap.ISPColor || "#6b7280",
    dist: nap.dist || 0,
  }));

  const totalCount = napsResponse?.totalRows || 0;
  const currentRegionName = resolveUuid(regionId);

  // Handlers
  const handleDeleteSelected = (ids: string[]) => {
    setIdsToDelete(ids);
    setDeleteDialogOpen(true);
  };

  const handleConfirmDelete = async (ids: string[]) => {
    // Delete a chunk of NAPs
    await deleteNAPMutation.mutateAsync({ pathParams: {}, queryParams: { ids } });
    
    // Optimistically update cache by removing deleted items - instant UI update
    queryClient.setQueriesData(
      { predicate: (query) => query.queryKey[0] === "get-NAPs" },
      (oldData: unknown) => {
        if (!oldData || typeof oldData !== "object" || !("data" in oldData)) return oldData;
        
        const data = oldData as { data?: NAP[]; totalRows?: number };
        const idsSet = new Set(Array.isArray(ids) ? ids : [ids]);
        
        return {
          ...data,
          data: (data.data || []).filter((nap) => !idsSet.has(nap.id)),
          totalRows: Math.max(0, (data.totalRows || 0) - idsSet.size),
        };
      },
    );
    
    // Invalidate analytics cache to refresh dashboard stats
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs-infinite", refetchType: "all" });
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });
  };

  const handleUpdateStatusSelected = async (ids: string[], _status: string) => {
    toast.info(`Status update for ${ids.length} NAP(s) - API not yet implemented`);
  };

  const handleEditNAP = useCallback((nap: NAPDetails) => {
    setSelectedNAP(nap);
    setEditDialogOpen(true);
  }, []);

  const handleDeleteNAP = useCallback((nap: NAPDetails) => {
    setIdsToDelete([nap.id]);
    setDeleteDialogOpen(true);
  }, []);

  // Navigate to map route with selected NAP
  const handleViewOnMap = useCallback(
    (nap: NAPDetails) => {
      const params = new URLSearchParams();
      if (typeof nap.lat === "number") params.set("lat", String(nap.lat));
      if (typeof nap.lng === "number") params.set("lng", String(nap.lng));
      const query = params.toString();
      router.push(`/${regionId}/broadband/naps/map${query ? `?${query}` : ""}`);
    },
    [router, regionId],
  );

  return (
    <div ref={containerRef} className="flex flex-col h-screen overflow-auto">
      {/* Hero Section */}
      <NAPHero
        regionName={currentRegionName || `Region ${regionId}`}
        totalCount={totalCount}
        isCollapsed={isHeroCollapsed}
        showMap={false}
        onToggleCollapse={handleToggleHeroCollapse}
        onToggleMap={() => router.push(`/${regionId}/broadband/naps/map`)}
      />

      {/* Analytics Panel - Collapsible */}
      <div className="px-4 pt-4">
        <NAPAnalyticsPanel
          regionIds={[regionId]}
          serviceProviderIds={providerFilter !== "all" ? [providerFilter] : undefined}
          provinceIds={provinceFilter !== "all" ? [provinceFilter] : undefined}
          municipalityIds={municipalityFilter !== "all" ? [municipalityFilter] : undefined}
          barangayIds={barangayFilter !== "all" ? [barangayFilter] : undefined}
          isCollapsed={analyticsCollapsed}
          onCollapsedChange={setAnalyticsCollapsed}
        />
      </div>

      {/* Main Content - Table */}
      <div className="flex-1 min-h-0">
        <NAPTable
          data={naps}
          totalCount={totalCount}
          regionId={regionId}
          searchTerm={searchTerm}
          providerFilter={providerFilter}
          provinceFilter={provinceFilter}
          municipalityFilter={municipalityFilter}
          barangayFilter={barangayFilter}
          regionFilter={regionFilter}
          currentPage={currentPage}
          pageSize={pageSize}
          onSearchChange={setSearchTerm}
          onProviderFilterChange={setProviderFilter}
          onProvinceFilterChange={setProvinceFilter}
          onMunicipalityFilterChange={setMunicipalityFilter}
          onBarangayFilterChange={setBarangayFilter}
          onRegionFilterChange={setRegionFilter}
          onPageChange={setCurrentPage}
          onPageSizeChange={setPageSize}
          onClearAllFilters={() => clearAllFilters()}
          onDeleteSelected={handleDeleteSelected}
          onUpdateStatusSelected={handleUpdateStatusSelected}
          onEditNAP={handleEditNAP}
          onDeleteNAP={handleDeleteNAP}
          onViewOnMap={handleViewOnMap}
          isLoading={napsLoading || isOptimisticPending}
          readonly={false}
          toolbarCollapsed={toolbarCollapsed}
          onToolbarCollapsedChange={setToolbarCollapsed}
        />
      </div>

      {/* Edit Dialog */}
      {selectedNAP && (
        <EditNAPDialog
          nap={selectedNAP}
          open={editDialogOpen}
          onOpenChange={setEditDialogOpen}
          onSuccess={() => {
            setSelectedNAP(null);
            setEditDialogOpen(false);
            // Invalidate both NAP list and analytics for real-time updates
            void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs", refetchType: "active" });
            void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs-infinite", refetchType: "all" });
            void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });
          }}
        />
      )}

      {/* Add NAP Dialog (triggered from map click) */}
      <AddNAPSDialog open={addDialogOpen} onOpenChange={setAddDialogOpen} />

      {/* Bulk Delete Dialog with Progress */}
      <BulkDeleteDialog
        open={deleteDialogOpen}
        onOpenChange={(open) => {
          setDeleteDialogOpen(open);
          if (!open) {
            setIdsToDelete([]);
            void refetchNAPs();
          }
        }}
        idsToDelete={idsToDelete}
        onConfirmDelete={handleConfirmDelete}
        itemName="NAP"
      />
    </div>
  );
}
