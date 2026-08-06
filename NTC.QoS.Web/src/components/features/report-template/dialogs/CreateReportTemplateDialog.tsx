import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateReportTemplateForm from "../forms/CreateReportTemplateForm";
import { type ReportTemplate } from "../types/ReportTemplate";

interface CreateReportTemplateDialogProps {
  onCreated?: (reportTemplate: ReportTemplate) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function CreateReportTemplateDialog({
  onCreated,
  open,
  onOpenChange,
}: CreateReportTemplateDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Report Template"
      description="Create a new report template for AI-powered report generation.">
      <CreateReportTemplateForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
