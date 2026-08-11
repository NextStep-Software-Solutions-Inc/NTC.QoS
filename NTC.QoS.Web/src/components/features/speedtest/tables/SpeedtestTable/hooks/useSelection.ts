"use client";

import { useCallback, useState } from "react";

interface UseSelectionReturn<T extends { id: string }> {
  selectedIds: Set<string>;
  selectedItems: T[];
  isSelected: (id: string) => boolean;
  toggleSelection: (id: string) => void;
  toggleSelectAll: (items: T[]) => void;
  clearSelection: () => void;
  selectAll: (items: T[]) => void;
  selectedCount: number;
}

export function useSelection<T extends { id: string }>(items: T[] = []): UseSelectionReturn<T> {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const isSelected = useCallback((id: string) => selectedIds.has(id), [selectedIds]);

  const toggleSelection = useCallback((id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }, []);

  const toggleSelectAll = useCallback((allItems: T[]) => {
    setSelectedIds((prev) => {
      const allIds = new Set(allItems.map((item) => item.id));
      const allSelected = allItems.every((item) => prev.has(item.id));

      if (allSelected) {
        // Deselect all
        return new Set();
      } else {
        // Select all
        return allIds;
      }
    });
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedIds(new Set());
  }, []);

  const selectAll = useCallback((allItems: T[]) => {
    setSelectedIds(new Set(allItems.map((item) => item.id)));
  }, []);

  const selectedItems = items.filter((item) => selectedIds.has(item.id));

  return {
    selectedIds,
    selectedItems,
    isSelected,
    toggleSelection,
    toggleSelectAll,
    clearSelection,
    selectAll,
    selectedCount: selectedIds.size,
  };
}
