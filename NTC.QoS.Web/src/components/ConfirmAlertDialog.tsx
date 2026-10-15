import { Loader2 } from "lucide-react";

import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { cn } from "@/lib/utils";
// import SubmitButton from "../SubmitButton"
import SubmitButton from "./SubmitButton";

interface ConfirmAlertDialogProps {
  id: string;
  onConfirm: (id: string) => void;
  onCancel?: () => void;
  open: boolean;
  onOpenChange: (state: boolean) => void;
  title: string;
  description: string;
  loading?: boolean;
}
export default function ConfirmAlertDialog({
  onConfirm,
  onCancel,
  onOpenChange,
  open,
  id,
  title,
  description,
  loading = false,
}: ConfirmAlertDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={onCancel}>Cancel</AlertDialogCancel>
          <SubmitButton variant="destructive" disabled={loading} onClick={() => onConfirm(id)}>
            <Loader2 className={cn("hidden", { "block animate-spin transition-all": loading })} />
            Confirm
          </SubmitButton>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
