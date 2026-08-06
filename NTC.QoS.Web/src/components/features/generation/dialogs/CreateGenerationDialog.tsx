import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateGenerationForm from "../forms/CreateGenerationForm";
import { type Generation } from "../types/Generation";

interface CreateGenerationDialogProps {
  onCreated?: (generation: Generation) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function CreateGenerationDialog({ onCreated, open, onOpenChange }: CreateGenerationDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Generation"
      description="Create a new generation by filling out the form below.">
      <CreateGenerationForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
