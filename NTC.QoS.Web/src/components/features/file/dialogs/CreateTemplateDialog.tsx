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
      title="Update File"
      description="Update the file by filling out the form below.">
      <CreateTemplateForm />
    </ResponsiveDialog>
  );
}
