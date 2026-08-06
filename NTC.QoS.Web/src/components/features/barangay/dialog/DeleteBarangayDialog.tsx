"use client";

import React from "react";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { Button } from "@/components/ui/button";

interface DeleteBarangayDialogProps {
  count: number;
  isPending?: boolean;
  onDelete: (id: string) => void;
  onCancel?: () => void;
}
const DeleteBarangayDialog = ({ count, isPending, onDelete, onCancel }: DeleteBarangayDialogProps) => {
  const [deleteBarangayOpen, setDeleteBarangayOpen] = React.useState(false);

  return (
    <>
      <Button variant="destructive" size="sm" onClick={() => setDeleteBarangayOpen(true)}>
        Delete ({count})
      </Button>
      <ConfirmAlertDialog
        title={`Delete Barangay ${count > 0 ? ` (${count})` : ""}`}
        description="Are you sure you want to delete this barangay?"
        open={deleteBarangayOpen}
        onOpenChange={setDeleteBarangayOpen}
        onCancel={onCancel}
        onConfirm={onDelete}
        id=""
        loading={isPending}
      />
    </>
  );
};

export default DeleteBarangayDialog;
