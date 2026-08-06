import { useState } from "react";
import { toast } from "sonner";

const MAX_SELECTION_LIMIT = 500; // Maximum items that can be selected at once

export function useSelection<T extends { id: string }>(data: T[]) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const toggleSelection = (id: string) => {
    setSelectedIds((prev) => {
      const newSelection = new Set(prev);
      if (newSelection.has(id)) {
        newSelection.delete(id);
      } else {
        // Check selection limit
        if (newSelection.size >= MAX_SELECTION_LIMIT) {
          toast.warning(`Maximum ${MAX_SELECTION_LIMIT} items can be selected at once`, {
            duration: 3000,
          });
          return prev; // Don't add, return previous state
        }
        newSelection.add(id);
      }
      return newSelection;
    });
  };

  const toggleSelectAll = () => {
    setSelectedIds((prev) => {
      if (prev.size === data.length && data.length > 0) {
        return new Set();
      } else {
        // Check if selecting all would exceed limit
        if (data.length > MAX_SELECTION_LIMIT) {
          toast.warning(
            `Cannot select all ${data.length} items. Maximum ${MAX_SELECTION_LIMIT} items can be selected at once. Selected first ${MAX_SELECTION_LIMIT}.`,
            {
              duration: 4000,
            },
          );
          return new Set(data.slice(0, MAX_SELECTION_LIMIT).map((item) => item.id));
        }
        return new Set(data.map((item) => item.id));
      }
    });
  };

  const clearSelection = () => {
    setSelectedIds(new Set());
  };

  const selectedCount = selectedIds.size;
  const selectedItems = data.filter((item) => selectedIds.has(item.id));
  const isAtLimit = selectedIds.size >= MAX_SELECTION_LIMIT;

  return {
    selectedIds,
    selectedCount,
    selectedItems,
    toggleSelection,
    toggleSelectAll,
    clearSelection,
    isAtLimit,
    selectionLimit: MAX_SELECTION_LIMIT,
  };
}
