import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditLinkForm from "../forms/EditLinkForm";
import { type Link } from "../types/Link";

interface EditLinkDialogProps {
  link?: Link;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
export default function EditLinkDialog({ link, open, onOpenChange }: EditLinkDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Link"
      description=""
      // description="Create a new link by filling out the form below."
    >
      <EditLinkForm link={link} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
