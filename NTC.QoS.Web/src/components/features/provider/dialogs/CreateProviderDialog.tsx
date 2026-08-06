import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateProviderForm from "../forms/CreateProviderForm";
import { type Provider } from "../types/Provider";

interface CreateProviderDialogProps {
  onCreated?: (provider: Provider) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function CreateProviderDialog({ onCreated, open, onOpenChange }: CreateProviderDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Provider"
      description="Create a new provider by filling out the form below.">
      <CreateProviderForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
