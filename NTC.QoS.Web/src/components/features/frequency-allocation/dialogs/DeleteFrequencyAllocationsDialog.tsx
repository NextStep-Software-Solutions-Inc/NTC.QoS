import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteFrequencyAllocationRequest } from "../handlers/deleteFrequencyAllocationRequest";

const useDeleteFrequencyAllocation = createMutationHookFromTemplate(deleteFrequencyAllocationRequest);

interface DeleteFrequencyAllocationsDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

export default function DeleteFrequencyAllocationsDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteFrequencyAllocationsDialogProps) {
  const { mutateAsync, isPending } = useDeleteFrequencyAllocation();

  const onDelete = async () => {
    try {
      const results = await Promise.allSettled(
        ids.map((id) => mutateAsync({ pathParams: { id } }))
      );

      const successCount = results.filter(
        (result) => result.status === "fulfilled"
      ).length;
      const failureCount = ids.length - successCount;

      if (successCount === ids.length) {
        toast.success(`Successfully deleted ${ids.length} frequency allocation(s).`);
        onSuccess?.();
        onOpenChange(false);
      } else if (successCount > 0) {
        toast.success(
          `Successfully deleted ${successCount} of ${ids.length} frequency allocation(s).`
        );
        toast.error(
          `Failed to delete ${failureCount} frequency allocation(s).`
        );
      } else {
        toast.error("Failed to delete any frequency allocation(s).");
      }
    } catch (err) {
      console.error(err);
      toast.error("Failed to delete frequency allocation(s).");
    }
  };

  const handleCancel = () => {
    onCancel?.();
    onOpenChange(false);
  };

  return (
    <ConfirmAlertDialog
      id=""
      loading={isPending}
      onConfirm={onDelete}
      onCancel={handleCancel}
      open={open}
      onOpenChange={onOpenChange}
      title={`Delete ${ids.length} Frequency Allocation${ids.length > 1 ? "s" : ""}?`}
      description="This action cannot be undone. The selected frequency allocation(s) and their paired links will be permanently removed."
    />
  );
}
