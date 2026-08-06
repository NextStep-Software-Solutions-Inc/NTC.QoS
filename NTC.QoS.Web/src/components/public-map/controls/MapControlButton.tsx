"use client";

import { forwardRef, type ButtonHTMLAttributes } from "react";

import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

interface MapControlButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  tooltip?: string;
  active?: boolean;
  variant?: "default" | "rounded";
}

const MapControlButton = forwardRef<HTMLButtonElement, MapControlButtonProps>(
  ({ className, tooltip, active = false, variant = "default", children, ...props }, ref) => {
    const button = (
      <button
        ref={ref}
        className={cn(
          "flex items-center justify-center transition-all duration-200 ease-in-out",
          "hover:bg-primary/10 hover:scale-110 active:scale-95",
          "focus:outline-none focus:ring-2 focus:ring-primary/50 focus:ring-offset-1",
          "disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100",
          variant === "rounded" ? "w-10 h-10 rounded-full" : "w-9 h-9 rounded-lg",
          active && "bg-primary/20 text-primary",
          className,
        )}
        {...props}>
        {children}
      </button>
    );

    if (tooltip) {
      return (
        <TooltipProvider delayDuration={200}>
          <Tooltip>
            <TooltipTrigger asChild>{button}</TooltipTrigger>
            <TooltipContent side="right" className="text-xs">
              {tooltip}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      );
    }

    return button;
  },
);

MapControlButton.displayName = "MapControlButton";

export { MapControlButton };
