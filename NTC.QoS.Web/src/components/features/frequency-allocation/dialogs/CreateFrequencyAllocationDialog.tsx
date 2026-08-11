import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import CreateFrequencyAllocationForm from "../forms/CreateFrequencyAllocationForm";

interface CreateFrequencyAllocationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function CreateFrequencyAllocationDialog({ open, onOpenChange }: CreateFrequencyAllocationDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add Frequency Allocation"
      description="Create a new frequency allocation with its paired uplink and downlink."
      size="wide"
      className="h-auto">
      <CreateFrequencyAllocationForm onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
