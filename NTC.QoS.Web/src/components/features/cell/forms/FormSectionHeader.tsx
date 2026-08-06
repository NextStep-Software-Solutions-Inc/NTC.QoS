import React from "react";

import { cn } from "@/lib/utils";

interface FormSectionHeaderProps {
  icon: React.ElementType;
  title: string;
  subtitle: string;
  color: string;
}

/**
 * Shared section header component for cell forms.
 * Provides visual hierarchy with icon, title, and subtitle.
 */
export function FormSectionHeader({ icon: Icon, title, subtitle, color }: FormSectionHeaderProps) {
  return (
    <div className="flex items-center gap-3 mb-4">
      <div className={cn("p-2 rounded-xl shadow-sm", color)}>
        <Icon className="h-5 w-5 text-white" />
      </div>
      <div>
        <h4 className="text-sm font-semibold text-foreground">{title}</h4>
        <p className="text-xs text-muted-foreground">{subtitle}</p>
      </div>
    </div>
  );
}
