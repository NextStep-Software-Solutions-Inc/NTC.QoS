import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditGenerationForm from "../forms/EditGenerationForm";
import { type Generation } from "../types/Generation";

interface EditGenerationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  generation?: Generation;
}
export default function EditGenerationDialog({ open, onOpenChange, generation }: EditGenerationDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Generation"
      description="Edit the generation by filling out the form below.">
      <EditGenerationForm generation={generation} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
