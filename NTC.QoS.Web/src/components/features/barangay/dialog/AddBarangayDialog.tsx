import React from "react";
import { Plus } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import { type Municipality } from "../../municipality/types/Municipality";
import AddBarangayForm from "../form/AddBarangayForm";

interface AddBarangayDialogProps {
  municipality?: Municipality;
  municipalityId?: string;
}

const AddBarangayDialog = ({ municipalityId, municipality }: AddBarangayDialogProps) => {
  return (
    <ResponsiveDialog
      title="Add Barangay"
      description="Add a new barangay with their respective municipality"
      trigger={
        <Button variant="ghost" size="sm">
          <Plus />
        </Button>
      }>
      <AddBarangayForm municipality={municipality} municipalityId={municipalityId} />
    </ResponsiveDialog>
  );
};

export default AddBarangayDialog;
