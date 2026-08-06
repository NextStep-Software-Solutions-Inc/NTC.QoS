"use client";

import React from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteNAPRequest } from "../handlers/deleteNAPRequest";
import { type NAP, type NAPDetails } from "../types/NAP";

const useDeleteNAP = createMutationHookFromTemplate(deleteNAPRequest);

interface DeleteNAPDialogProps {
  nap?: NAPDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

export default function DeleteNAPDialog({ nap, open, onOpenChange, onSuccess }: DeleteNAPDialogProps) {
  const queryClient = useQueryClient();
  const { mutateAsync, isPending } = useDeleteNAP();

  const handleDelete = async () => {
    if (!nap) return;

    toast.promise(mutateAsync({ pathParams: {}, queryParams: { ids: nap.id } }), {
      loading: "Deleting NAP...",
      success: () => {
        // Optimistically update cache by removing deleted item - instant UI update
        queryClient.setQueriesData(
          { predicate: (query) => query.queryKey[0] === "get-NAPs" },
          (oldData: unknown) => {
            if (!oldData || typeof oldData !== "object" || !("data" in oldData)) return oldData;
            
            const data = oldData as { data?: NAP[]; totalRows?: number };
            
            return {
              ...data,
              data: (data.data || []).filter((item) => item.id !== nap.id),
              totalRows: Math.max(0, (data.totalRows || 0) - 1),
            };
          },
        );
        
        // Invalidate analytics cache to refresh dashboard stats
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });
        
        onOpenChange(false);
        onSuccess?.();
        return "NAP deleted successfully!";
      },
      error: "Failed to delete NAP. Please try again.",
    });
  };

  if (!nap) return null;

  return (
    <ResponsiveDialog open={open} onOpenChange={onOpenChange} title="Delete Network Access Point" description="">
      <div className="space-y-4">
        <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-950">
          <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          <p className="text-sm text-amber-800 dark:text-amber-200">
            This action cannot be undone. The NAP and all associated data will be permanently deleted.
          </p>
        </div>

        <div className="rounded-lg border bg-muted/50 p-4">
          <p className="text-sm text-muted-foreground mb-2">You are about to delete:</p>
          <div className="space-y-1">
            <p className="font-medium">{nap.ISP}</p>
            <p className="text-sm text-muted-foreground font-mono">
              {nap.lat?.toFixed(6)}, {nap.lng?.toFixed(6)}
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={handleDelete} disabled={isPending} className="gap-2">
            <Trash2 className="h-4 w-4" />
            {isPending ? "Deleting..." : "Delete NAP"}
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  );
}
