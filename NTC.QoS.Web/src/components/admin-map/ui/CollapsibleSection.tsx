/**
 * Collapsible Section Component
 *
 * A reusable collapsible section for panels and menus.
 * Used in layer controls and filter panels.
 */

"use client";

import { useState, type ReactNode } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronDown } from "lucide-react";

import { cn } from "@/lib/utils";

interface CollapsibleSectionProps {
  /** Section title */
  title: string;
  /** Optional subtitle or description */
  subtitle?: string;
  /** Icon to display before the title */
  icon?: ReactNode;
  /** Whether the section is expanded by default */
  defaultExpanded?: boolean;
  /** Section content */
  children: ReactNode;
  /** Additional class names */
  className?: string;
  /** Header class names */
  headerClassName?: string;
  /** Content class names */
  contentClassName?: string;
  /** Badge or count to show */
  badge?: ReactNode;
  /** Whether the section is disabled */
  disabled?: boolean;
}

export function CollapsibleSection({
  title,
  subtitle,
  icon,
  defaultExpanded = false,
  children,
  className,
  headerClassName,
  contentClassName,
  badge,
  disabled = false,
}: CollapsibleSectionProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  // Render header as a non-button element to avoid nesting interactive
  // controls (e.g. rendering a `Button` inside the header's badge). Use
  // role/button and keyboard handlers for accessibility.
  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (disabled) return;
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      setIsExpanded(!isExpanded);
    }
  };

  return (
    <div className={cn("border-b border-gray-100/50 last:border-b-0", className)}>
      <div
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-expanded={isExpanded}
        aria-disabled={disabled}
        onClick={() => !disabled && setIsExpanded(!isExpanded)}
        onKeyDown={handleKeyDown}
        className={cn(
          "w-full flex items-center gap-3 px-4 py-3",
          "text-left transition-colors duration-200",
          "hover:bg-gray-50/50",
          "disabled:opacity-50 disabled:cursor-not-allowed",
          headerClassName,
        )}>
        {icon && <span className="w-5 h-5 flex items-center justify-center text-gray-500">{icon}</span>}
        <div className="flex-1 min-w-0">
          <span className="text-sm font-medium text-gray-800 block truncate">{title}</span>
          {subtitle && <span className="text-xs text-gray-500 block truncate">{subtitle}</span>}
        </div>
        {badge && <span className="shrink-0">{badge}</span>}
        <motion.div animate={{ rotate: isExpanded ? 180 : 0 }} transition={{ duration: 0.2 }} className="shrink-0">
          <ChevronDown className="w-4 h-4 text-gray-400" />
        </motion.div>
      </div>

      <AnimatePresence initial={false}>
        {isExpanded && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2, ease: "easeInOut" }}
            className="overflow-hidden">
            <div className={cn("px-4 pb-4", contentClassName)}>{children}</div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
