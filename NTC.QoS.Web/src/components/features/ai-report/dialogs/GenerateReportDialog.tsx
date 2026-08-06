import React from "react";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import GenerateReportForm from "../forms/GenerateReportForm";

interface GenerateReportDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onGenerated?: () => void;
}

export default function GenerateReportDialog({
  open,
  onOpenChange,
  onGenerated,
}: GenerateReportDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Generate AI Report"
      description="Create comprehensive reports using AI-powered analysis."
      size="wide"
    >
      <GenerateReportForm
        onGenerated={onGenerated}
        onClose={() => onOpenChange(false)}
      />
    </ResponsiveDialog>
  );
}
