/**
 * Glass Panel Component
 *
 * A reusable glassmorphic container following the DRY principle.
 * Used throughout the admin map for consistent styling.
 */

"use client";

import { forwardRef, type HTMLAttributes } from "react";

import { cn } from "@/lib/utils";
import type { GlassPanelVariant } from "../types";

interface GlassPanelProps extends HTMLAttributes<HTMLDivElement> {
  /** Visual variant of the panel */
  variant?: GlassPanelVariant;
  /** Whether to add padding */
  padding?: "none" | "sm" | "md" | "lg";
  /** Whether the panel is interactive (hover effects) */
  interactive?: boolean;
  /** Border radius size */
  rounded?: "sm" | "md" | "lg" | "xl" | "2xl" | "full";
}

const variantStyles: Record<GlassPanelVariant, string> = {
  default: "bg-white/85 border border-white/30 text-gray-800",
  dark: "bg-gray-900/85 border border-gray-700/30 text-white",
  primary: "bg-primary/10 border border-primary/20 text-primary-foreground",
  transparent: "bg-white/50 border border-white/20 text-gray-800",
};

const paddingStyles = { none: "", sm: "p-2", md: "p-3", lg: "p-4" };

const roundedStyles = {
  sm: "rounded-md",
  md: "rounded-lg",
  lg: "rounded-xl",
  xl: "rounded-2xl",
  "2xl": "rounded-3xl",
  full: "rounded-full",
};

const GlassPanel = forwardRef<HTMLDivElement, GlassPanelProps>(
  (
    { className, variant = "default", padding = "none", interactive = false, rounded = "xl", children, ...props },
    ref,
  ) => {
    return (
      <div
        ref={ref}
        className={cn(
          "backdrop-blur-xl shadow-xl transition-all duration-300 ease-in-out",
          variantStyles[variant],
          paddingStyles[padding],
          roundedStyles[rounded],
          interactive && "hover:shadow-2xl hover:scale-[1.02] cursor-pointer",
          className,
        )}
        {...props}>
        {children}
      </div>
    );
  },
);

GlassPanel.displayName = "GlassPanel";

export { GlassPanel };
export type { GlassPanelProps };
