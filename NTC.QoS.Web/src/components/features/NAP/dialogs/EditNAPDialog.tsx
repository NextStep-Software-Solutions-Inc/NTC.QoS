"use client";

import React, { memo } from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditNAPForm from "../forms/EditNAPForm";
import { type NAPDetails } from "../types/NAP";

interface EditNAPDialogProps {
  nap?: NAPDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

function EditNAPDialog({ nap, open, onOpenChange, onSuccess }: EditNAPDialogProps) {
  const handleSuccess = () => {
    onOpenChange(false);
    onSuccess?.();
  };

  const handleCancel = () => {
    onOpenChange(false);
  };

  if (!nap) return null;

  return (
    <ResponsiveDialog
      preventAutoFocus
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Network Access Point"
      description="Update the details for this Network Access Point">
      <EditNAPForm nap={nap} onSuccess={handleSuccess} onCancel={handleCancel} />
    </ResponsiveDialog>
  );
}

export default memo(EditNAPDialog);
