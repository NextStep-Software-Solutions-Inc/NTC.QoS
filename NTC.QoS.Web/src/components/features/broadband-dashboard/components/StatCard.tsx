import { LucideIcon } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";

interface StatCardProps {
  title: string;
  value: string | number;
  subtext?: string;
  icon?: LucideIcon;
  breakdown?: { label: string; value: number }[];
  color?: "default" | "blue" | "green" | "orange" | "purple";
  action?: { label: string; onClick: () => void };
}

export function StatCard({ title, value, subtext, icon: Icon, breakdown, color = "default", action }: StatCardProps) {
  const colorStyles = {
    default: {
      border: "border-gray-200",
      iconBg: "bg-gray-100",
      iconColor: "text-gray-600",
      valueColor: "text-gray-900",
      titleColor: "text-gray-600",
      gradient: "from-gray-50 to-white",
    },
    blue: {
      border: "border-blue-200",
      iconBg: "bg-blue-100",
      iconColor: "text-blue-600",
      valueColor: "text-blue-900",
      titleColor: "text-blue-700",
      gradient: "from-blue-50 to-white",
    },
    green: {
      border: "border-green-200",
      iconBg: "bg-green-100",
      iconColor: "text-green-600",
      valueColor: "text-green-900",
      titleColor: "text-green-700",
      gradient: "from-green-50 to-white",
    },
    orange: {
      border: "border-orange-200",
      iconBg: "bg-orange-100",
      iconColor: "text-orange-600",
      valueColor: "text-orange-900",
      titleColor: "text-orange-700",
      gradient: "from-orange-50 to-white",
    },
    purple: {
      border: "border-purple-200",
      iconBg: "bg-purple-100",
      iconColor: "text-purple-600",
      valueColor: "text-purple-900",
      titleColor: "text-purple-700",
      gradient: "from-purple-50 to-white",
    },
  };

  const styles = colorStyles[color];

  const content = (
    <Card
      className={`h-full transition-all duration-200 hover:shadow-lg hover:-translate-y-1 border ${styles.border} overflow-hidden relative group`}>
      <div className={`absolute inset-0 bg-gradient-to-br ${styles.gradient} opacity-50`} />
      <div className="relative z-10">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className={`text-sm font-medium ${styles.titleColor}`}>{title}</CardTitle>
          <div className="flex items-center gap-2">
            {action && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  action.onClick();
                }}
                className={`text-xs font-medium ${styles.iconColor} hover:underline focus:outline-none opacity-0 group-hover:opacity-100 transition-opacity`}>
                {action.label}
              </button>
            )}
            {Icon && (
              <div className={`p-2.5 rounded-xl ${styles.iconBg} shadow-sm`}>
                <Icon className={`h-5 w-5 ${styles.iconColor}`} />
              </div>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <div className={`text-3xl font-bold tracking-tight ${styles.valueColor}`}>{value}</div>
          {subtext && <p className="text-xs text-muted-foreground mt-1 font-medium">{subtext}</p>}
        </CardContent>
      </div>
    </Card>
  );

  if (breakdown && breakdown.length > 0) {
    return (
      <TooltipProvider>
        <Tooltip delayDuration={0}>
          <TooltipTrigger asChild>
            <div className="h-full cursor-help">{content}</div>
          </TooltipTrigger>
          <TooltipContent className="bg-white text-foreground border shadow-lg p-4">
            <div className="space-y-2">
              <h4 className="font-semibold mb-2">{title} Breakdown:</h4>
              {breakdown.map((item, index) => (
                <div key={index} className="flex items-center gap-2 text-sm">
                  <div className="h-2 w-2 rounded-full bg-blue-500" />
                  <span className="font-medium text-blue-600">{item.label}:</span>
                  <span>{item.value}</span>
                </div>
              ))}
            </div>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  return content;
}
