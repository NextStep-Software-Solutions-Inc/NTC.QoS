/**
 * Map Control Button Component
 *
 * A reusable button for map controls with tooltip support.
 * Follows the Single Responsibility principle - handles only button rendering and interaction.
 */

"use client";

import { forwardRef, type ButtonHTMLAttributes } from "react";

import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

interface MapControlButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Tooltip text shown on hover */
  tooltip?: string;
  /** Whether the button is in active state */
  active?: boolean;
  /** Button shape variant */
  variant?: "default" | "rounded" | "square";
  /** Button size */
  size?: "sm" | "md" | "lg";
  /** Tooltip position */
  tooltipSide?: "top" | "right" | "bottom" | "left";
}

const sizeStyles = { sm: "w-8 h-8", md: "w-10 h-10", lg: "w-12 h-12" };

const variantStyles = { default: "rounded-xl", rounded: "rounded-full", square: "rounded-lg" };

const MapControlButton = forwardRef<HTMLButtonElement, MapControlButtonProps>(
  (
    {
      className,
      tooltip,
      active = false,
      variant = "default",
      size = "md",
      tooltipSide = "left",
      children,
      disabled,
      ...props
    },
    ref,
  ) => {
    const button = (
      <button
        ref={ref}
        disabled={disabled}
        className={cn(
          "flex items-center justify-center",
          "bg-white/90 backdrop-blur-md",
          "shadow-lg border border-white/30",
          "transition-all duration-200 ease-in-out",
          "hover:bg-white hover:shadow-xl hover:scale-105",
          "active:scale-95 active:shadow-md",
          "focus:outline-none focus:ring-2 focus:ring-primary/50 focus:ring-offset-1",
          "disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100",
          sizeStyles[size],
          variantStyles[variant],
          active && "bg-primary/10 border-primary/30 text-primary",
          className,
        )}
        {...props}>
        {children}
      </button>
    );

    if (tooltip) {
      return (
        <TooltipProvider delayDuration={100}>
          <Tooltip>
            <TooltipTrigger asChild>{button}</TooltipTrigger>
            <TooltipContent side={tooltipSide} className="text-xs font-medium bg-gray-900 text-white border-0">
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
export type { MapControlButtonProps };
