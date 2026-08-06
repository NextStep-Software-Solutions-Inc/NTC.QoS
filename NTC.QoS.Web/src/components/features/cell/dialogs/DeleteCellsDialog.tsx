/**
 * Delete Cells Dialog with Progress Tracking
 * Handles large-scale cell deletions with chunked requests and progress UI
 */

import { useCallback, useState } from "react";
import { AlertTriangle, CheckCircle2, Loader2, XCircle } from "lucide-react";
import { toast } from "sonner";

import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";

const CHUNK_SIZE = 100; // Maximum IDs per request
const MAX_SELECTION_LIMIT = 500; // Maximum cells that can be deleted at once

interface DeleteCellsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  idsToDelete: string[];
  onConfirmDelete: (ids: string[]) => Promise<void>;
}

type DeletionState = "idle" | "confirming" | "processing" | "completed" | "error";

interface DeletionProgress {
  total: number;
  processed: number;
  successful: number;
  failed: number;
  currentChunk: number;
  totalChunks: number;
}

export default function DeleteCellsDialog({
  open,
  onOpenChange,
  idsToDelete,
  onConfirmDelete,
}: DeleteCellsDialogProps) {
  const [state, setState] = useState<DeletionState>("confirming");
  const [progress, setProgress] = useState<DeletionProgress>({
    total: 0,
    processed: 0,
    successful: 0,
    failed: 0,
    currentChunk: 0,
    totalChunks: 0,
  });

  const count = idsToDelete.length;
  const isOverLimit = count > MAX_SELECTION_LIMIT;
  const needsChunking = count > CHUNK_SIZE;
  const totalChunks = Math.ceil(count / CHUNK_SIZE);

  // Split IDs into chunks
  const chunkIds = useCallback((ids: string[]): string[][] => {
    const chunks: string[][] = [];
    for (let i = 0; i < ids.length; i += CHUNK_SIZE) {
      chunks.push(ids.slice(i, i + CHUNK_SIZE));
    }
    return chunks;
  }, []);

  // Process deletion in chunks
  const handleDelete = useCallback(async () => {
    if (isOverLimit) {
      toast.error(`Cannot delete more than ${MAX_SELECTION_LIMIT} cells at once`);
      return;
    }

    setState("processing");

    const chunks = chunkIds(idsToDelete);
    const newProgress: DeletionProgress = {
      total: count,
      processed: 0,
      successful: 0,
      failed: 0,
      currentChunk: 0,
      totalChunks: chunks.length,
    };

    setProgress(newProgress);

    try {
      for (let i = 0; i < chunks.length; i++) {
        const chunk = chunks[i]!;

        try {
          await onConfirmDelete(chunk);
          newProgress.successful += chunk.length;
        } catch (error) {
          console.error(`Failed to delete chunk ${i + 1}:`, error);
          newProgress.failed += chunk.length;
        }

        newProgress.processed += chunk.length;
        newProgress.currentChunk = i + 1;
        setProgress({ ...newProgress });

        // Small delay between chunks to prevent overwhelming the server
        if (i < chunks.length - 1) {
          await new Promise((resolve) => setTimeout(resolve, 100));
        }
      }

      setState("completed");

      // Show success/failure summary
      if (newProgress.failed === 0) {
        toast.success(`Successfully deleted ${newProgress.successful} cell(s)`);
      } else if (newProgress.successful === 0) {
        toast.error(`Failed to delete all ${newProgress.failed} cell(s)`);
      } else {
        toast.warning(
          `Deleted ${newProgress.successful} cell(s), failed to delete ${newProgress.failed}`,
        );
      }

      // Close dialog after short delay
      setTimeout(() => {
        onOpenChange(false);
        resetState();
      }, 2000);
    } catch (error) {
      console.error("Bulk delete error:", error);
      setState("error");
      toast.error("Failed to delete cells");
    }
  }, [isOverLimit, chunkIds, idsToDelete, count, onConfirmDelete, onOpenChange]);

  const resetState = useCallback(() => {
    setState("confirming");
    setProgress({
      total: 0,
      processed: 0,
      successful: 0,
      failed: 0,
      currentChunk: 0,
      totalChunks: 0,
    });
  }, []);

  const handleCancel = useCallback(() => {
    if (state !== "processing") {
      onOpenChange(false);
      setTimeout(resetState, 150);
    }
  }, [state, onOpenChange, resetState]);

  const progressPercentage = progress.total > 0 ? (progress.processed / progress.total) * 100 : 0;

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="sm:max-w-[500px]">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div
              className={cn(
                "flex h-12 w-12 items-center justify-center rounded-full",
                state === "completed" && progress.failed === 0
                  ? "bg-green-100 dark:bg-green-900/30"
                  : state === "completed" || state === "processing"
                    ? "bg-orange-100 dark:bg-orange-900/30"
                    : "bg-red-100 dark:bg-red-900/30",
              )}>
              {state === "processing" ? (
                <Loader2 className="h-6 w-6 animate-spin text-orange-600 dark:text-orange-400" />
              ) : state === "completed" ? (
                <CheckCircle2 className="h-6 w-6 text-green-600 dark:text-green-400" />
              ) : (
                <AlertTriangle className="h-6 w-6 text-red-600 dark:text-red-400" />
              )}
            </div>
            <div>
              <AlertDialogTitle className="text-lg">
                {state === "processing"
                  ? "Deleting Cells"
                  : state === "completed"
                    ? "Deletion Complete"
                    : state === "error"
                      ? "Deletion Failed"
                      : "Delete Cells"}
              </AlertDialogTitle>
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {count} record{count !== 1 ? "s" : ""} selected
              </p>
            </div>
          </div>

          <AlertDialogDescription className="pt-4">
            <div className="space-y-4">
              {/* Confirming State */}
              {state === "confirming" && (
                <>
                  <p className="text-sm text-gray-700 dark:text-gray-300">
                    You are about to permanently delete {count} cell{count !== 1 ? "s" : ""}. This
                    action cannot be undone.
                  </p>

                  {isOverLimit && (
                    <div className="rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3">
                      <p className="text-sm text-red-800 dark:text-red-200 font-medium">
                        Selection limit exceeded
                      </p>
                      <p className="text-xs text-red-700 dark:text-red-300 mt-1">
                        You can delete a maximum of {MAX_SELECTION_LIMIT} cells at once. Please
                        reduce your selection.
                      </p>
                    </div>
                  )}

                  {needsChunking && !isOverLimit && (
                    <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 p-3">
                      <p className="text-sm text-blue-800 dark:text-blue-200">
                        This will be processed in {totalChunks} batch{totalChunks !== 1 ? "es" : ""}{" "}
                        of up to {CHUNK_SIZE} cells each.
                      </p>
                    </div>
                  )}
                </>
              )}

              {/* Processing State */}
              {state === "processing" && (
                <div className="space-y-3">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-700 dark:text-gray-300 font-medium">
                      Processing batch {progress.currentChunk} of {progress.totalChunks}
                    </span>
                    <span className="text-gray-600 dark:text-gray-400">
                      {progress.processed} / {progress.total}
                    </span>
                  </div>

                  <Progress value={progressPercentage} className="h-2" />

                  <div className="flex items-center gap-4 text-xs">
                    <div className="flex items-center gap-1">
                      <CheckCircle2 className="h-3.5 w-3.5 text-green-500" />
                      <span className="text-gray-700 dark:text-gray-300">
                        {progress.successful} successful
                      </span>
                    </div>
                    {progress.failed > 0 && (
                      <div className="flex items-center gap-1">
                        <XCircle className="h-3.5 w-3.5 text-red-500" />
                        <span className="text-gray-700 dark:text-gray-300">
                          {progress.failed} failed
                        </span>
                      </div>
                    )}
                  </div>

                  <p className="text-xs text-gray-500 dark:text-gray-400 italic">
                    Please wait while we process your request...
                  </p>
                </div>
              )}

              {/* Completed State */}
              {state === "completed" && (
                <div className="space-y-2">
                  <div
                    className={cn(
                      "rounded-lg p-3 border",
                      progress.failed === 0
                        ? "bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800"
                        : "bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-800",
                    )}>
                    <div className="flex items-center justify-between text-sm">
                      <span
                        className={
                          progress.failed === 0
                            ? "text-green-800 dark:text-green-200"
                            : "text-orange-800 dark:text-orange-200"
                        }>
                        Processed: {progress.total}
                      </span>
                    </div>
                    <div className="mt-2 flex gap-4 text-xs">
                      <span className="text-green-700 dark:text-green-300">
                        ✓ Success: {progress.successful}
                      </span>
                      {progress.failed > 0 && (
                        <span className="text-red-700 dark:text-red-300">
                          ✗ Failed: {progress.failed}
                        </span>
                      )}
                    </div>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 text-center">
                    Closing automatically...
                  </p>
                </div>
              )}

              {/* Error State */}
              {state === "error" && (
                <div className="rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3">
                  <p className="text-sm text-red-800 dark:text-red-200">
                    An error occurred during deletion. Please try again or contact support if the
                    issue persists.
                  </p>
                </div>
              )}
            </div>
          </AlertDialogDescription>
        </AlertDialogHeader>

        <AlertDialogFooter>
          {state === "confirming" && (
            <>
              <AlertDialogCancel onClick={handleCancel}>Cancel</AlertDialogCancel>
              <Button
                onClick={handleDelete}
                disabled={isOverLimit}
                variant="destructive"
                className="min-w-24">
                Delete {count > 0 && `(${count})`}
              </Button>
            </>
          )}

          {state === "processing" && (
            <Button disabled variant="ghost" className="min-w-24">
              <Loader2 className="h-4 w-4 animate-spin mr-2" />
              Processing...
            </Button>
          )}

          {(state === "completed" || state === "error") && (
            <Button onClick={handleCancel} variant="secondary" className="min-w-24">
              Close
            </Button>
          )}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
