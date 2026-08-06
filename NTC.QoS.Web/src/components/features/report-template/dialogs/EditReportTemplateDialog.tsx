import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditReportTemplateForm from "../forms/EditReportTemplateForm";
import { type ReportTemplate } from "../types/ReportTemplate";

interface EditReportTemplateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  reportTemplate?: ReportTemplate;
}

export default function EditReportTemplateDialog({
  open,
  onOpenChange,
  reportTemplate,
}: EditReportTemplateDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Report Template"
      description="Edit the report template by filling out the form below.">
      <EditReportTemplateForm reportTemplate={reportTemplate} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
