import { useState } from "react";

export function useSelection<T extends { id: string }>(data: T[]) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const toggleSelection = (id: string) => {
    setSelectedIds((prev) => {
      const newSelection = new Set(prev);
      if (newSelection.has(id)) {
        newSelection.delete(id);
      } else {
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
        return new Set(data.map((item) => item.id));
      }
    });
  };

  const clearSelection = () => {
    setSelectedIds(new Set());
  };

  const selectedCount = selectedIds.size;
  const selectedItems = data.filter((item) => selectedIds.has(item.id));

  return { selectedIds, selectedCount, selectedItems, toggleSelection, toggleSelectAll, clearSelection };
}
