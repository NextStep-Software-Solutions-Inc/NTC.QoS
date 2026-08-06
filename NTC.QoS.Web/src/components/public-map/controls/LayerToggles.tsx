"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { AnimatePresence, motion } from "framer-motion";
import { Cable, ChevronDown, Layers, Signal, type LucideIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { GlassPanel } from "./GlassPanel";

interface LayerToggle {
  id: string;
  label: string;
  icon: LucideIcon;
  param: string;
  color: string;
  description?: string;
  hasOpacity?: boolean;
}

const layers: LayerToggle[] = [
  {
    id: "qos",
    label: "Fiber Coverage",
    icon: Cable,
    param: "qos",
    color: "text-violet-500",
    description: "Service provider coverage areas",
    hasOpacity: true,
  },
  {
    id: "cellular",
    label: "Cell Site",
    icon: Signal,
    param: "cellular",
    color: "text-blue-500",
    description: "Cell tower locations",
    hasOpacity: false,
  },
];

// Layer opacity context
interface LayerOpacityContextType {
  opacities: Record<string, number>;
  setOpacity: (layerId: string, opacity: number) => void;
}

const LayerOpacityContext = createContext<LayerOpacityContextType>({ opacities: { qos: 70 }, setOpacity: () => {} });

export const useLayerOpacity = () => useContext(LayerOpacityContext);

export function LayerOpacityProvider({ children }: { children: React.ReactNode }) {
  const [opacities, setOpacities] = useState<Record<string, number>>({ qos: 70 });

  const setOpacity = useCallback((layerId: string, opacity: number) => {
    setOpacities((prev) => ({ ...prev, [layerId]: opacity }));
  }, []);

  return <LayerOpacityContext.Provider value={{ opacities, setOpacity }}>{children}</LayerOpacityContext.Provider>;
}

export function LayerToggles() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isOpen, setIsOpen] = useState(false);
  const { opacities, setOpacity } = useLayerOpacity();
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

  // Derive activeFilters from URL params (no effect needed)
  const activeFilters = useMemo(() => {
    const filters = new Set<string>();
    layers.forEach((layer) => {
      const param = searchParams.get(layer.param);
      if (param === "true") {
        filters.add(layer.id);
      }
    });
    return filters;
  }, [searchParams]);

  const handleToggle = useCallback(
    (layer: LayerToggle) => {
      const newStatus = !activeFilters.has(layer.id);

      // Update URL params using Next.js router
      const params = new URLSearchParams(searchParams.toString());
      params.set(layer.param, String(newStatus));
      router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    },
    [activeFilters, pathname, router, searchParams],
  );

  const activeCount = activeFilters.size;

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
              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Map Layers</p>
              <div className="space-y-3">
                {layers.map((layer) => {
                  const Icon = layer.icon;
                  const isActive = activeFilters.has(layer.id);

                  return (
                    <div key={layer.id} className="space-y-2">
                      <button
                        onClick={() => handleToggle(layer)}
                        className={cn(
                          "w-full flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-200",
                          "hover:bg-gray-100/80 focus:outline-none focus:ring-2 focus:ring-primary/50",
                          isActive && "bg-primary/10",
                        )}>
                        <div
                          className={cn(
                            "w-8 h-8 rounded-lg flex items-center justify-center transition-colors",
                            isActive ? "bg-primary/20" : "bg-gray-100",
                          )}>
                          <Icon className={cn("h-4 w-4", isActive ? layer.color : "text-gray-400")} />
                        </div>
                        <div className="flex-1 text-left">
                          <span
                            className={cn("text-sm font-medium block", isActive ? "text-gray-800" : "text-gray-500")}>
                            {layer.label}
                          </span>
                          {layer.description && <span className="text-xs text-gray-400">{layer.description}</span>}
                        </div>
                        <motion.div
                          className={cn(
                            "w-5 h-5 rounded-md border-2 flex items-center justify-center shrink-0",
                            isActive ? "border-primary bg-primary" : "border-gray-300",
                          )}
                          animate={isActive ? { scale: [1, 1.2, 1] } : { scale: 1 }}
                          transition={{ duration: 0.2 }}>
                          {isActive && (
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

                      {/* Opacity slider - only show when layer is active and has opacity control */}
                      {isActive && layer.hasOpacity && (
                        <motion.div
                          initial={{ opacity: 0, height: 0 }}
                          animate={{ opacity: 1, height: "auto" }}
                          exit={{ opacity: 0, height: 0 }}
                          className="px-3 pb-2">
                          <div className="flex items-center gap-3">
                            <span className="text-xs text-gray-400 w-14">Opacity</span>
                            <input
                              id={`opacity-${layer.id}`}
                              name={`opacity-${layer.id}`}
                              type="range"
                              min="10"
                              max="100"
                              value={opacities[layer.id] ?? 70}
                              onChange={(e) => setOpacity(layer.id, parseInt(e.target.value))}
                              className="flex-1 h-1.5 bg-gray-200 rounded-full appearance-none cursor-pointer accent-primary"
                            />
                            <span className="text-xs text-gray-500 w-8 text-right">{opacities[layer.id] ?? 70}%</span>
                          </div>
                        </motion.div>
                      )}
                    </div>
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
          <Layers className="h-5 w-5 text-gray-700" />
          <span className="text-sm font-medium text-gray-700 hidden">Layers</span>
          {activeCount > 0 && (
            <span className="px-1.5 py-0.5 rounded-full bg-primary text-white text-xs font-medium min-w-[20px] text-center">
              {activeCount}
            </span>
          )}
          <motion.div animate={{ rotate: isOpen ? 180 : 0 }} transition={{ duration: 0.2 }}>
            <ChevronDown className="h-4 w-4 text-gray-500" />
          </motion.div>
        </button>
      </GlassPanel>
    </div>
  );
}
