import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteSystemTypesRequest } from "../handlers/deleteSystemTypesRequest";

interface DeleteSystemTypesDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteSystemTypes = createMutationHookFromTemplate(deleteSystemTypesRequest);
export default function DeleteSystemTypesDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteSystemTypesDialogProps) {
  const { mutateAsync, isPending } = useDeleteSystemTypes();
  const queryClient = useQueryClient();
  const onDelete = () => {
    for (const id of ids) {
      toast.promise(
        mutateAsync(
          { pathParams: { id } },
          {
            onSuccess: () => {
              void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-system-types" });
            },
          },
        ),
        {
          loading: "Deleting system types...",
          success: (res) => {
            onSuccess?.();
            onOpenChange(false);
            return res.message;
          },
          error: "Failed to delete system types.",
        },
      );
    }
  };

  const handleCancelDelete = () => {
    void queryClient.cancelQueries({});
    onCancel?.();
  };

  return (
    <ConfirmAlertDialog
      id=""
      loading={isPending}
      onConfirm={onDelete}
      onCancel={handleCancelDelete}
      open={open}
      onOpenChange={onOpenChange}
      title="Are you absolutely sure you want to delete these system types?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
