"use client";

import { useCallback, useState } from "react";
import { toast } from "sonner";

import { createBarangayCoverageRequest } from "@/components/features/barangay-coverage/handlers/createBarangayCoverageRequest";
import { updateBarangayCoverageRequest } from "@/components/features/barangay-coverage/handlers/updateBarangayCoverageRequest";
import { deleteBarangayCoverageRequest } from "@/components/features/barangay-coverage/handlers/deleteBarangayCoverageRequest";
import type { BarangayCoverageLightDto } from "@/components/features/barangay-coverage/types/BarangayCoverage";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";

// Create mutation hooks
const useCreateCoverage = createMutationHookFromTemplate(createBarangayCoverageRequest);
const useUpdateCoverage = createMutationHookFromTemplate(updateBarangayCoverageRequest);
const useDeleteCoverage = createMutationHookFromTemplate(deleteBarangayCoverageRequest);

/**
 * OptimisticBarangayCoverage extends base coverage fields with optimistic state tracking.
 */
export interface OptimisticBarangayCoverage extends BarangayCoverageLightDto {
  isOptimistic?: boolean;
  isPending?: boolean;
  hasError?: boolean; // Track if save operation failed
  isDeleted?: boolean; // Track if this is being deleted
}

interface UseOptimisticBarangayCoverageOptions {
  onSaveSuccess?: (barangayId: string) => void;
  onDeleteSuccess?: (coverageId: string) => void;
  onError?: (error: Error, operation: "create" | "update" | "delete") => void;
}

/**
 * Hook for optimistic barangay coverage operations
 * Provides instant UI feedback while syncing with API in background
 * Implements smart deletion: automatically deletes when both percentages are 0
 * Failed operations keep modified values and highlight error for retry
 */
export function useOptimisticBarangayCoverage(options: UseOptimisticBarangayCoverageOptions = {}) {
  const [optimisticCoverages, setOptimisticCoverages] = useState<Map<string, OptimisticBarangayCoverage>>(new Map());

  const { mutateAsync: createCoverageAsync, isPending: isCreating } = useCreateCoverage();
  const { mutateAsync: updateCoverageAsync, isPending: isUpdating } = useUpdateCoverage();
  const { mutateAsync: deleteCoverageAsync, isPending: isDeleting } = useDeleteCoverage();

  /**
   * Smart save: creates, updates, or deletes coverage based on values
   * - If coverage doesn't exist and values > 0: CREATE
   * - If coverage exists and values > 0: UPDATE
   * - If coverage exists and both values = 0: DELETE
   * - If coverage doesn't exist and both values = 0: DO NOTHING
   */
  const saveCoverageOptimistic = useCallback(
    async (
      barangayId: string,
      barangayName: string,
      existingCoverage: BarangayCoverageLightDto | undefined,
      wiredPercentage: number,
      wirelessPercentage: number,
    ) => {
      const shouldDelete = existingCoverage && wiredPercentage === 0 && wirelessPercentage === 0;
      const shouldCreate = !existingCoverage && (wiredPercentage > 0 || wirelessPercentage > 0);
      const shouldUpdate = existingCoverage && !shouldDelete;

      // Generate temp ID for optimistic operations
      const tempId = existingCoverage?.id || crypto.randomUUID();

      // Skip if no coverage exists and both values are 0
      if (!existingCoverage && wiredPercentage === 0 && wirelessPercentage === 0) {
        return;
      }

      // Create optimistic coverage
      const optimisticCoverage: OptimisticBarangayCoverage = {
        id: tempId,
        barangayId,
        wiredFtthPercentage: wiredPercentage,
        wirelessPercentage: wirelessPercentage,
        combinedPercentage: Math.min(wiredPercentage + wirelessPercentage, 100),
        isWiredCovered: wiredPercentage === 100,
        isWirelessCovered: wirelessPercentage === 100,
        updatedAt: new Date().toISOString(),
        updatedBy: existingCoverage?.updatedBy || "", // Keep existing or use empty string until server updates
        isOptimistic: true,
        isPending: true,
        isDeleted: shouldDelete,
      };

      // Add to optimistic state immediately
      setOptimisticCoverages((prev) => {
        const next = new Map(prev);
        next.set(barangayId, optimisticCoverage);
        return next;
      });

      try {
        if (shouldDelete) {
          // DELETE operation
          await deleteCoverageAsync({
            pathParams: { id: existingCoverage.id },
          });
          toast.success(`Removed coverage for ${barangayName}`);
          options.onDeleteSuccess?.(existingCoverage.id);
        } else if (shouldCreate) {
          // CREATE operation
          await createCoverageAsync({
            pathParams: {},
            payload: {
              barangayId,
              wiredFtthPercentage: wiredPercentage,
              wirelessPercentage: wirelessPercentage,
            },
          });
          toast.success(`Created coverage for ${barangayName}`);
          options.onSaveSuccess?.(barangayId);
        } else if (shouldUpdate) {
          // UPDATE operation
          await updateCoverageAsync({
            pathParams: { id: existingCoverage.id },
            payload: {
              wiredFtthPercentage: wiredPercentage,
              wirelessPercentage: wirelessPercentage,
            },
          });
          toast.success(`Updated coverage for ${barangayName}`);
          options.onSaveSuccess?.(barangayId);
        }

        // Remove optimistic coverage after successful operation
        setOptimisticCoverages((prev) => {
          const next = new Map(prev);
          next.delete(barangayId);
          return next;
        });

        // Do NOT invalidate the large barangay-coverages query to avoid frequent API calls
        // The optimistic update already reflects the change in the UI
      } catch (error) {
        // Keep the optimistic update visible but mark it as errored
        // This allows the user to see what failed and retry
        setOptimisticCoverages((prev) => {
          const next = new Map(prev);
          const coverage = next.get(barangayId);
          if (coverage) {
            next.set(barangayId, { ...coverage, isPending: false, hasError: true });
          }
          return next;
        });

        const operation = shouldDelete ? "delete" : shouldCreate ? "create" : "update";
        toast.error(`Failed to ${operation} coverage for ${barangayName}. Please try again.`);
        options.onError?.(error as Error, operation);
        throw error;
      }
    },
    [createCoverageAsync, updateCoverageAsync, deleteCoverageAsync, options],
  );

  /**
   * Clear error state for a barangay to allow retry
   */
  const clearError = useCallback(
    (barangayId: string) => {
      setOptimisticCoverages((prev) => {
        const next = new Map(prev);
        const coverage = next.get(barangayId);
        if (coverage?.hasError) {
          // Remove the error state but keep the modified values
          next.set(barangayId, { ...coverage, hasError: false });
        }
        return next;
      });
    },
    [],
  );

  /**
   * Clear all optimistic coverages (for cleanup)
   */
  const clearOptimistic = useCallback(() => {
    setOptimisticCoverages(new Map());
  }, []);

  /**
   * Get optimistic coverage for a barangay
   */
  const getOptimisticCoverage = useCallback(
    (barangayId: string) => {
      return optimisticCoverages.get(barangayId);
    },
    [optimisticCoverages],
  );

  return {
    optimisticCoverages: Array.from(optimisticCoverages.values()),
    saveCoverageOptimistic,
    clearError,
    clearOptimistic,
    getOptimisticCoverage,
    isCreating,
    isUpdating,
    isDeleting,
    isPending: isCreating || isUpdating || isDeleting,
  };
}

export default useOptimisticBarangayCoverage;
