import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";

interface ResponsiveDialogProps {
  open?: boolean;
  onOpenChange?: (state: boolean) => void;
  title: React.ReactNode;
  description: React.ReactNode;
  trigger?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
  size?: "default" | "wide" | "narrow";
  preventOutsideClick?: boolean;
  /** Prevents Radix from auto-focusing the first focusable element on open.
   * Use when the dialog contains coordinate/number inputs to avoid browser
   * select-all behaviour triggered by programmatic focus. */
  preventAutoFocus?: boolean;
}
export function ResponsiveDialog({
  children,
  open,
  onOpenChange,
  title,
  description,
  trigger,
  className,
  size = "default",
  preventOutsideClick = false,
  preventAutoFocus = false,
}: ResponsiveDialogProps) {
  const sizeClasses = { narrow: "sm:max-w-[400px]", default: "sm:max-w-[520px]", wide: "sm:max-w-[640px]" };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      {trigger && <DialogTrigger asChild>{trigger}</DialogTrigger>}
      <DialogContent
        className={cn(sizeClasses[size], "max-h-[90vh] h-[90vh] flex flex-col", className)}
        onInteractOutside={(e) => {
          if (preventOutsideClick) {
            e.preventDefault();
          }
        }}
        onOpenAutoFocus={(e) => {
          if (preventAutoFocus) {
            e.preventDefault();
          }
        }}
      >
        <DialogHeader className="flex-shrink-0">
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>
        <div className="flex-1 min-h-0 flex flex-col h-full">{children}</div>
      </DialogContent>
    </Dialog>
  );
}
