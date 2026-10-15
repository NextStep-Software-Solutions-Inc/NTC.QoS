import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteFileRequest } from "../handlers/deleteFileRequest";

const useDeleteFile = createMutationHookFromTemplate(deleteFileRequest);

interface DeleteFileDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  id: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export default function DeleteFileDialog({ id, open, onOpenChange, onCancel, onSuccess }: DeleteFileDialogProps) {
  const { mutateAsync, isPending } = useDeleteFile();

  const queryClient = useQueryClient();
  const onDelete = () => {
    toast.promise(mutateAsync({ pathParams: { id } }), {
      loading: "Deleting file",
      success: (res) => {
        onSuccess?.();
        onOpenChange(false);
        return res.message;
      },
      error: "Failed to delete file",
    });
  };

  const handleCancelDelete = () => {
    void queryClient.cancelQueries({ queryKey: ["delete-file"] });
    onCancel?.();
  };

  return (
    <ConfirmAlertDialog
      id={id}
      loading={isPending}
      onConfirm={onDelete}
      onCancel={handleCancelDelete}
      open={open}
      onOpenChange={onOpenChange}
      title="Are you absolutely sure you want to delete this file?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
