import React from "react";
import { Trash2 } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";

interface DeleteCellDialogProps {
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
}

const DeleteCellDialog = ({ open, onOpenChange }: DeleteCellDialogProps) => {
  return (
    <ResponsiveDialog
      title="Delete Cell Site"
      description="Are you sure you want to delete this cell site?"
      open={open}
      onOpenChange={onOpenChange}
      trigger={
        <Button variant="outline" className="text-red-500 hover:text-red-700">
          <Trash2 />
        </Button>
      }>
      <div className="p-4">
        <p className="text-sm text-gray-700">
          This action cannot be undone. Please confirm that you want to delete this cell site.
        </p>
      </div>
      <div className="flex justify-end gap-2 p-4">
        <Button variant="outline" className="text-gray-500 hover:text-gray-700" onClick={() => onOpenChange?.(false)}>
          Cancel
        </Button>
        <Button variant="outline" className="text-white bg-red-500 hover:bg-red-600">
          Delete
        </Button>
      </div>
    </ResponsiveDialog>
  );
};

export default DeleteCellDialog;
