import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateAreaStudyForm from "../forms/CreateAreaStudyForm";
import { type AreaStudy } from "../types/AreaStudy";

interface CreateAreaStudyDialogProps {
  onCreated?: (areaStudy: AreaStudy) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function CreateAreaStudyDialog({ onCreated, open, onOpenChange }: CreateAreaStudyDialogProps) {
  const handleAreaStudyCreated = (areaStudy: AreaStudy) => {
    onOpenChange(false);
    onCreated?.(areaStudy);
  };

  const handleCancel = () => {
    onOpenChange(false);
  };
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Area Study"
      description=""
      // description="Create a new area study by filling out the form below."
    >
      <CreateAreaStudyForm onCreated={handleAreaStudyCreated} onCancel={handleCancel} />
    </ResponsiveDialog>
  );
}
