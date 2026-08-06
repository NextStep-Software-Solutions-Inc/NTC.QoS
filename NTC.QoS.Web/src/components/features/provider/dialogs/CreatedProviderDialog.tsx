import React from "react";
import { Plus } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import { type Provider } from "../types/Provider";

interface CreateProviderDialogProps {
  onPrev?: () => void;
  onNext?: () => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  provider: Provider;
  onSuccessDialogClose?: () => void;
}
export default function CreatedProviderDialog({
  onPrev,
  onNext,
  open,
  onOpenChange,
  provider,
  onSuccessDialogClose,
}: CreateProviderDialogProps) {
  const handleSuccessCloseDialog = () => {
    if (!provider) return;
    onNext?.();
    onSuccessDialogClose?.();
  };
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={"Provider created successfully"}
      description="You have successfully created a new provider. You can now add services for this provider.">
      <div className="flex justify-between items-center">
        <section className="flex items-center space-x-2">
          <div className="py-2 px-3 bg-gray-300 rounded-full">{provider.name.charAt(0)}</div>
          <div className="text-center space-y-1">
            <div className="text-white bg-blue-500 text-sm px-3 py-1 rounded-full">{provider.name}</div>
            <div className="text-[13px]">Service Provider</div>
          </div>
        </section>
        <div>
          <section className="text-[15px] font-semibold">{provider.licenseNumber}</section>
        </div>
      </div>
      <div className="flex justify-between mt-4">
        <Button variant="ghost" onClick={onPrev}>
          Close
        </Button>
        <Button onClick={handleSuccessCloseDialog}>
          <Plus /> Add Service
        </Button>
      </div>
    </ResponsiveDialog>
  );
}
