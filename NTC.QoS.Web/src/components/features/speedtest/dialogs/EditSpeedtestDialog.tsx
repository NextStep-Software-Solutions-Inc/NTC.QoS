"use client";

import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditSpeedtestForm from "../forms/EditSpeedtestForm";
import { type SpeedTest } from "../types/SpeedTest";

interface EditSpeedtestDialogProps {
  speedtest?: SpeedTest;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
  onDelete?: () => void;
}

export default function EditSpeedtestDialog({
  speedtest,
  open,
  onOpenChange,
  onSuccess,
  onDelete,
}: EditSpeedtestDialogProps) {
  const handleSuccess = () => {
    onOpenChange(false);
    onSuccess?.();
  };

  const handleCancel = () => {
    onOpenChange(false);
  };

  const handleDelete = () => {
    onOpenChange(false);
    onDelete?.();
  };

  if (!speedtest) return null;

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Speedtest"
      description="Update the performance data for this speedtest"
      className="sm:max-w-[600px]">
      <EditSpeedtestForm
        speedtest={speedtest}
        onSuccess={handleSuccess}
        onCancel={handleCancel}
        onDelete={handleDelete}
      />
    </ResponsiveDialog>
  );
}
