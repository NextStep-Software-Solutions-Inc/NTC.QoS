/**
 * Layer Toggle Component
 *
 * Individual layer toggle with optional opacity slider.
 * Follows Single Responsibility - handles one layer's visibility and opacity.
 */

"use client";

import { memo, useCallback, type ReactNode } from "react";
import { motion } from "framer-motion";

import { Slider } from "@/components/ui/slider";
import { cn } from "@/lib/utils";

interface LayerToggleProps {
  /** Unique layer identifier */
  id: string;
  /** Display label */
  label: string;
  /** Layer description */
  description?: string;
  /** Icon to display */
  icon: ReactNode;
  /** Icon color class when active */
  activeColor?: string;
  /** Whether the layer is visible */
  isVisible: boolean;
  /** Current opacity value (0-100) */
  opacity?: number;
  /** Whether to show opacity slider */
  showOpacitySlider?: boolean;
  /** Callback when visibility changes */
  onVisibilityChange: (visible: boolean) => void;
  /** Callback when opacity changes */
  onOpacityChange?: (opacity: number) => void;
  /** Whether the toggle is disabled */
  disabled?: boolean;
  /** Additional class names */
  className?: string;
}

export const LayerToggle = memo(function LayerToggle({
  id,
  label,
  description,
  icon,
  activeColor = "text-primary",
  isVisible,
  opacity = 100,
  showOpacitySlider = true,
  onVisibilityChange,
  onOpacityChange,
  disabled = false,
  className,
}: LayerToggleProps) {
  const handleToggle = useCallback(() => {
    if (!disabled) {
      onVisibilityChange(!isVisible);
    }
  }, [disabled, isVisible, onVisibilityChange]);

  const handleOpacityChange = useCallback(
    (value: number[]) => {
      onOpacityChange?.(value[0] ?? 100);
    },
    [onOpacityChange],
  );

  return (
    <div className={cn("space-y-2", className)}>
      {/* Toggle button */}
      <button
        onClick={handleToggle}
        disabled={disabled}
        className={cn(
          "w-full flex items-center gap-3 px-3 py-2.5 rounded-xl",
          "transition-all duration-200",
          "hover:bg-gray-100/60 focus:outline-none focus:ring-2 focus:ring-primary/30",
          "disabled:opacity-50 disabled:cursor-not-allowed",
          isVisible && "bg-primary/5",
        )}>
        {/* Icon container */}
        <div
          className={cn(
            "w-9 h-9 rounded-lg flex items-center justify-center shrink-0",
            "transition-colors duration-200",
            isVisible ? "bg-primary/15" : "bg-gray-100",
          )}>
          <span className={cn("w-5 h-5", isVisible ? activeColor : "text-gray-400")}>{icon}</span>
        </div>

        {/* Text content */}
        <div className="flex-1 text-left min-w-0">
          <span className={cn("text-sm font-medium block truncate", isVisible ? "text-gray-900" : "text-gray-600")}>
            {label}
          </span>
          {description && <span className="text-xs text-gray-400 block truncate">{description}</span>}
        </div>

        {/* Checkbox indicator */}
        <motion.div
          className={cn(
            "w-5 h-5 rounded-md border-2 flex items-center justify-center shrink-0",
            "transition-colors duration-200",
            isVisible ? "border-primary bg-primary" : "border-gray-300 bg-white",
          )}
          animate={isVisible ? { scale: [1, 1.15, 1] } : { scale: 1 }}
          transition={{ duration: 0.2 }}>
          {isVisible && (
            <motion.svg
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: 1 }}
              className="w-3 h-3 text-white"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="3"
              strokeLinecap="round"
              strokeLinejoin="round">
              <motion.path d="M20 6L9 17l-5-5" />
            </motion.svg>
          )}
        </motion.div>
      </button>

      {/* Opacity slider */}
      {isVisible && showOpacitySlider && onOpacityChange && (
        <motion.div
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: "auto" }}
          exit={{ opacity: 0, height: 0 }}
          transition={{ duration: 0.2 }}
          className="px-3 pb-1">
          <div className="flex items-center gap-3">
            <span className="text-xs text-gray-400 w-14 shrink-0">Opacity</span>
            <Slider
              value={[opacity]}
              min={10}
              max={100}
              step={5}
              onValueChange={handleOpacityChange}
              className="flex-1"
            />
            <span className="text-xs text-gray-500 w-10 text-right font-medium">{opacity}%</span>
          </div>
        </motion.div>
      )}
    </div>
  );
});
