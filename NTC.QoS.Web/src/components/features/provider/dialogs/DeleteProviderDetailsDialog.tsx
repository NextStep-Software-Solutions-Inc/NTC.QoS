import { useQueryClient } from "@tanstack/react-query";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";

interface DeleteProviderDetailsDialogProps {
  open?: boolean;
  onOpenChange?: (state: boolean) => void;
  ids: string[];
  onCancel?: () => void;
  onDelete?: (id: string) => void;
  isPending?: boolean;
  providerId: string;
}

export default function DeleteProviderDetailsDialog({
  providerId,
  open,
  onOpenChange,
  onCancel,
  onDelete,
  isPending,
}: DeleteProviderDetailsDialogProps) {
  const queryClient = useQueryClient();

  const handleCancelDelete = () => {
    void queryClient.cancelQueries({});
    onCancel?.();
  };

  return (
    <ConfirmAlertDialog
      id={providerId}
      loading={isPending}
      onConfirm={() => onDelete?.(providerId)}
      onCancel={handleCancelDelete}
      open={open ?? false}
      onOpenChange={() => onOpenChange?.(!open)}
      title="Are you absolutely sure you want to delete this provider?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
