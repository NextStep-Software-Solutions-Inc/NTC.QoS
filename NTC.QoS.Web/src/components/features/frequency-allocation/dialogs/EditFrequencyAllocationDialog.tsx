import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditFrequencyAllocationForm from "../forms/EditFrequencyAllocationForm";
import { type FrequencyAllocation } from "../types/FrequencyAllocation";

interface EditFrequencyAllocationDialogProps {
  allocation: FrequencyAllocation;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function EditFrequencyAllocationDialog({
  allocation,
  open,
  onOpenChange,
}: EditFrequencyAllocationDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Frequency Allocation"
      description="Update the frequency allocation details.">
      <EditFrequencyAllocationForm allocation={allocation} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
