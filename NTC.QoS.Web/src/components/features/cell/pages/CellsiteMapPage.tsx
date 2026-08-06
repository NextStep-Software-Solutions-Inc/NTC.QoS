"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import dynamic from "next/dynamic";
import { useRouter, useSearchParams } from "next/navigation";
import { ArrowLeft, List, Loader2, Radio } from "lucide-react";
import { toast } from "sonner";

import { CellSiteDetails } from "@/components/admin-map/components/CellSiteDetails";
import { cellToCellMapDetails, type CellMapDetails } from "@/components/features/cell/components/CellsiteMapView/types";
import { getBandsRequest } from "@/components/features/band/handlers/getBandsRequest";
import CreateCellDialog from "@/components/features/cell/dialogs/CreateCellDialog";
import EditCellDialog from "@/components/features/cell/dialogs/EditCellsDialog";
import { deleteCellsRequest } from "@/components/features/cell/handlers/deleteCellsRequest";
import { getCellsRequest } from "@/components/features/cell/handlers/getCellsRequest";
import { getGenerationsRequest } from "@/components/features/generation/handlers/getGenerationsRequest";
import { getTechnologiesRequest } from "@/components/features/technology/handlers/getTechnologiesRequest";
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
import { Button } from "@/components/ui/button";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useLocationMap } from "@/providers/LocationMapContext";
import { request } from "@/services/fetch";
import { type APIResponse, type PaginationResponse } from "@/types/reusable/api";

// Dynamically import map to avoid SSR issues
const CellsiteMapView = dynamic(() => import("@/components/features/cell/components/CellsiteMapView"), {
  ssr: false,
  loading: () => (
    <div className="h-full flex items-center justify-center bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-red-600 mx-auto mb-3" />
        <p className="text-sm text-muted-foreground">Loading map...</p>
      </div>
    </div>
  ),
});

// Create hooks
const useGetCells = createQueryHookFromTemplate(getCellsRequest);
const useGetGenerations = createQueryHookFromTemplate(getGenerationsRequest);
const useGetBands = createQueryHookFromTemplate(getBandsRequest);
const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useDeleteCells = createMutationHookFromTemplate(deleteCellsRequest);

interface CellsiteMapPageProps {
  regionId: string;
}

export default function CellsiteMapPage({ regionId }: CellsiteMapPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { resolveUuid } = useLocationMap();
  const currentRegionName = resolveUuid(regionId);

  const latParam = searchParams.get("lat");
  const lngParam = searchParams.get("lng");
  const lat = latParam ? Number(latParam) : NaN;
  const lng = lngParam ? Number(lngParam) : NaN;
  const focusedLocation = Number.isFinite(lat) && Number.isFinite(lng) ? { lat, lng } : null;

  // State
  const [selectedCell, setSelectedCell] = useState<CellMapDetails | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [pendingAddLocation, setPendingAddLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [cellToDelete, setCellToDelete] = useState<CellMapDetails | null>(null);
  const [cellToEdit, setCellToEdit] = useState<Cell | null>(null);
  const [showCellDetails, setShowCellDetails] = useState(false);
  const [selectedAdvancedFilters, setSelectedAdvancedFilters] = useState<{
    generations: string[];
    bands: string[];
    technologies: string[];
  }>({
    generations: [],
    bands: [],
    technologies: [],
  });

  // Infinite loading state
  const [allCells, setAllCells] = useState<CellMapDetails[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoadingAll, setIsLoadingAll] = useState(true);
  const [loadingProgress, setLoadingProgress] = useState({ loaded: 0, total: 0 });
  const loadingRef = useRef(false);
  const PAGE_SIZE = 1000;

  const { data: generationsResponse } = useGetGenerations({
    pathParams: {},
    queryParams: { pageSize: 200, pageNumber: 1 },
  });

  const { data: bandsResponse } = useGetBands({
    pathParams: {},
    queryParams: { pageSize: 500, pageNumber: 1 },
  });

  const { data: technologiesResponse } = useGetTechnologies({
    pathParams: {},
    queryParams: { pageSize: 500, pageNumber: 1 },
  });

  const selectedGenerationIds = useMemo(() => {
    if (selectedAdvancedFilters.generations.length === 0) return [];

    const generations = generationsResponse?.data ?? [];
    const nameToId = new Map(generations.map((g) => [g.name.trim().toLowerCase(), g.id]));

    return selectedAdvancedFilters.generations
      .map((name) => nameToId.get(name.trim().toLowerCase()))
      .filter((id): id is string => Boolean(id));
  }, [selectedAdvancedFilters.generations, generationsResponse]);

  const selectedBandIds = useMemo(() => {
    if (selectedAdvancedFilters.bands.length === 0) return [];

    const bands = bandsResponse?.data ?? [];
    const nameToId = new Map(bands.map((band) => [band.name.trim().toLowerCase(), band.id]));

    return selectedAdvancedFilters.bands
      .map((name) => nameToId.get(name.trim().toLowerCase()))
      .filter((id): id is string => Boolean(id));
  }, [selectedAdvancedFilters.bands, bandsResponse]);

  const selectedTechnologyIds = useMemo(() => {
    if (selectedAdvancedFilters.technologies.length === 0) return [];

    const technologies = technologiesResponse?.data ?? [];
    const nameToId = new Map(technologies.map((tech) => [tech.standard.trim().toLowerCase(), tech.id]));

    return selectedAdvancedFilters.technologies
      .map((name) => nameToId.get(name.trim().toLowerCase()))
      .filter((id): id is string => Boolean(id));
  }, [selectedAdvancedFilters.technologies, technologiesResponse]);

  const generationOptions = useMemo(
    () => Array.from(new Set((generationsResponse?.data ?? []).map((generation) => generation.name))).sort(),
    [generationsResponse],
  );

  const bandOptions = useMemo(
    () => Array.from(new Set((bandsResponse?.data ?? []).map((band) => band.name))).sort(),
    [bandsResponse],
  );

  const technologyOptions = useMemo(
    () => Array.from(new Set((technologiesResponse?.data ?? []).map((technology) => technology.standard))).sort(),
    [technologiesResponse],
  );

  // Fetch first page using React Query for proper caching
  const { data: firstPageResponse, refetch: refetchFirstPage } = useGetCells({
    pathParams: {},
    queryParams: {
      pageSize: PAGE_SIZE,
      pageNumber: 1,
      regionIds: [regionId],
      generationIds: selectedGenerationIds.length > 0 ? selectedGenerationIds : undefined,
      bandIds: selectedBandIds.length > 0 ? selectedBandIds : undefined,
      technologyIds: selectedTechnologyIds.length > 0 ? selectedTechnologyIds : undefined,
    },
  });

  // Infinite loading effect - load all pages after first page
  useEffect(() => {
    if (!firstPageResponse || loadingRef.current) return;

    const loadAllPages = async () => {
      if (loadingRef.current) return;
      loadingRef.current = true;
      setIsLoadingAll(true);

      const totalRows = firstPageResponse.totalRows || 0;
      const totalPages = Math.ceil(totalRows / PAGE_SIZE);

      setTotalCount(totalRows);
      setLoadingProgress({ loaded: firstPageResponse.data?.length || 0, total: totalRows });

      // Start with first page data
      const firstPageCells: CellMapDetails[] = (firstPageResponse.data || []).map(cellToCellMapDetails);

      // If all data is in first page, we're done
      if (totalPages <= 1) {
        setAllCells(firstPageCells);
        setIsLoadingAll(false);
        loadingRef.current = false;
        return;
      }

      // Load remaining pages
      const allCellsCollected = [...firstPageCells];

      for (let page = 2; page <= totalPages; page++) {
        try {
          // Build URL with query params
          const params = new URLSearchParams({ pageSize: String(PAGE_SIZE), pageNumber: String(page) });
          regionId && params.append("regionIds", regionId);
          selectedGenerationIds.forEach((id) => params.append("generationIds", id));
          selectedBandIds.forEach((id) => params.append("bandIds", id));
          selectedTechnologyIds.forEach((id) => params.append("technologyIds", id));

          const response = await request.get<PaginationResponse<Cell>>(`/cells?${params.toString()}`);

          const pageCells: CellMapDetails[] = (response.data || []).map(cellToCellMapDetails);

          allCellsCollected.push(...pageCells);
          setLoadingProgress({ loaded: allCellsCollected.length, total: totalRows });

          // Update incrementally so map shows data as it loads
          setAllCells([...allCellsCollected]);
        } catch (error) {
          console.error(`Failed to load page ${page}:`, error);
          toast.error(`Failed to load some cellsite data (page ${page})`);
        }
      }

      setAllCells(allCellsCollected);
      setIsLoadingAll(false);
      loadingRef.current = false;
    };

    void loadAllPages();
  }, [firstPageResponse, regionId, selectedGenerationIds, selectedBandIds, selectedTechnologyIds]);

  // Refetch all data
  const refetchCells = useCallback(async () => {
    loadingRef.current = false;
    setAllCells([]);
    setLoadingProgress({ loaded: 0, total: 0 });
    await refetchFirstPage();
  }, [refetchFirstPage]);

  // Mutations
  const deleteCellsMutation = useDeleteCells();

  // Handlers
  const handleBackToList = useCallback(() => {
    router.push(`/${regionId}/broadband/cellsites`);
  }, [router, regionId]);

  const handleMapAddCell = useCallback((lat: number, lng: number) => {
    setPendingAddLocation({ lat, lng });
    setAddDialogOpen(true);
  }, []);

  const handleMapEditCell = useCallback(async (cell: CellMapDetails) => {
    setSelectedCell(cell);

    try {
      // Fetch full cell details for editing
      const response = await request.get<APIResponse<Cell>>(`/cells/${cell.id}`);
      if (response.data) {
        setCellToEdit(response.data);
        setEditDialogOpen(true);
      } else {
        toast.error("Cell site not found - it may have been deleted");
      }
    } catch (error) {
      console.error("Failed to fetch cell details:", error);
      toast.error("Network error while loading cell details. Please try again.");
    }
  }, []);

  const handleMapDeleteCell = useCallback((cell: CellMapDetails) => {
    setCellToDelete(cell);
    setDeleteDialogOpen(true);
  }, []);

  // Handler for "View Details" button in popup
  const handleViewCellDetails = useCallback((cellId: string) => {
    setShowCellDetails(true);
    // Also update URL for CellSiteDetails component
    const params = new URLSearchParams(window.location.search);
    params.set("cellId", cellId);
    window.history.replaceState({}, "", `${window.location.pathname}?${params.toString()}`);
  }, []);

  // Close cell details panel
  const handleCloseCellDetails = useCallback(() => {
    setShowCellDetails(false);
    const params = new URLSearchParams(window.location.search);
    params.delete("cellId");
    window.history.replaceState({}, "", `${window.location.pathname}?${params.toString()}`);
  }, []);

  const confirmDelete = async () => {
    if (!cellToDelete) return;

    try {
      await deleteCellsMutation.mutateAsync({ pathParams: {}, queryParams: { ids: [cellToDelete.id] } });
      toast.success("Cell site deleted successfully");
      void refetchCells();
    } catch (error) {
      toast.error("Failed to delete cell site");
      console.error("Delete error:", error);
    } finally {
      setDeleteDialogOpen(false);
      setCellToDelete(null);
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header Bar - Mobile Responsive */}
      <header className="flex-shrink-0 border-b bg-gradient-to-r from-slate-50 via-red-50/30 to-orange-50/50 dark:from-gray-900 dark:via-gray-900 dark:to-gray-800">
        <div className="px-2 sm:px-4 py-2 sm:py-3 flex items-center justify-between gap-2">
          <div className="flex items-center gap-2 sm:gap-3 min-w-0">
            <Button
              variant="ghost"
              size="sm"
              onClick={handleBackToList}
              className="gap-1 sm:gap-2 hover:bg-white/50 dark:hover:bg-gray-800/50 h-8 px-2 sm:px-3">
              <ArrowLeft className="h-4 w-4" />
              <span className="hidden sm:inline">Back to List</span>
            </Button>
            <div className="hidden sm:block h-6 w-px bg-border" />
            <div className="flex items-center gap-2 min-w-0">
              <div className="p-1 sm:p-1.5 rounded-lg bg-gradient-to-br from-red-500 to-orange-600 shadow-sm flex-shrink-0">
                <Radio className="h-3 w-3 sm:h-4 sm:w-4 text-white" />
              </div>
              <div className="min-w-0">
                <h1 className="text-xs sm:text-sm font-semibold truncate">Cell Site Geographic Management</h1>
                <div className="flex items-center gap-1.5 text-[9px] sm:text-[10px] text-muted-foreground">
                  <span className="truncate">{currentRegionName || `Region ${regionId}`}</span>
                  <span>•</span>
                  {isLoadingAll && loadingProgress.total > 0 ? (
                    <span className="flex items-center gap-1">
                      <Loader2 className="h-2.5 w-2.5 animate-spin" />
                      {loadingProgress.loaded.toLocaleString()}/{loadingProgress.total.toLocaleString()} Cell Sites
                    </span>
                  ) : (
                    <span>{totalCount.toLocaleString()} Cell Sites</span>
                  )}
                </div>
              </div>
            </div>
          </div>
          <div className="flex items-center gap-1 sm:gap-2 flex-shrink-0">
            <Button variant="outline" size="sm" onClick={handleBackToList} className="gap-1 sm:gap-2 h-8 px-2 sm:px-3">
              <List className="h-3.5 w-3.5" />
              <span className="hidden sm:inline">Table View</span>
            </Button>
          </div>
        </div>
      </header>

      {/* Full-screen Map */}
      <div className="flex-1 min-h-0 relative">
        <CellsiteMapView
          cells={allCells}
          isExpanded={true}
          className="h-full w-full rounded-none border-0"
          onAddCell={handleMapAddCell}
          onEditCell={handleMapEditCell}
          onDeleteCell={handleMapDeleteCell}
          onSelectCell={setSelectedCell}
          onViewCellDetails={handleViewCellDetails}
          onAdvancedFiltersChange={setSelectedAdvancedFilters}
          filterOptions={{
            generations: generationOptions,
            bands: bandOptions,
            technologies: technologyOptions,
          }}
          selectedCellId={selectedCell?.id}
          optimisticCells={[]}
          regionId={regionId}
          focusedLocation={focusedLocation}
        />

        {/* CellSiteDetails Panel - rendered when cellId is in URL */}
        {showCellDetails && (
          <CellSiteDetails className="!fixed !right-4 !top-20 !bottom-4 !w-[380px]" onClose={handleCloseCellDetails} />
        )}
      </div>

      {/* Edit Dialog */}
      {cellToEdit && (
        <EditCellDialog
          cell={cellToEdit}
          open={editDialogOpen}
          onOpenChange={(open) => {
            setEditDialogOpen(open);
            if (!open) {
              setCellToEdit(null);
              setSelectedCell(null);
            }
          }}
        />
      )}

      {/* Add Cell Dialog (triggered from map click) */}
      <CreateCellDialog
        open={addDialogOpen}
        onOpenChange={(open) => {
          setAddDialogOpen(open);
          if (!open) {
            setPendingAddLocation(null);
          }
        }}
        initialCoordinates={pendingAddLocation}
        onCreated={() => {
          setAddDialogOpen(false);
          setPendingAddLocation(null);
          void refetchCells();
        }}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Cell Site?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete this cell site ({cellToDelete?.name || cellToDelete?.providerName}). This
              action cannot be undone.
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
