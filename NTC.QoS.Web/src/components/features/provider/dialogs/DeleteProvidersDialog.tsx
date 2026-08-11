import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteProvidersRequest } from "../handlers/deleteProviderRequest";

interface DeleteProvidersDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

export default function DeleteProvidersDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteProvidersDialogProps) {
  const useDeleteProviders = createMutationHookFromTemplate(deleteProvidersRequest);
  const { mutateAsync, isPending } = useDeleteProviders();

  const queryClient = useQueryClient();
  const onDelete = () => {
    const mutations = ids.map((id) => mutateAsync({ pathParams: { id } }));
    toast.promise(Promise.allSettled(mutations), {
      loading: "Deleting providers...",
      success: (results) => {
        const succeeded = results.filter((r) => r.status === "fulfilled");
        const failed = results.filter((r) => r.status === "rejected");
        if (succeeded.length > 0) {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-providers" });
          onSuccess?.();
          onOpenChange(false);
        }
        if (failed.length > 0) {
          return `${succeeded.length} provider(s) deleted. ${failed.length} failed.`;
        }
        return `${succeeded.length} provider(s) deleted successfully.`;
      },
      error: "Failed to delete providers.",
    });
  };

  const handleCancelDelete = () => {
    void queryClient.cancelQueries({ predicate: (query) => query.queryKey[0] === "get-providers" });
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
      title="Are you absolutely sure you want to delete these provider?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
