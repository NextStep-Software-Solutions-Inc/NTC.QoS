"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Image from "next/image";
import * as maptilersdk from "@maptiler/sdk";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronDown, Map } from "lucide-react";

import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";
import { GlassPanel } from "./GlassPanel";

const mapStyles = [
  {
    id: "streets",
    name: "Streets",
    src: "https://cloud.maptiler.com/static/img/maps/streets-v4.png",
    style: maptilersdk.MapStyle.STREETS_V4,
  },
  {
    id: "satellite",
    name: "Satellite",
    src: "https://cloud.maptiler.com/static/img/maps/hybrid.png",
    style: maptilersdk.MapStyle.HYBRID,
  },
  {
    id: "dark",
    name: "Dark",
    src: "https://cloud.maptiler.com/static/img/maps/streets-v4-dark.png",
    style: maptilersdk.MapStyle.STREETS_V4.DARK,
  },
  {
    id: "terrain",
    name: "Terrain",
    src: "https://cloud.maptiler.com/static/img/maps/outdoor.png",
    style: maptilersdk.MapStyle.OUTDOOR,
  },
  {
    id: "topo",
    name: "Topography",
    src: "https://cloud.maptiler.com/static/img/maps/topo-v4.png",
    style: maptilersdk.MapStyle.TOPO,
  },
  {
    id: "topo-dark",
    name: "Topography Dark",
    src: "https://cloud.maptiler.com/static/img/maps/topo-v4-dark.png",
    style: maptilersdk.MapStyle.TOPO.DARK,
  },
];

export function MapStyleSwitcher() {
  const { setMapStyle } = useMap();
  const [isOpen, setIsOpen] = useState(false);
  const [activeStyle, setActiveStyle] = useState("topo");
  const containerRef = useRef<HTMLDivElement>(null);

  // Close on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isOpen]);

  const handleStyleChange = useCallback(
    (styleId: string, style: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant) => {
      setActiveStyle(styleId);
      setMapStyle(style);
      setIsOpen(false);
    },
    [setMapStyle],
  );

  const activeStyleData = mapStyles.find((s) => s.id === activeStyle);

  return (
    <div ref={containerRef} className="relative">
      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0, y: 10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.95 }}
            transition={{ duration: 0.2 }}
            className="absolute bottom-full mb-2 right-0 min-w-[280px] max-w-[calc(100vw-2rem)]">
            <GlassPanel className="p-3">
              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Map Styles</p>
              <div className="space-y-3">
                {mapStyles.map((style) => {
                  const isActive = activeStyle === style.id;

                  return (
                    <button
                      key={style.id}
                      onClick={() => handleStyleChange(style.id, style.style)}
                      className={cn(
                        "w-full flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-200",
                        "hover:bg-gray-100/80 focus:outline-none focus:ring-2 focus:ring-primary/50",
                        activeStyle === style.id && "bg-primary/10",
                      )}>
                      <div
                        className={cn(
                          "w-8 h-8 rounded-lg flex items-center justify-center transition-colors overflow-hidden",
                          activeStyle === style.id ? "bg-primary/20" : "bg-gray-100",
                        )}>
                        <Image
                          src={style.src}
                          alt="Map style"
                          width={32}
                          height={32}
                          unoptimized
                          className="w-full h-full object-cover"
                        />
                      </div>
                      <div className="flex-1 text-left">
                        <span
                          className={cn(
                            "text-sm font-medium block",
                            activeStyle === style.id ? "text-gray-800" : "text-gray-500",
                          )}>
                          {style.name}
                        </span>
                      </div>
                      <motion.div
                        className={cn(
                          "w-5 h-5 rounded-md border-2 flex items-center justify-center shrink-0",
                          activeStyle === style.id ? "border-primary bg-primary" : "border-gray-300",
                        )}
                        animate={activeStyle === style.id ? { scale: [1, 1.2, 1] } : { scale: 1 }}
                        transition={{ duration: 0.2 }}>
                        {activeStyle === style.id && (
                          <motion.svg
                            initial={{ pathLength: 0 }}
                            animate={{ pathLength: 1 }}
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
                  );
                })}
              </div>
            </GlassPanel>
          </motion.div>
        )}
      </AnimatePresence>

      <GlassPanel className="p-1.5">
        <button
          onClick={() => setIsOpen(!isOpen)}
          className={cn(
            "flex items-center gap-2 px-3 py-2 rounded-xl transition-all duration-200",
            "hover:bg-gray-100/80 focus:outline-none focus:ring-2 focus:ring-primary/50",
          )}>
          <Map className="h-5 w-5 text-gray-700" />
          <span className="text-sm font-medium text-gray-700 hidden">{activeStyleData?.name ?? "Topography"}</span>
          <motion.div animate={{ rotate: isOpen ? 180 : 0 }} transition={{ duration: 0.2 }}>
            <ChevronDown className="h-4 w-4 text-gray-500" />
          </motion.div>
        </button>
      </GlassPanel>
    </div>
  );
}
