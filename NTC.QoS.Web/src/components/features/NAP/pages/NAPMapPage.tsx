"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import dynamic from "next/dynamic";
import { useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, List, Loader2, MapPin } from "lucide-react";
import { toast } from "sonner";

import AddNAPSDialog from "@/components/features/NAP/dialogs/AddNAPSDialog";
import EditNAPDialog from "@/components/features/NAP/dialogs/EditNAPDialog";
import { deleteNAPRequest } from "@/components/features/NAP/handlers/deleteNAPRequest";
import {
  type NAPViewportResponse,
  getNAPViewportEndpoint,
} from "@/components/features/NAP/handlers/getNAPViewportRequest";
import { type NAPDetails, type NAPViewportAdmin } from "@/components/features/NAP/types/NAP";
import { type ViewportBounds } from "@/components/features/NAP/components/NAPMapView/types";
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
import { useOptimisticNAPs } from "@/hooks/useOptimisticNAPs";
import { buildEndpoint } from "@/lib/endpoint-builder";
import { useLocationMap } from "@/providers/LocationMapContext";
import { request } from "@/services/fetch";

// Dynamically import map to avoid SSR issues
const NAPMapView = dynamic(() => import("@/components/features/NAP/components/NAPMapView"), {
  ssr: false,
  loading: () => (
    <div className="h-full flex items-center justify-center bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3" />
        <p className="text-sm text-muted-foreground">Loading map...</p>
      </div>
    </div>
  ),
});

const useDeleteNAP = createMutationHookFromTemplate(deleteNAPRequest);

const DEFAULT_PAGE_SIZE = 2000;
const MAX_VIEWPORT_AREA = 300;

type NormalizedViewportBounds = {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
};

const normalizeBounds = (bounds: ViewportBounds): NormalizedViewportBounds => ({
  minLat: Number(bounds.minLat.toFixed(6)),
  maxLat: Number(bounds.maxLat.toFixed(6)),
  minLng: Number(bounds.minLng.toFixed(6)),
  maxLng: Number(bounds.maxLng.toFixed(6)),
});

const isValidBounds = (bounds: NormalizedViewportBounds): boolean => {
  return bounds.minLat < bounds.maxLat && bounds.minLng < bounds.maxLng;
};

const getBoundsArea = (bounds: NormalizedViewportBounds): number => {
  return (bounds.maxLat - bounds.minLat) * (bounds.maxLng - bounds.minLng);
};

const buildViewportCacheKey = (
  pageNumber: number,
  pageSize: number,
  bounds: NormalizedViewportBounds,
  regionIds: string[],
): string => {
  return `v2_viewport_${pageNumber}_${pageSize}_${bounds.minLat}_${bounds.maxLat}_${bounds.minLng}_${bounds.maxLng}_${regionIds.join(",")}`;
};

const mapViewportItemToNAPDetails = (item: NAPViewportAdmin): NAPDetails | null => {
  const id = item.id ?? item.Id;
  const lat = item.lat ?? item.Lat;
  const lng = item.lng ?? item.Lng;

  if (!id || typeof lat !== "number" || typeof lng !== "number") {
    return null;
  }

  return {
    id,
    lat,
    lng,
    ISP: item.isp ?? item.ISP ?? "Unknown",
    ISPColor: item.ispColor ?? item.ISPColor ?? "#6b7280",
    dist: 0,
    address: item.address ?? item.Address,
    serviceProviderId: item.serviceProviderId ?? item.ServiceProviderId,
  };
};

interface NAPMapPageProps {
  regionId: string;
}

export default function NAPMapPage({ regionId }: NAPMapPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const { resolveUuid } = useLocationMap();
  const currentRegionName = resolveUuid(regionId);

  const latParam = searchParams.get("lat");
  const lngParam = searchParams.get("lng");
  const lat = latParam ? Number(latParam) : NaN;
  const lng = lngParam ? Number(lngParam) : NaN;
  const focusedLocation = Number.isFinite(lat) && Number.isFinite(lng) ? { lat, lng } : null;

  // State
  const [selectedNAP, setSelectedNAP] = useState<NAPDetails | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [pendingAddLocation, setPendingAddLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [napToDelete, setNapToDelete] = useState<NAPDetails | null>(null);

  // Viewport-driven loading state
  const [allNaps, setAllNaps] = useState<NAPDetails[]>([]);
  const [latestBounds, setLatestBounds] = useState<NormalizedViewportBounds | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoadingAll, setIsLoadingAll] = useState(false);
  const [loadingProgress, setLoadingProgress] = useState({ loaded: 0, total: 0 });
  const lastCacheKeyRef = useRef<string | null>(null);
  const requestTokenRef = useRef(0);
  const abortControllerRef = useRef<AbortController | null>(null);

  const fetchViewportPage = useCallback(
    async (
      pageNumber: number,
      pageSize: number,
      bounds: NormalizedViewportBounds,
      regionIds: string[],
      signal?: AbortSignal,
    ): Promise<NAPViewportResponse> => {
      const finalUrl = buildEndpoint(getNAPViewportEndpoint, {}, {
        pageNumber,
        pageSize,
        minLat: bounds.minLat,
        maxLat: bounds.maxLat,
        minLng: bounds.minLng,
        maxLng: bounds.maxLng,
        regionIds,
      });

      return await request.get<NAPViewportResponse>(finalUrl, signal ? { signal } : undefined);
    },
    [],
  );

  const fetchViewportNAPs = useCallback(
    async (bounds: NormalizedViewportBounds, options?: { force?: boolean }) => {
      if (!isValidBounds(bounds)) {
        return;
      }

      if (getBoundsArea(bounds) > MAX_VIEWPORT_AREA) {
        toast.warning("Zoom in to load NAPs in this area");
        return;
      }

      const regionIds = regionId ? [regionId] : [];
      const firstPageKey = buildViewportCacheKey(1, DEFAULT_PAGE_SIZE, bounds, regionIds);

      if (!options?.force && firstPageKey === lastCacheKeyRef.current) {
        return;
      }

      abortControllerRef.current?.abort();
      const controller = new AbortController();
      abortControllerRef.current = controller;

      const requestToken = requestTokenRef.current + 1;
      requestTokenRef.current = requestToken;

      setIsLoadingAll(true);

      try {
        const firstPage = await fetchViewportPage(1, DEFAULT_PAGE_SIZE, bounds, regionIds, controller.signal);

        if (requestTokenRef.current !== requestToken || controller.signal.aborted) {
          return;
        }

        lastCacheKeyRef.current = firstPageKey;

        const firstPageNaps = (firstPage.data || [])
          .map(mapViewportItemToNAPDetails)
          .filter((item): item is NAPDetails => item !== null);

        setAllNaps(firstPageNaps);

        const serverTotalCount = firstPage.totalCount ?? firstPage.totalRows ?? firstPageNaps.length;
        const effectivePageSize = firstPage.pageSize || DEFAULT_PAGE_SIZE;
        const totalPages = Math.ceil(serverTotalCount / Math.max(effectivePageSize, 1));

        setTotalCount(serverTotalCount);
        setLoadingProgress({ loaded: firstPageNaps.length, total: serverTotalCount });

        if (totalPages <= 1) {
          return;
        }

        const collectedById = new Map(firstPageNaps.map((nap) => [nap.id, nap]));
        const MAX_PAGES = 50;
        const pagesToLoad = Math.min(totalPages, MAX_PAGES);

        for (let page = 2; page <= pagesToLoad; page++) {
          if (controller.signal.aborted || requestTokenRef.current !== requestToken) {
            return;
          }

          try {
            const pageResponse = await fetchViewportPage(page, effectivePageSize, bounds, regionIds, controller.signal);

            if (controller.signal.aborted || requestTokenRef.current !== requestToken) {
              return;
            }

            const pageNaps = (pageResponse.data || [])
              .map(mapViewportItemToNAPDetails)
              .filter((item): item is NAPDetails => item !== null);

            pageNaps.forEach((nap) => {
              collectedById.set(nap.id, nap);
            });

            const mergedNAPs = Array.from(collectedById.values());
            setAllNaps(mergedNAPs);
            setLoadingProgress({ loaded: mergedNAPs.length, total: serverTotalCount });
          } catch (error: unknown) {
            if (controller.signal.aborted || requestTokenRef.current !== requestToken) {
              return;
            }
            const err = error as { name?: string; message?: string; response?: { data?: { message?: string } } };
            if (err?.name === "AbortError") return;

            console.error(`Failed to load viewport page ${page}:`, error);
            const errorMessage =
              err?.response?.data?.message ||
              err?.message ||
              `Failed to load NAP data (page ${page}).`;
            toast.error(errorMessage);
            break; // Stop loop on failure to prevent flood of failing requests
          }
        }

        if (!controller.signal.aborted && requestTokenRef.current === requestToken && totalPages > MAX_PAGES) {
          toast.info(
            `Loaded ${collectedById.size.toLocaleString()} of ${serverTotalCount.toLocaleString()} NAPs. Please zoom in to view more details.`,
          );
        }
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }

        const status = (error as { status?: number })?.status;
        if (status === 400) {
          toast.warning("Viewport is too large or invalid. Please zoom in.");
        } else {
          toast.error("Failed to load NAPs for the current viewport");
        }
      } finally {
        if (requestTokenRef.current === requestToken) {
          setIsLoadingAll(false);
        }
      }
    },
    [fetchViewportPage, regionId],
  );

  const previousRegionIdRef = useRef(regionId);

  useEffect(() => {
    if (!latestBounds) {
      return;
    }

    const regionChanged = previousRegionIdRef.current !== regionId;
    if (regionChanged) {
      setAllNaps([]);
      setTotalCount(0);
      setLoadingProgress({ loaded: 0, total: 0 });
      lastCacheKeyRef.current = null;
      previousRegionIdRef.current = regionId;
    }

    void fetchViewportNAPs(latestBounds, { force: regionChanged });
  }, [regionId, latestBounds, fetchViewportNAPs]);

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  // Refetch all data
  const refetchNAPs = useCallback(async () => {
    if (!latestBounds) return;
    await fetchViewportNAPs(latestBounds, { force: true });
  }, [fetchViewportNAPs, latestBounds]);

  // Mutations
  const deleteNAPMutation = useDeleteNAP();

  // Optimistic updates
  const { optimisticNAPs } = useOptimisticNAPs({
    onAddSuccess: () => {
      void refetchNAPs();
      setAddDialogOpen(false);
      setPendingAddLocation(null);
    },
    onDeleteSuccess: () => {
      void refetchNAPs();
    },
  });

  // Use allNaps from infinite loading
  const naps = allNaps;

  // Handlers
  const handleBackToList = useCallback(() => {
    router.push(`/${regionId}/broadband/naps`);
  }, [router, regionId]);

  const handleMapAddNAP = useCallback((lat: number, lng: number) => {
    setPendingAddLocation({ lat, lng });
    setAddDialogOpen(true);
  }, []);

  const handleMapEditNAP = useCallback((nap: NAPDetails) => {
    setSelectedNAP(nap);
    setEditDialogOpen(true);
  }, []);

  const handleEditSuccess = useCallback(() => {
    setSelectedNAP(null);
    setEditDialogOpen(false);
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs", refetchType: "active" });
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });
    void refetchNAPs();
  }, [queryClient, refetchNAPs]);

  const handleMapDeleteNAP = useCallback((nap: NAPDetails) => {
    setNapToDelete(nap);
    setDeleteDialogOpen(true);
  }, []);

  const confirmDelete = async () => {
    if (!napToDelete) return;

    try {
      await deleteNAPMutation.mutateAsync({ pathParams: {}, queryParams: { ids: napToDelete.id } });

      // Optimistically update cache by removing deleted item - instant UI update
      queryClient.setQueriesData(
        { predicate: (query) => query.queryKey[0] === "get-NAPs" },
        (oldData: unknown) => {
          if (!oldData || typeof oldData !== "object" || !(".data" in oldData)) return oldData;

          const data = oldData as { data?: NAPDetails[]; totalRows?: number };

          return {
            ...data,
            data: (data.data || []).filter((nap) => nap.id !== napToDelete.id),
            totalRows: Math.max(0, (data.totalRows || 0) - 1),
          };
        },
      );

      // Invalidate analytics cache to refresh dashboard stats
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });

      toast.success("NAP deleted successfully");
      void refetchNAPs();
    } catch (error) {
      toast.error("Failed to delete NAP");
      console.error("Delete error:", error);
    } finally {
      setDeleteDialogOpen(false);
      setNapToDelete(null);
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header Bar - Mobile Responsive */}
      <header className="flex-shrink-0 border-b bg-gradient-to-r from-slate-50 via-blue-50/30 to-indigo-50/50 dark:from-gray-900 dark:via-gray-900 dark:to-gray-800">
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
              <div className="p-1 sm:p-1.5 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow-sm flex-shrink-0">
                <MapPin className="h-3 w-3 sm:h-4 sm:w-4 text-white" />
              </div>
              <div className="min-w-0">
                <h1 className="text-xs sm:text-sm font-semibold truncate">NAP Geographic Management</h1>
                <div className="flex items-center gap-1.5 text-[9px] sm:text-[10px] text-muted-foreground">
                  <span className="truncate">{currentRegionName || `Region ${regionId}`}</span>
                  <span>•</span>
                  {isLoadingAll && loadingProgress.total > 0 ? (
                    <span className="flex items-center gap-1">
                      <Loader2 className="h-2.5 w-2.5 animate-spin" />
                      {loadingProgress.loaded.toLocaleString()}/{loadingProgress.total.toLocaleString()} NAPs
                    </span>
                  ) : (
                    <span>{totalCount.toLocaleString()} NAPs</span>
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
      <div className="flex-1 min-h-0">
        <NAPMapView
          naps={naps}
          regionId={regionId}
          focusedLocation={focusedLocation}
          onViewportChange={(bounds) => {
            setLatestBounds(normalizeBounds(bounds));
          }}
          isExpanded={true}
          className="h-full w-full rounded-none border-0"
          onAddNAP={handleMapAddNAP}
          onEditNAP={handleMapEditNAP}
          onDeleteNAP={handleMapDeleteNAP}
          onSelectNAP={setSelectedNAP}
          selectedNAPId={selectedNAP?.id}
          optimisticNAPs={optimisticNAPs}
        />
      </div>

      {/* Edit Dialog */}
      {selectedNAP && (
        <EditNAPDialog
          nap={selectedNAP}
          open={editDialogOpen}
          onOpenChange={setEditDialogOpen}
          onSuccess={handleEditSuccess}
        />
      )}

      {/* Add NAP Dialog (triggered from map click) */}
      <AddNAPSDialog
        open={addDialogOpen}
        onOpenChange={setAddDialogOpen}
        defaultLocation={pendingAddLocation || undefined}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete NAP?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete this NAP ({napToDelete?.ISP}). This action cannot be undone.
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
