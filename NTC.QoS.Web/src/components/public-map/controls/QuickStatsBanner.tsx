"use client";

import Link from "next/link";

import { QoSLogo } from "@/components/sections/IconsImages";
import { GlassPanel } from "./GlassPanel";

export function QuickStatsBanner() {
  return (
    <GlassPanel className="flex items-center gap-3 lg:px-3 lg:py-2 sm:p-1 md:w-10 lg:w-56">
      <Link href="/" className="flex items-center gap-2 hover:opacity-80 transition-opacity">
        <div className="w-8 h-8 flex items-center justify-center">
          <QoSLogo className="w-full h-full" />
        </div>
        <div className="opacity-0 lg:opacity-100 transition-opacity duration-300 ease-in-out text-nowrap">
          <p className="text-sm font-semibold text-gray-800 leading-tight">QoS & Broadband</p>
          <p className="text-xs text-gray-500 leading-tight">Mapping Portal</p>
        </div>
      </Link>
    </GlassPanel>
  );
}
