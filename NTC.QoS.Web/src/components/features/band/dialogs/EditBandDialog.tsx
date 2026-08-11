import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import EditBandForm from "../forms/EditBandForm";
import { type Band } from "../types/Band";

interface EditBandDialogProps {
  band?: Band;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function EditBandDialog({ band, open, onOpenChange }: EditBandDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit Band"
      description="Edit the band by filling out the form below.">
      <EditBandForm band={band} onClose={() => onOpenChange(false)} />
    </ResponsiveDialog>
  );
}
