import React, { useState } from "react";
import { Plus } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import AddNAPForm from "../forms/AddNAPForm";

interface AddNAPSDialogProps {
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  defaultLocation?: { lat: number; lng: number };
}

const AddNAPSDialog = ({ open, onOpenChange, defaultLocation }: AddNAPSDialogProps) => {
  const [internalOpen, setInternalOpen] = useState<boolean>(false);

  // Use controlled or uncontrolled mode
  const isOpen = open !== undefined ? open : internalOpen;
  const handleOpenChange = onOpenChange || setInternalOpen;

  return (
    <ResponsiveDialog
      open={isOpen}
      onOpenChange={handleOpenChange}
      title="Add Network Access Point"
      description={
        defaultLocation
          ? `Location: ${defaultLocation.lat.toFixed(6)}, ${defaultLocation.lng.toFixed(6)}`
          : "Add a new Network Access Point"
      }
      trigger={
        open === undefined ? (
          <Button className="flex items-center h-9">
            <Plus className="h-4 w-4 mr-1" />
            Add NAP
          </Button>
        ) : undefined
      }>
      <AddNAPForm
        defaultLocation={defaultLocation}
        onSuccess={() => {
          handleOpenChange(false);
        }}
        onCancel={() => {
          handleOpenChange(false);
        }}
      />
    </ResponsiveDialog>
  );
};

export default AddNAPSDialog;
