import UpdateFileForm from "@/components/features/file/forms/UpdateFileForm";
import { type File } from "@/components/features/file/types/File";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";

interface UpdateFileDialogProps {
  onUpdate?: () => void;
  open: boolean;
  onOpenChange: (state: boolean) => void;
  file: File;
}
export function UpdateFileDialog({ onUpdate, onOpenChange, open, file }: UpdateFileDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Update File"
      description="Update the file by filling out the form below.">
      <UpdateFileForm onClose={() => onOpenChange(false)} onSuccess={onUpdate} file={file} />
    </ResponsiveDialog>
  );
}
