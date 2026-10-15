import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditCellForm from "../forms/EditCellForm";
import { type Cell } from "../types/Cell";

interface EditCellDialogProps {
  cell?: Cell;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function EditCellDialog({ cell, open, onOpenChange }: EditCellDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Cell Site"
      description="Update the cell site details below."
      size="wide">
      <EditCellForm cell={cell} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
