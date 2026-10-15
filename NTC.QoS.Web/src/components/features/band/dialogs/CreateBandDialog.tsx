import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateBandForm from "../forms/CreateBandForm";
import { type Band } from "../types/Band";

interface CreateBandDialogProps {
  onCreated?: (band: Band) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function CreateBandDialog({ onCreated, open, onOpenChange }: CreateBandDialogProps) {
  return (
    <ResponsiveDialog open={open} onOpenChange={onOpenChange} title="Add New Band" description="">
      <CreateBandForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
