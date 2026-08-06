import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteTechnologyRequest } from "../handlers/deleteTechnologyRequest";

interface DeleteTechnologiesDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteTechnologies = createMutationHookFromTemplate(deleteTechnologyRequest);
export default function DeleteTechnologiesDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteTechnologiesDialogProps) {
  const { mutateAsync, isPending } = useDeleteTechnologies();
  const queryClient = useQueryClient();
  const onDelete = async () => {
    try {
      for (const id of ids) {
        await mutateAsync({ pathParams: { id } });
      }

      // Invalidate queries after all deletions
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-technologies" });

      toast.success("Technologies deleted successfully");
      onSuccess?.();
      onOpenChange(false);
    } catch (error) {
      toast.error("Failed to delete technologies");
      console.error("Error deleting technologies:", error);
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
      title="Are you absolutely sure you want to delete these technologies?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
