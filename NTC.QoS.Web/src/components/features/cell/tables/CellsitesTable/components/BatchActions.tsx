"use client";

import { CheckCircle2, Trash, X, XCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface BatchActionsProps {
  selectedCount: number;
  onClearSelection: () => void;
  onUpdateStatus: (status: string) => void;
  onDelete: () => void;
}

export function BatchActions({ selectedCount, onClearSelection, onUpdateStatus, onDelete }: BatchActionsProps) {
  return (
    <div className="flex items-center gap-3 p-4 bg-blue-50 dark:bg-blue-950/20 rounded-lg border-2 border-blue-200 dark:border-blue-800">
      <div className="flex items-center gap-2">
        <div className="h-8 w-8 rounded-full bg-blue-500 text-white flex items-center justify-center text-sm font-bold">
          {selectedCount}
        </div>
        <span className="text-sm font-semibold">items selected</span>
      </div>

      {/* Clear Selection Button */}
      <Button variant="ghost" size="sm" onClick={onClearSelection} className="text-gray-600 hover:text-gray-900">
        <X className="h-4 w-4 mr-1" />
        Clear Selection
      </Button>

      <div className="flex-1" />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm">
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
            <XCircle className="mr-2 h-4 w-4 text-amber-500" />
            Maintenance
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Button variant="destructive" size="sm" onClick={onDelete}>
        <Trash className="h-4 w-4 mr-2" />
        Delete
      </Button>
    </div>
  );
}
