"use client";

import React, { useState } from "react";
import { Activity, Plus } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import CreateSpeedtestForm from "../forms/CreateSpeedtestForm";

interface CreateSpeedtestDialogProps {
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  defaultLocation?: { lat: number; lng: number };
  onSuccess?: () => void;
}

const CreateSpeedtestDialog = ({ open, onOpenChange, defaultLocation, onSuccess }: CreateSpeedtestDialogProps) => {
  const [internalOpen, setInternalOpen] = useState<boolean>(false);

  // Use controlled or uncontrolled mode
  const isOpen = open !== undefined ? open : internalOpen;
  const handleOpenChange = onOpenChange || setInternalOpen;

  const handleSuccess = () => {
    handleOpenChange(false);
    onSuccess?.();
  };

  return (
    <ResponsiveDialog
      open={isOpen}
      onOpenChange={handleOpenChange}
      title="Add Speedtest"
      description={
        defaultLocation
          ? `Location: ${defaultLocation.lat.toFixed(6)}, ${defaultLocation.lng.toFixed(6)}`
          : "Record a new network performance test"
      }
      trigger={
        open === undefined ? (
          <Button className="flex items-center h-9 gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-600 hover:to-blue-700 text-white shadow-lg shadow-cyan-500/25">
            <Plus className="h-4 w-4" />
            Add Speedtest
          </Button>
        ) : undefined
      }
      className="sm:max-w-[600px]">
      <CreateSpeedtestForm
        defaultLocation={defaultLocation}
        onSuccess={handleSuccess}
        onCancel={() => handleOpenChange(false)}
      />
    </ResponsiveDialog>
  );
};

export default CreateSpeedtestDialog;
