import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditTechnologyForm from "../forms/EditTechnologyForm";
import { type Technology } from "../types/Technology";

interface EditTechnologyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  technology?: Technology;
}
export default function EditTechnologyDialog({ open, onOpenChange, technology }: EditTechnologyDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Technology"
      description="Edit the technology by filling out the form below.">
      <EditTechnologyForm technology={technology} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
