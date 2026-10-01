"use client";

import React from "react";
import { type DateRange } from "react-day-picker";
import { Button } from "@/components/ui/button";
import { type PresetItem } from "./types";

interface PresetListProps {
  presets: PresetItem[];
  onSelectPreset: (range: DateRange) => void;
}

export function PresetList({ presets, onSelectPreset }: PresetListProps) {
  return (
    <div className="border-b sm:border-b-0 sm:border-r border-gray-200 dark:border-gray-800 p-2.5 flex flex-row sm:flex-col gap-1 overflow-x-auto sm:overflow-visible min-w-[130px] bg-gray-50/40 dark:bg-gray-900/40">
      <span className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider px-2 py-1 hidden sm:block">
        Presets
      </span>
      {presets.map((preset) => (
        <Button
          key={preset.label}
          variant="ghost"
          size="sm"
          className="justify-start text-xs font-normal h-7 px-2 hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-blue-950/30 dark:hover:text-blue-400"
          onClick={() => {
            const r = preset.getRange();
            onSelectPreset(r);
          }}>
          {preset.label}
        </Button>
      ))}
    </div>
  );
}
