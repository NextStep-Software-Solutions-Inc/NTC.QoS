"use client";

import * as React from "react";
import { format, subYears } from "date-fns";
import { CalendarIcon } from "lucide-react";
import { type DateRange } from "react-day-picker";

import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export function DatePicker({
  className,
  date: externalDate,
  onDateChange,
}: React.HTMLAttributes<HTMLDivElement> & { date?: DateRange; onDateChange?: (date: DateRange | undefined) => void }) {
  const [internalDate, setInternalDate] = React.useState<DateRange | undefined>({
    from: subYears(new Date(), 1),
    to: new Date(),
  });

  const date = externalDate ?? internalDate;
  const setDate = onDateChange ?? setInternalDate;

  return (
    <div className={cn("flex items-center justify-center gap-3 rounded-sm px-3", className)}>
      <Popover>
        <PopoverTrigger className="h-10" asChild>
          <Button
            id="date"
            variant={"outline"}
            className={cn("w-[300px] justify-start text-left font-normal", !date && "text-muted-foreground")}>
            <CalendarIcon />
            {date?.from ? (
              date.to ? (
                <>
                  {format(date.from, "LLL dd, y")} - {format(date.to, "LLL dd, y")}
                </>
              ) : (
                format(date.from, "LLL dd, y")
              )
            ) : (
              <span>Pick a date</span>
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar
            initialFocus
            mode="range"
            defaultMonth={date?.from}
            selected={date}
            onSelect={setDate}
            numberOfMonths={2}
          />
        </PopoverContent>
      </Popover>
    </div>
  );
}
