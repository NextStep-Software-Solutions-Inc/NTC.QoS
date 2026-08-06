import { useState } from "react";
import { motion } from "framer-motion";
import { ChevronLeft, ChevronRight } from "lucide-react";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import SearchPHLocations from "../Map/SearchPHLocations";
import CellTab from "../Tabs/CellTab";
import FiberTab from "../Tabs/FiberTab";
import SpeedTestTab from "../Tabs/SpeedTestTab";
import { Button } from "../ui/button";
import { ScrollArea, ScrollBar } from "../ui/scroll-area";

export default function PanelMenu() {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div className="relative">
      <motion.div
        initial={{ x: "-100%" }}
        animate={{ x: collapsed ? "-100%" : 0 }}
        transition={{ duration: 0.3 }}
        className="fixed top-0 left-0 h-full w-[420px] bg-white shadow-lg flex flex-col">
        {/* Header */}
        <div className="p-4 border-b shrink-0">
          <SearchPHLocations />
          <div className="flex items-center justify-between mt-2">
            <div>
              <h2>Filters</h2>
              <p>Use the options below to filter the map data.</p>
            </div>
            <Button variant="outline" size="sm" aria-label="Reset filters">
              Reset
            </Button>
          </div>
        </div>

        {/* Content */}
        <ScrollArea className="flex-1">
          <div className="px-3">
            <Tabs defaultValue="cell" className="flex flex-col flex-1">
              <TabsList className="grid w-full grid-cols-3 rounded-none">
                <TabsTrigger value="cell">Cell</TabsTrigger>
                <TabsTrigger value="fiber">Fiber</TabsTrigger>
                <TabsTrigger value="speed-test">Speed Test</TabsTrigger>
              </TabsList>

              <div className="px-6 py-4">
                <TabsContent value="cell">
                  <CellTab />
                </TabsContent>
                <TabsContent value="fiber">
                  <FiberTab />
                </TabsContent>
                <TabsContent value="speed-test">
                  <SpeedTestTab />
                </TabsContent>
              </div>
            </Tabs>
          </div>
          <ScrollBar orientation="vertical" />
        </ScrollArea>

        {/* Floating collapse button */}
        {collapsed ? (
          <button
            onClick={() => {
              setCollapsed(false);
            }}
            className="absolute top-1/2 -right-4 z-50 bg-black text-white rounded-full p-2 shadow-md hover:bg-gray-800">
            <ChevronRight size={16} />
          </button>
        ) : (
          <button
            onClick={() => {
              setCollapsed(true);
            }}
            className="absolute top-1/2 -right-4 z-50 bg-black text-white rounded-full p-2 shadow-md hover:bg-gray-800">
            <ChevronLeft size={16} />
          </button>
        )}
      </motion.div>
    </div>
  );
}
