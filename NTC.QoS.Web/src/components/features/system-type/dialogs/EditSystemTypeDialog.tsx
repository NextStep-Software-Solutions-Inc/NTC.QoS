import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditSystemTypeForm from "../forms/EditSystemTypeForm";
import { type SystemType } from "../types/SystemType";

interface EditSystemTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  systemType?: SystemType;
}
export default function EditSystemTypeDialog({ open, onOpenChange, systemType }: EditSystemTypeDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit System Type"
      description="Edit the system type by filling out the form below.">
      <EditSystemTypeForm systemType={systemType} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
