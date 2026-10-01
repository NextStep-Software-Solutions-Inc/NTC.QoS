"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import dynamic from "next/dynamic";
import { useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";

import { CreateSpeedtestDialog, EditSpeedtestDialog } from "@/components/features/speedtest/dialogs";
import { deleteSpeedtestRequest } from "@/components/features/speedtest/handlers/deleteSpeedtestRequest";
import { getSpeedtestsRequest } from "@/components/features/speedtest/handlers/getSpeedtestsRequest";
import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
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
import { useLocationMap } from "@/providers/LocationMapContext";
import { request } from "@/services/fetch";
import { type PaginationResponse } from "@/types/reusable/api";
import type { SpeedtestMapViewProps } from "../components/SpeedtestMapViewAdvanced/types";

// Dynamically import map to avoid SSR issues
const SpeedtestMapView = dynamic<SpeedtestMapViewProps>(() => import("../components/SpeedtestMapViewAdvanced"), {
  ssr: false,
  loading: () => (
    <div className="h-full flex items-center justify-center bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-cyan-600 mx-auto mb-3" />
        <p className="text-sm text-muted-foreground">Loading map...</p>
      </div>
    </div>
  ),
});

// Create hooks
const useGetSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);
const useDeleteSpeedtest = createMutationHookFromTemplate(deleteSpeedtestRequest);

interface SpeedtestMapPageProps {
  regionId: string;
}

export default function SpeedtestMapPage({ regionId }: SpeedtestMapPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { resolveUuid } = useLocationMap();
  const currentRegionName = resolveUuid(regionId);
  const latParam = searchParams.get("lat");
  const lngParam = searchParams.get("lng");
  const lat = latParam ? Number(latParam) : NaN;
  const lng = lngParam ? Number(lngParam) : NaN;
  const focusedLocation = Number.isFinite(lat) && Number.isFinite(lng) ? { lat, lng } : null;

  // Filter params
  const startDate = searchParams.get("startDate") || undefined;
  const endDate = searchParams.get("endDate") || undefined;

  // State
  const [selectedSpeedtest, setSelectedSpeedtest] = useState<SpeedTest | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [pendingAddLocation, setPendingAddLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [speedtestToDelete, setSpeedtestToDelete] = useState<SpeedTest | null>(null);

  // Infinite loading state
  const [allSpeedtests, setAllSpeedtests] = useState<SpeedTest[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoadingAll, setIsLoadingAll] = useState(true);
  const [loadingProgress, setLoadingProgress] = useState({ loaded: 0, total: 0 });
  const loadingRef = useRef(false);
  const PAGE_SIZE = 1000;

  // Statistics computed from loaded data
  const statistics = useMemo(() => {
    if (allSpeedtests.length === 0) return null;
    const avgDownload = allSpeedtests.reduce((sum, st) => sum + (st.downloadSpeed || 0), 0) / allSpeedtests.length;
    const avgUpload = allSpeedtests.reduce((sum, st) => sum + (st.uploadSpeed || 0), 0) / allSpeedtests.length;
    const avgPing = allSpeedtests.reduce((sum, st) => sum + (st.ping || 0), 0) / allSpeedtests.length;
    return { avgDownload, avgUpload, avgPing };
  }, [allSpeedtests]);

  // Reset infinite load state when filters change
  useEffect(() => {
    loadingRef.current = false;
    setAllSpeedtests([]);
    setTotalCount(0);
    setLoadingProgress({ loaded: 0, total: 0 });
    setIsLoadingAll(true);
  }, [regionId, startDate, endDate]);

  // Fetch first page using React Query for proper caching
  const { data: firstPageResponse, refetch: refetchFirstPage } = useGetSpeedtests({
    pathParams: {},
    queryParams: {
      pageSize: PAGE_SIZE,
      pageNumber: 1,
      regionIds: [regionId],
      startDate,
      endDate,
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

      // Start with first page data and allow incremental rendering
      const firstPageData = firstPageResponse.data || [];

      // Render first page immediately so the map can show data incrementally
      setAllSpeedtests(firstPageData);
      setLoadingProgress({ loaded: firstPageData.length, total: totalRows });
      setIsLoadingAll(false);

      // If all data is in first page, we're done
      if (totalPages <= 1) {
        loadingRef.current = false;
        return;
      }

      // Load remaining pages in the background and append to the map
      const allDataCollected = [...firstPageData];

      for (let page = 2; page <= totalPages; page++) {
        try {
          const params = new URLSearchParams({ pageSize: String(PAGE_SIZE), pageNumber: String(page) });
          if (regionId) params.append("regionIds", regionId);
          if (startDate) params.append("startDate", startDate);
          if (endDate) params.append("endDate", endDate);

          const response = await request.get<PaginationResponse<SpeedTest>>(`/speedtests?${params.toString()}`);

          const pageData = response.data || [];
          allDataCollected.push(...pageData);
          setLoadingProgress({ loaded: allDataCollected.length, total: totalRows });

          // Append newly fetched page so the map updates incrementally
          setAllSpeedtests([...allDataCollected]);
        } catch (error) {
          console.error(`Failed to load page ${page}:`, error);
          toast.error(`Failed to load some speedtest data (page ${page})`);
        }
      }

      setAllSpeedtests(allDataCollected);
      setIsLoadingAll(false);
      loadingRef.current = false;
    };

    void loadAllPages();
  }, [firstPageResponse, regionId, startDate, endDate]);

  // Refetch all data
  const refetchSpeedtests = useCallback(async () => {
    loadingRef.current = false;
    setAllSpeedtests([]);
    setLoadingProgress({ loaded: 0, total: 0 });
    await refetchFirstPage();
  }, [refetchFirstPage]);

  // Mutations
  const deleteSpeedtestMutation = useDeleteSpeedtest();

  // Handlers
  const handleDateRangeChange = useCallback(
    (newStart?: string, newEnd?: string) => {
      const params = new URLSearchParams(searchParams.toString());
      if (newStart) {
        params.set("startDate", newStart);
      } else {
        params.delete("startDate");
      }
      if (newEnd) {
        params.set("endDate", newEnd);
      } else {
        params.delete("endDate");
      }
      router.replace(`?${params.toString()}`, { scroll: false });
    },
    [router, searchParams],
  );

  const handleBackToList = useCallback(() => {
    const params = new URLSearchParams();
    if (startDate) params.set("startDate", startDate);
    if (endDate) params.set("endDate", endDate);
    const query = params.toString();
    router.push(`/${regionId}/broadband/speedtests${query ? `?${query}` : ""}`);
  }, [router, regionId, startDate, endDate]);

  const handleMapAddSpeedtest = useCallback((lat: number, lng: number) => {
    setPendingAddLocation({ lat, lng });
    setCreateDialogOpen(true);
  }, []);

  const handleMapEditSpeedtest = useCallback((speedtest: SpeedTest) => {
    setSelectedSpeedtest(speedtest);
    setEditDialogOpen(true);
  }, []);

  const handleMapDeleteSpeedtest = useCallback((speedtest: SpeedTest) => {
    setSpeedtestToDelete(speedtest);
    setDeleteDialogOpen(true);
  }, []);

  const confirmDelete = async () => {
    if (!speedtestToDelete) return;

    try {
      await deleteSpeedtestMutation.mutateAsync({ 
        pathParams: {},
        queryParams: { ids: [speedtestToDelete.id] }
      });
      toast.success("Speedtest deleted successfully");
      void refetchSpeedtests();
    } catch (error) {
      toast.error("Failed to delete speedtest");
      console.error("Delete error:", error);
    } finally {
      setDeleteDialogOpen(false);
      setSpeedtestToDelete(null);
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Full-screen Map with all controls built-in */}
      <SpeedtestMapView
        speedtests={allSpeedtests}
        regionId={regionId}
        focusedLocation={focusedLocation}
        isLoading={isLoadingAll}
        loadingProgress={loadingProgress}
        onAddSpeedtest={handleMapAddSpeedtest}
        onEditSpeedtest={handleMapEditSpeedtest}
        onDeleteSpeedtest={handleMapDeleteSpeedtest}
        onSelectSpeedtest={setSelectedSpeedtest}
        selectedSpeedtestId={selectedSpeedtest?.id}
        onBackToList={handleBackToList}
        regionName={currentRegionName || `Region ${regionId}`}
        startDate={startDate}
        endDate={endDate}
        onDateRangeChange={handleDateRangeChange}
      />

      {/* Edit Dialog */}
      <EditSpeedtestDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        speedtest={selectedSpeedtest || undefined}
        onSuccess={() => {
          setSelectedSpeedtest(null);
          setEditDialogOpen(false);
          void refetchSpeedtests();
        }}
      />

      {/* Create Dialog */}
      <CreateSpeedtestDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        defaultLocation={pendingAddLocation || undefined}
        onSuccess={() => {
          setCreateDialogOpen(false);
          setPendingAddLocation(null);
          void refetchSpeedtests();
        }}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Speedtest?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete this speedtest record
              {speedtestToDelete?.serviceProvider?.provider?.name && (
                <>
                  {" "}
                  from <span className="font-medium">{speedtestToDelete.serviceProvider.provider.name}</span>
                </>
              )}
              . This action cannot be undone.
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
