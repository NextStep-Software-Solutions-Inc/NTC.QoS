"use client";

import { useEffect, useRef } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { CheckCircle2, FileSpreadsheet, Trash2, X } from "lucide-react";
import { createPortal } from "react-dom";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface BatchActionsProps {
  selectedCount: number;
  onDelete: () => void;
  onExport: () => void;
  onClearSelection: () => void;
  isDeleting?: boolean;
}

export function BatchActions({
  selectedCount,
  onDelete,
  onExport,
  onClearSelection,
  isDeleting = false,
}: BatchActionsProps) {
  // Use ref to track mounting without causing re-renders
  const mountedRef = useRef(false);
  const forceUpdate = useRef<(() => void) | null>(null);

  // Force re-render when mounted changes
  useEffect(() => {
    mountedRef.current = true;
    // Trigger a re-render by using a callback
    forceUpdate.current?.();
    return () => {
      mountedRef.current = false;
    };
  }, []);

  // Simple check - on server or before hydration, return null
  if (typeof window === "undefined" || selectedCount === 0) return null;

  const content = (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0, y: 20, scale: 0.95 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 20, scale: 0.95 }}
        className="fixed bottom-6 left-1/2 -translate-x-1/2"
        style={{ transform: "translateX(-50%)" }}>
        <div className="flex items-center gap-3 px-5 py-3 bg-white dark:bg-gray-900 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 backdrop-blur-sm">
          {/* Selection count badge */}
          <Badge className="bg-gradient-to-r from-cyan-500 to-blue-500 text-white border-0 px-3 py-1">
            <CheckCircle2 className="h-3.5 w-3.5 mr-1.5" />
            {selectedCount} selected
          </Badge>

          <div className="w-px h-6 bg-gray-200 dark:bg-gray-700" />

          {/* Export button */}
          <Button
            variant="ghost"
            size="sm"
            onClick={onExport}
            className="h-9 px-4 gap-2 text-green-600 hover:text-green-700 hover:bg-green-50 dark:text-green-400 dark:hover:bg-green-950/30">
            <FileSpreadsheet className="h-4 w-4" />
            Export
          </Button>

          {/* Delete button */}
          <Button
            variant="ghost"
            size="sm"
            onClick={onDelete}
            disabled={isDeleting}
            className="h-9 px-4 gap-2 text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/30">
            <Trash2 className={cn("h-4 w-4", isDeleting && "animate-pulse")} />
            Delete
          </Button>

          <div className="w-px h-6 bg-gray-200 dark:bg-gray-700" />

          {/* Clear selection */}
          <Button
            variant="ghost"
            size="sm"
            onClick={onClearSelection}
            className="h-9 w-9 p-0 text-gray-500 hover:text-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full">
            <X className="h-4 w-4" />
          </Button>
        </div>
      </motion.div>
    </AnimatePresence>
  );

  // Use portal to render outside of any containing scroll context
  return createPortal(content, document.body);
}
