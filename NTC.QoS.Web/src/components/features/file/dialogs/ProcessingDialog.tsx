import React, { useState } from "react";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";

interface ProcessDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  id: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export default function ProcessDialog({ id, open, onOpenChange, onCancel, onSuccess }: ProcessDialogProps) {
  const [isPending, setIsPending] = useState(false);

  const onProcess = () => {
    setIsPending(true);

    toast.promise(
      new Promise((resolve, reject) => {
        setTimeout(() => {
          const isSuccess = Math.random() > 0.2; // 80% success rate
          setIsPending(false);
          if (isSuccess) {
            resolve("File processed successfully");
            onSuccess?.();
            onOpenChange(false);
          } else {
            reject(new Error("Processing failed"));
          }
        }, 2000);
      }),
      { loading: "Processing file...", success: "File processed successfully", error: "Failed to process file" },
    );
  };

  const handleCancelProcess = () => {
    setIsPending(false);
    onCancel?.();
  };

  return (
    <ConfirmAlertDialog
      id={id}
      loading={isPending}
      onConfirm={() => onProcess()}
      onCancel={handleCancelProcess}
      open={open}
      onOpenChange={onOpenChange}
      title="Start processing this file?"
      description="This will start processing the file. This may take some time depending on the file size."
    />
  );
}
