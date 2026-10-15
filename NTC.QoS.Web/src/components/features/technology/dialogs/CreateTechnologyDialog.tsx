import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateTechnologyForm from "../forms/CreateTechnologyForm";
import { type Technology } from "../types/Technology";

interface CreateTechnologyDialogProps {
  onCreated?: (technology: Technology) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function CreateTechnologyDialog({ onCreated, open, onOpenChange }: CreateTechnologyDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Technology"
      description="Create a new technology by filling out the form below.">
      <CreateTechnologyForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
