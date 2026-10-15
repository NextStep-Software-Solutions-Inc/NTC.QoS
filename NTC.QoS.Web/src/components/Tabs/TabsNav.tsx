"use client";

import React, { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";

import { cn } from "@/lib/utils";
import Each from "../Each";
import { Tabs, TabsList, TabsTrigger } from "../ui/tabs";

export interface TabItem {
  label: string;
  value: string;
  shortLabel?: string;
  icon?: React.ReactNode;
}

export interface TabsNavProps {
  tabs: TabItem[];
  currentTab: string;
  getTabHref: (tabValue: string) => string;
  className?: string;
  variant?: "default" | "scrollable";
}

const TabsNav: React.FC<TabsNavProps> = ({ tabs, currentTab, getTabHref, className = "", variant = "scrollable" }) => {
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const updateScrollButtons = () => {
    if (!scrollContainerRef.current) return;

    const { scrollLeft, scrollWidth, clientWidth } = scrollContainerRef.current;
    setCanScrollLeft(scrollLeft > 5);
    setCanScrollRight(scrollLeft < scrollWidth - clientWidth - 5);
  };

  useEffect(() => {
    updateScrollButtons();
    const handleResize = () => updateScrollButtons();
    window.addEventListener("resize", handleResize);

    return () => window.removeEventListener("resize", handleResize);
  }, [tabs]);

  const scroll = (direction: "left" | "right") => {
    if (!scrollContainerRef.current) return;

    const scrollAmount = direction === "left" ? -240 : 240;
    scrollContainerRef.current.scrollBy({ left: scrollAmount, behavior: "smooth" });
  };

  if (variant === "default") {
    return (
      <Tabs defaultValue={currentTab}>
        <TabsList className={className}>
          <Each of={tabs} keyExtractor={(tab) => tab.value}>
            {(tab) => (
              <TabsTrigger value={tab.value} asChild>
                <Link href={getTabHref(tab.value)} className="flex items-center gap-2">
                  {tab.icon}
                  <span>{tab.label}</span>
                </Link>
              </TabsTrigger>
            )}
          </Each>
        </TabsList>
      </Tabs>
    );
  }

  return (
    <Tabs defaultValue={currentTab}>
      <div className="relative w-full py-2 md:py-4">
        {/* Gradient fade-out on left */}
        {canScrollLeft && (
          <div className="absolute left-0 top-0 bottom-0 w-16 bg-gradient-to-r from-background via-background/80 to-transparent z-10 pointer-events-none" />
        )}

        {/* Gradient fade-out on right */}
        {canScrollRight && (
          <div className="absolute right-0 top-0 bottom-0 w-16 bg-gradient-to-l from-background via-background/80 to-transparent z-10 pointer-events-none" />
        )}

        {/* Left scroll button */}
        {canScrollLeft && (
          <button
            onClick={() => scroll("left")}
            className={cn(
              "absolute left-1 top-1/2 -translate-y-1/2 z-20",
              "h-9 w-9 rounded-full",
              "bg-background border-2 border-border/60 shadow-md",
              "flex items-center justify-center",
              "hover:bg-primary hover:text-primary-foreground hover:border-primary",
              "transition-all duration-200",
              "active:scale-90",
              "backdrop-blur-sm",
            )}
            aria-label="Scroll left">
            <ChevronLeft className="h-4 w-4 stroke-[2.5]" />
          </button>
        )}

        {/* Scrollable tabs container */}
        <div
          ref={scrollContainerRef}
          onScroll={updateScrollButtons}
          className={cn(
            "overflow-x-auto scrollbar-hide",
            "scroll-smooth snap-x snap-mandatory",
            "md:mx-0",
            "touch-pan-x",
          )}>
          <TabsList
            className={cn(
              "inline-flex w-auto min-w-full md:w-full",
              "h-auto gap-1.5 p-1.5",
              "bg-muted/50 md:bg-muted",
              "rounded-none md:rounded-lg",
              "shadow-none md:shadow-sm",
              className,
            )}>
            <Each of={tabs} keyExtractor={(tab) => tab.value}>
              {(tab) => (
                <TabsTrigger
                  value={tab.value}
                  asChild
                  className={cn(
                    "flex-shrink-0 snap-start",
                    "min-w-fit",
                    "px-4 py-2.5 md:px-3 md:py-2",
                    "text-sm font-medium",
                    "rounded-lg md:rounded-md",
                    "transition-all duration-200",
                    "data-[state=active]:shadow-md md:data-[state=active]:shadow",
                    "data-[state=active]:scale-[1.02] md:data-[state=active]:scale-100",
                    "hover:bg-background/50",
                    "active:scale-[0.98]",
                  )}>
                  <Link href={getTabHref(tab.value)} className="flex items-center gap-2 min-h-[44px] md:min-h-0">
                    {/* Icon - show on mobile, optional on desktop */}
                    {tab.icon && <span className="flex-shrink-0 text-base md:text-sm">{tab.icon}</span>}
                    {/* Label - responsive */}
                    <span className="hidden sm:inline whitespace-nowrap">{tab.label}</span>
                    <span className="sm:hidden whitespace-nowrap font-medium">{tab.shortLabel || tab.label}</span>
                  </Link>
                </TabsTrigger>
              )}
            </Each>
          </TabsList>
        </div>

        {/* Right scroll button */}
        {canScrollRight && (
          <button
            onClick={() => scroll("right")}
            className={cn(
              "absolute right-1 top-1/2 -translate-y-1/2 z-20",
              "h-9 w-9 rounded-full",
              "bg-background border-2 border-border/60 shadow-md",
              "flex items-center justify-center",
              "hover:bg-primary hover:text-primary-foreground hover:border-primary",
              "transition-all duration-200",
              "active:scale-90",
              "backdrop-blur-sm",
            )}
            aria-label="Scroll right">
            <ChevronRight className="h-4 w-4 stroke-[2.5]" />
          </button>
        )}
      </div>
    </Tabs>
  );
};

export default TabsNav;
