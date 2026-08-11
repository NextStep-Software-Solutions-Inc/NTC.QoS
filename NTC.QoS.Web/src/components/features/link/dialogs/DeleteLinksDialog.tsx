import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteLinksRequest } from "../handlers/deleteLinksRequest";

interface DeleteLinksDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteLinks = createMutationHookFromTemplate(deleteLinksRequest);

export default function DeleteLinksDialog({ ids, open, onOpenChange, onCancel, onSuccess }: DeleteLinksDialogProps) {
  const { mutateAsync, isPending } = useDeleteLinks();
  const queryClient = useQueryClient();

  const onDelete = async () => {
    try {
      // Delete links sequentially
      for (const id of ids) {
        await mutateAsync({ pathParams: { id } });
      }

      await queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-links" });

      toast.success(`Successfully deleted ${ids.length} link(s)`);
      onSuccess?.();
      onOpenChange(false);
    } catch (error) {
      console.error("Delete failed:", error);
      toast.error("Failed to delete links");
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
      title={`Are you absolutely sure you want to delete ${ids.length} link(s)?`}
      description="This action cannot be undone. This will permanently delete the record(s) from the server."
    />
  );
}
