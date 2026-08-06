import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateSystemTypeForm from "../forms/CreateSystemTypeForm";
import { type SystemType } from "../types/SystemType";

interface CreateSystemTypeDialogProps {
  onCreated?: (systemType: SystemType) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function CreateSystemTypeDialog({ onCreated, open, onOpenChange }: CreateSystemTypeDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New System Type"
      description="Create a new system type by filling out the form below.">
      <CreateSystemTypeForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
