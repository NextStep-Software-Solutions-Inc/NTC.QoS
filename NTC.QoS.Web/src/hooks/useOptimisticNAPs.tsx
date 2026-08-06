"use client";

import { useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { createNAPRequest } from "@/components/features/NAP/handlers/createNAPRequest";
import { deleteNAPRequest } from "@/components/features/NAP/handlers/deleteNAPRequest";
import { editNAPRequest } from "@/components/features/NAP/handlers/editNAPRequest";
import type { NAPDetails } from "@/components/features/NAP/types/NAP";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";

// Create mutation hooks
const useCreateNAP = createMutationHookFromTemplate(createNAPRequest);
const useEditNAP = createMutationHookFromTemplate(editNAPRequest);
const useDeleteNAP = createMutationHookFromTemplate(deleteNAPRequest);

/**
 * OptimisticNAP extends base NAP fields with optimistic state tracking.
 * Uses Pick to maintain type safety with NAPDetails.
 */
export interface OptimisticNAP extends Pick<NAPDetails, "id" | "lat" | "lng" | "ISP" | "ISPColor" | "dist"> {
  isOptimistic?: boolean;
  isPending?: boolean;
  hasError?: boolean;
}

interface UseOptimisticNAPsOptions {
  onAddSuccess?: () => void;
  onUpdateSuccess?: () => void;
  onDeleteSuccess?: (napId: string) => void;
  onError?: (error: Error, operation: "add" | "update" | "delete") => void;
}

/**
 * Hook for optimistic NAP operations
 * Provides instant UI feedback while syncing with API in background
 */
export function useOptimisticNAPs(options: UseOptimisticNAPsOptions = {}) {
  const queryClient = useQueryClient();
  const [optimisticNAPs, setOptimisticNAPs] = useState<Map<string, OptimisticNAP>>(new Map());

  const { mutateAsync: createNAPAsync, isPending: isCreating } = useCreateNAP();
  const { mutateAsync: editNAPAsync, isPending: isEditing } = useEditNAP();
  const { mutateAsync: deleteNAPAsync, isPending: isDeleting } = useDeleteNAP();

  /**
   * Add a NAP optimistically
   * Immediately shows the NAP on the map, then syncs with API
   */
  const addNAPOptimistic = useCallback(
    async (napData: {
      serviceProviderId: string;
      latitude: number;
      longitude: number;
      address?: string;
      ispName: string;
      ispColor: string;
    }) => {
      // Generate temporary ID for optimistic update
      const tempId = `temp-${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;

      // Create optimistic NAP
      const optimisticNAP: OptimisticNAP = {
        id: tempId,
        lat: napData.latitude,
        lng: napData.longitude,
        ISP: napData.ispName,
        ISPColor: napData.ispColor,
        isOptimistic: true,
        isPending: true,
        dist: 0,
      };

      // Add to optimistic state immediately
      setOptimisticNAPs((prev) => {
        const next = new Map(prev);
        next.set(tempId, optimisticNAP);
        return next;
      });

      try {
        // Sync with API
        await createNAPAsync({
          pathParams: {},
          payload: {
            serviceProviderId: napData.serviceProviderId,
            latitude: napData.latitude,
            longitude: napData.longitude,
            address: napData.address || "",
            isFileUploaded: false,
          },
        });

        // Remove optimistic NAP and invalidate queries
        setOptimisticNAPs((prev) => {
          const next = new Map(prev);
          next.delete(tempId);
          return next;
        });

        // Invalidate queries to refetch real data
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs", refetchType: "active" });
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs-infinite", refetchType: "all" });
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });

        toast.success("NAP created successfully!");
        options.onAddSuccess?.();
      } catch (error) {
        // Mark optimistic NAP as errored
        setOptimisticNAPs((prev) => {
          const next = new Map(prev);
          const nap = next.get(tempId);
          if (nap) {
            next.set(tempId, { ...nap, isPending: false, hasError: true });
          }
          return next;
        });

        toast.error("Failed to create NAP. Click to retry.");
        options.onError?.(error as Error, "add");
        throw error;
      }
    },
    [createNAPAsync, queryClient, options],
  );

  /**
   * Update a NAP optimistically
   */
  const updateNAPOptimistic = useCallback(
    async (
      napId: string,
      updates: {
        serviceProviderId?: string;
        latitude?: number;
        longitude?: number;
        ispName?: string;
        ispColor?: string;
      },
    ) => {
      // Store original state for rollback
      const previousNAPs = new Map(optimisticNAPs);

      // Apply optimistic update
      setOptimisticNAPs((prev) => {
        const next = new Map(prev);
        const existing = next.get(napId);
        next.set(napId, {
          ...(existing || { id: napId, lat: 0, lng: 0, ISP: "", ISPColor: "", dist: 0 }),
          lat: updates.latitude ?? existing?.lat ?? 0,
          lng: updates.longitude ?? existing?.lng ?? 0,
          ISP: updates.ispName ?? existing?.ISP ?? "",
          ISPColor: updates.ispColor ?? existing?.ISPColor ?? "",
          isOptimistic: true,
          isPending: true,
        });
        return next;
      });

      try {
        await editNAPAsync({
          pathParams: { id: napId },
          payload: {
            serviceProviderId: updates.serviceProviderId || "",
            latitude: updates.latitude || 0,
            longitude: updates.longitude || 0,
          },
        });

        // Remove from optimistic state
        setOptimisticNAPs((prev) => {
          const next = new Map(prev);
          next.delete(napId);
          return next;
        });

        // Invalidate queries
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs", refetchType: "active" });
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs-infinite", refetchType: "all" });
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });

        toast.success("NAP updated successfully!");
        options.onUpdateSuccess?.();
      } catch (error) {
        // Rollback on error
        setOptimisticNAPs(previousNAPs);
        toast.error("Failed to update NAP");
        options.onError?.(error as Error, "update");
        throw error;
      }
    },
    [editNAPAsync, queryClient, options, optimisticNAPs],
  );

  /**
   * Delete a NAP optimistically
   */
  const deleteNAPOptimistic = useCallback(
    async (napId: string) => {
      // Store for rollback
      const previousNAPs = new Map(optimisticNAPs);

      // Mark as pending deletion
      setOptimisticNAPs((prev) => {
        const next = new Map(prev);
        next.set(napId, {
          id: napId,
          lat: 0,
          lng: 0,
          ISP: "",
          ISPColor: "",
          dist: 0,
          isOptimistic: true,
          isPending: true,
        });
        return next;
      });

      try {
        await deleteNAPAsync({ pathParams: {}, queryParams: { ids: napId } });

        // Remove from optimistic state
        setOptimisticNAPs((prev) => {
          const next = new Map(prev);
          next.delete(napId);
          return next;
        });

        // Invalidate queries
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs", refetchType: "active" });
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs-infinite", refetchType: "all" });
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });

        toast.success("NAP deleted successfully!");
        options.onDeleteSuccess?.(napId);
      } catch (error) {
        // Rollback on error
        setOptimisticNAPs(previousNAPs);
        toast.error("Failed to delete NAP");
        options.onError?.(error as Error, "delete");
        throw error;
      }
    },
    [deleteNAPAsync, queryClient, options, optimisticNAPs],
  );

  /**
   * Retry a failed optimistic operation
   */
  const retryFailedNAP = useCallback(
    (napId: string) => {
      const nap = optimisticNAPs.get(napId);
      if (!nap?.hasError) return;

      // Remove the failed NAP and let user try again
      setOptimisticNAPs((prev) => {
        const next = new Map(prev);
        next.delete(napId);
        return next;
      });
    },
    [optimisticNAPs],
  );

  /**
   * Clear all optimistic NAPs (for cleanup)
   */
  const clearOptimistic = useCallback(() => {
    setOptimisticNAPs(new Map());
  }, []);

  return {
    optimisticNAPs: Array.from(optimisticNAPs.values()),
    addNAPOptimistic,
    updateNAPOptimistic,
    deleteNAPOptimistic,
    retryFailedNAP,
    clearOptimistic,
    isCreating,
    isEditing,
    isDeleting,
    isPending: isCreating || isEditing || isDeleting,
  };
}

export default useOptimisticNAPs;
