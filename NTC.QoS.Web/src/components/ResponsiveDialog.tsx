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
        className={cn(
          sizeClasses[size],
          "w-[calc(100vw-2rem)] sm:w-full max-h-[calc(100dvh-2rem)] sm:max-h-[90dvh] flex flex-col overflow-hidden rounded-xl sm:rounded-lg p-4 sm:p-6 gap-3 sm:gap-4",
          className
        )}
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
          {description && <DialogDescription className="text-xs sm:text-sm line-clamp-2 sm:line-clamp-none">{description}</DialogDescription>}
        </DialogHeader>
        <div className="flex-1 min-h-0 flex flex-col h-full overflow-hidden">{children}</div>
      </DialogContent>
    </Dialog>
  );
}
