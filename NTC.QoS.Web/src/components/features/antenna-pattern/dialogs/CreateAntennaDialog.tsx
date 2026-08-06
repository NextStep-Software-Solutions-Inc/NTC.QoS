import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateAntennaForm from "../form/CreateAntennaForm";
// import { type AntennaPattern } from "../features/antenna-pattern/types/AntennaPattern";
// import CreateAntennaForm from "../forms/CreateAntennaForm";
import { type AntennaPattern } from "../types/AntennaPattern";

interface CreateAntennaDialogProps {
  onCreated?: (antenna: AntennaPattern) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function CreateAntennaDialog({ onCreated, open, onOpenChange }: CreateAntennaDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add New Antenna"
      description="Fill in the required antenna details.">
      <CreateAntennaForm onCreated={onCreated} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
