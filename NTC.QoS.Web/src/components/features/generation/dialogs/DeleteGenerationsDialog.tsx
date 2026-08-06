import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteGenerationRequest } from "../handlers/deleteGenerationRequest";

interface DeleteGenerationsDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteGenerations = createMutationHookFromTemplate(deleteGenerationRequest);
export default function DeleteGenerationDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteGenerationsDialogProps) {
  const { mutateAsync, isPending } = useDeleteGenerations();
  const queryClient = useQueryClient();
  const onDelete = () => {
    for (const id of ids) {
      toast.promise(
        mutateAsync(
          { pathParams: { id } },
          {
            onSuccess: () => {
              void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-generations" });
            },
          },
        ),
        {
          loading: "Deleting generations...",
          success: (res) => {
            onSuccess?.();
            onOpenChange(false);
            return res.message;
          },
          error: "Failed to delete generations.",
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
      title="Are you absolutely sure you want to delete these generations?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
