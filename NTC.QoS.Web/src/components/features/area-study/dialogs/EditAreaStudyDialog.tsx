import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditAreaStudyForm from "../forms/EditAreaStudyForm";
import { type AreaStudy } from "../types/AreaStudy";

interface EditAreaStudyDialogProps {
  areaStudy?: AreaStudy;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function EditAreaStudyDialog({ areaStudy, open, onOpenChange }: EditAreaStudyDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Area Study"
      description=""
      // description="Create a new study by filling out the form below."
    >
      <EditAreaStudyForm areaStudy={areaStudy} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
