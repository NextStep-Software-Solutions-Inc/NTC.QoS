import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateCellForm from "../forms/CreateCellForm";
import { type Cell } from "../types/Cell";

interface CreateCellDialogProps {
  onCreated?: (cell: Cell) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialCoordinates?: { lat: number; lng: number } | null;
}
export default function CreateCellDialog({ onCreated, open, onOpenChange, initialCoordinates }: CreateCellDialogProps) {
  const handleCellCreated = (cell: Cell) => {
    onOpenChange(false);
    onCreated?.(cell);
  };

  const handleCancel = () => {
    onOpenChange(false);
  };
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Cell Site"
      description="Fill in the cell site details below."
      size="wide">
      <CreateCellForm
        onCreated={handleCellCreated}
        onCancel={handleCancel}
        initialCoordinates={initialCoordinates ?? undefined}
      />
    </ResponsiveDialog>
  );
}
