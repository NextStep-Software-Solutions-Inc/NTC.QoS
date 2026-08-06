import React from "react";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import CoverageMap from "./CoverageMap";
import TechnologyBands from "./TechnologyBands";
import TowerAntennaSpec from "./TowerAntennaSpeci";

const TabsUnderProviderName = () => {
  return (
    <Tabs defaultValue="technologies" className="">
      <TabsList className=" px-3 py-2 ">
        <TabsTrigger value="technologies">Technologies</TabsTrigger>
        <TabsTrigger value="antenna-details">Antenna Details</TabsTrigger>
        <TabsTrigger value="coverage">Coverage</TabsTrigger>
      </TabsList>
      <TabsContent value="technologies">
        <TechnologyBands />
      </TabsContent>
      <TabsContent value="antenna-details">
        <TowerAntennaSpec />
      </TabsContent>
      <TabsContent value="coverage">
        <CoverageMap />
      </TabsContent>
    </Tabs>
  );
};

export default TabsUnderProviderName;
