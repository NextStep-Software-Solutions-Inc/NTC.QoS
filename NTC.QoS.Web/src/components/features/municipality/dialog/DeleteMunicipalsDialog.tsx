"use client";

import React from "react";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { Button } from "@/components/ui/button";

interface DeleteMunicipalDialogProps {
  count: number;
  isPending?: boolean;
  onDelete: (id: string) => void;
  onCancel?: () => void;
}

const DeleteMunicipalsDialog = ({ count, onDelete, onCancel }: DeleteMunicipalDialogProps) => {
  const [deleteMunicipalitiesOpen, setDeleteMunicipalitiesOpen] = React.useState(false);
  return (
    <>
      <Button variant="destructive" size="sm">
        Delete ({count})
      </Button>
      <ConfirmAlertDialog
        title="Delete Municipalities"
        description={`Are you absolutely sure you want to delete ${count > 1 ? `these ${count} municipalities?` : `this ${count} municipality?`}`}
        open={deleteMunicipalitiesOpen}
        onOpenChange={setDeleteMunicipalitiesOpen}
        onCancel={onCancel}
        onConfirm={onDelete}
        id=""
        loading={false}
      />
    </>
  );
};
export default DeleteMunicipalsDialog;
