import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteBandsRequest } from "../handlers/deleteBandsRequest";

interface DeleteBandsDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteBands = createMutationHookFromTemplate(deleteBandsRequest);
export default function DeleteBandsDialog({ ids, open, onOpenChange, onCancel, onSuccess }: DeleteBandsDialogProps) {
  const { mutateAsync, isPending } = useDeleteBands();
  const queryClient = useQueryClient();
  const onDelete = () => {
    for (const id of ids) {
      toast.promise(
        mutateAsync(
          { pathParams: { id } },
          {
            onSuccess: () => {
              void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-bands" });
            },
          },
        ),
        {
          loading: "Deleting bands...",
          success: (res) => {
            onSuccess?.();
            onOpenChange(false);
            return res.message;
          },
          error: "Failed to delete bands.",
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
      title="Are you absolutely sure you want to delete these bands?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
