import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteAreaStudyRequest } from "../handlers/deleteAreaStudyRequest";

interface DeleteAreaStudiesDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteAreaStudies = createMutationHookFromTemplate(deleteAreaStudyRequest);

export default function DeleteAreaStudiesDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteAreaStudiesDialogProps) {
  const { mutateAsync, isPending } = useDeleteAreaStudies();
  const queryClient = useQueryClient();

  const onDelete = async () => {
    try {
      // Delete area studies sequentially
      for (const id of ids) {
        await mutateAsync({ pathParams: { id } });
      }

      await queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-area-studies" });

      toast.success(`Successfully deleted ${ids.length} area study(ies)`);
      onSuccess?.();
      onOpenChange(false);
    } catch (error) {
      console.error("Delete failed:", error);
      toast.error("Failed to delete area studies");
    }
  };

  const handleCancelDelete = () => {
    onCancel?.();
    onOpenChange(false);
  };

  return (
    <ConfirmAlertDialog
      id=""
      loading={isPending}
      onConfirm={onDelete}
      onCancel={handleCancelDelete}
      open={open}
      onOpenChange={onOpenChange}
      title={`Are you absolutely sure you want to delete ${ids.length} area study(ies)?`}
      description="This action cannot be undone. This will permanently delete the record(s) from the server."
    />
  );
}
