import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteProvinceRequest } from "../handlers/deleteProvinceRequest";

interface DeleteProvinceDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

export default function DeleteProvinceDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteProvinceDialogProps) {
  const useDeleteProvice = createMutationHookFromTemplate(deleteProvinceRequest);
  const { mutateAsync, isPending } = useDeleteProvice();

  const queryClient = useQueryClient();
  const onDelete = () => {
    for (const id of ids) {
      toast.promise(
        mutateAsync(
          { pathParams: { id } },
          {
            onSuccess: (res) => {
              void queryClient.invalidateQueries({ queryKey: ["/provinces"] });
              return res.message;
            },
          },
        ),
        {
          loading: "Deleting provinces...",
          success: (res) => {
            onSuccess?.();
            onOpenChange(false);
            return res.message;
          },
          error: "Failed to delete provinces.",
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
      title="Are you absolutely sure you want to delete these provinces?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
