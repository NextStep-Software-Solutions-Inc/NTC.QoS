"use client";

import { AlertTriangle, CheckCircle2, Trash, Wrench, X, XCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";

const MAX_SELECTION_LIMIT = 500;
const WARNING_THRESHOLD = 400; // Show warning at 80% of limit

interface BatchActionsProps {
  selectedCount: number;
  onClearSelection: () => void;
  onUpdateStatus: (status: string) => void;
  onDelete: () => void;
}

export function BatchActions({ selectedCount, onClearSelection, onUpdateStatus, onDelete }: BatchActionsProps) {
  const isOverLimit = selectedCount > MAX_SELECTION_LIMIT;
  const isApproachingLimit = selectedCount >= WARNING_THRESHOLD && selectedCount <= MAX_SELECTION_LIMIT;
  const showWarning = isOverLimit || isApproachingLimit;

  return (
    <div
      className={cn(
        "flex items-center gap-3 p-4 rounded-lg border-2",
        isOverLimit
          ? "bg-red-50 dark:bg-red-950/20 border-red-300 dark:border-red-800"
          : isApproachingLimit
            ? "bg-orange-50 dark:bg-orange-950/20 border-orange-300 dark:border-orange-800"
            : "bg-blue-50 dark:bg-blue-950/20 border-blue-200 dark:border-blue-800",
      )}>
      <div className="flex items-center gap-2">
        <div
          className={cn(
            "h-8 w-8 rounded-full text-white flex items-center justify-center text-sm font-bold",
            isOverLimit
              ? "bg-red-500"
              : isApproachingLimit
                ? "bg-orange-500"
                : "bg-blue-500",
          )}>
          {selectedCount}
        </div>
        <div className="flex flex-col">
          <span className="text-sm font-semibold">NAPs selected</span>
          {showWarning && (
            <span
              className={cn(
                "text-xs flex items-center gap-1",
                isOverLimit ? "text-red-600 dark:text-red-400" : "text-orange-600 dark:text-orange-400",
              )}>
              <AlertTriangle className="h-3 w-3" />
              {isOverLimit
                ? `Exceeds limit of ${MAX_SELECTION_LIMIT}`
                : `Limit: ${MAX_SELECTION_LIMIT}`}
            </span>
          )}
        </div>
      </div>

      {/* Clear Selection Button */}
      <Button variant="ghost" size="sm" onClick={onClearSelection} className="text-gray-600 hover:text-gray-900">
        <X className="h-4 w-4 mr-1" />
        Clear Selection
      </Button>

      <div className="flex-1" />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm" disabled={isOverLimit}>
            <CheckCircle2 className="h-4 w-4 mr-2" />
            Update Status
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent>
          <DropdownMenuLabel>Change status to</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => onUpdateStatus("active")}>
            <CheckCircle2 className="mr-2 h-4 w-4 text-emerald-500" />
            Active
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => onUpdateStatus("inactive")}>
            <XCircle className="mr-2 h-4 w-4 text-slate-500" />
            Inactive
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => onUpdateStatus("maintenance")}>
            <Wrench className="mr-2 h-4 w-4 text-amber-500" />
            Maintenance
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Button variant="destructive" size="sm" onClick={onDelete} disabled={isOverLimit}>
        <Trash className="h-4 w-4 mr-2" />
        Delete
      </Button>
    </div>
  );
}
