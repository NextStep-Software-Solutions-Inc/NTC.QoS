"use client";

import * as React from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { DayPicker } from "react-day-picker";
import "react-day-picker/style.css";

import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type CalendarProps = React.ComponentProps<typeof DayPicker>;

function Calendar({ className, classNames, showOutsideDays = true, ...props }: CalendarProps) {
  return (
    <DayPicker
      showOutsideDays={showOutsideDays}
      className={cn("p-2", className)}
      classNames={{
        months: "relative flex flex-col sm:flex-row gap-4",
        month: "space-y-3",
        month_caption: "flex justify-center items-center h-8 relative font-medium text-sm text-gray-800 dark:text-gray-200",
        caption_label: "text-sm font-semibold",
        nav: "flex items-center gap-1 absolute top-0 inset-x-0 justify-between pointer-events-auto z-10",
        button_previous: cn(
          buttonVariants({ variant: "outline" }),
          "h-7 w-7 bg-white dark:bg-gray-800 p-0 opacity-70 hover:opacity-100 shadow-sm",
        ),
        button_next: cn(
          buttonVariants({ variant: "outline" }),
          "h-7 w-7 bg-white dark:bg-gray-800 p-0 opacity-70 hover:opacity-100 shadow-sm",
        ),
        month_grid: "w-full border-collapse",
        weekdays: "border-b border-gray-100 dark:border-gray-800",
        weekday: "text-muted-foreground w-9 h-8 text-[0.8rem] font-medium text-center align-middle",
        weeks: "",
        week: "",
        day: "h-9 w-9 p-0 text-center text-sm relative align-middle focus-within:relative focus-within:z-20",
        day_button: cn(
          buttonVariants({ variant: "ghost" }),
          "h-8 w-8 p-0 font-normal rounded-md transition-colors hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-blue-950/40 dark:hover:text-blue-300",
        ),
        range_start: "day-range-start !bg-blue-600 !text-white rounded-l-md font-semibold",
        range_end: "day-range-end !bg-blue-600 !text-white rounded-r-md font-semibold",
        range_middle: "!bg-blue-100 dark:!bg-blue-950/50 !text-blue-900 dark:!text-blue-100 rounded-none",
        selected: "!bg-blue-600 !text-white font-semibold",
        today: "font-bold text-blue-600 dark:text-blue-400 underline decoration-2 underline-offset-4",
        outside: "text-gray-300 dark:text-gray-600 opacity-50",
        disabled: "text-gray-300 dark:text-gray-600 opacity-40 cursor-not-allowed",
        hidden: "invisible",
        ...classNames,
      }}
      components={{
        Chevron: ({ orientation, ...props }) => {
          if (orientation === "left") {
            return <ChevronLeft className="h-4 w-4" {...props} />;
          }
          return <ChevronRight className="h-4 w-4" {...props} />;
        },
      }}
      {...props}
    />
  );
}
Calendar.displayName = "Calendar";

export { Calendar };
