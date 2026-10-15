"use client";

import { forwardRef, type HTMLAttributes } from "react";

import { cn } from "@/lib/utils";

interface GlassPanelProps extends HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "dark";
}

const GlassPanel = forwardRef<HTMLDivElement, GlassPanelProps>(
  ({ className, variant = "default", children, ...props }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          "rounded-2xl backdrop-blur-md shadow-xl transition-all duration-300 ease-in-out",
          variant === "default"
            ? "bg-white/80 border border-white/20"
            : "bg-gray-900/80 border border-gray-700/30 text-white",
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
