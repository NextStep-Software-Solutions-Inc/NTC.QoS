import React from "react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { type Provider } from "../../provider/types/Provider";
import AddServiceProviderForm from "../../service/form/AddServiceProviderForm";

interface AddServiceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: () => void;
  onClose?: () => void;
  provider: Provider;
  onSuccessDialogClose?: () => void;
}

export default function AddServiceProviderDialog({
  onClose,
  onCreated,
  onOpenChange,
  open,
  provider,
  onSuccessDialogClose,
}: AddServiceDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add Service"
      description="Create a new service by filling out the form below."
      className="sm:max-w-[600px]">
      <AddServiceProviderForm
        onClose={onClose}
        onCreated={onCreated}
        provider={provider}
        onSuccessDialogClose={onSuccessDialogClose}
      />
    </ResponsiveDialog>
  );
}
