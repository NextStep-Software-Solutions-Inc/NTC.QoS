import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateTemplateForm from "../forms/CreateTemplateForm";

interface CreateTemplateDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  onCreate?: () => void;
}
export default function CreateTemplateDialog({ open, onOpenChange }: CreateTemplateDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Create Template"
      description="Create a new template by filling out the form below.">
      <CreateTemplateForm />
    </ResponsiveDialog>
  );
}
