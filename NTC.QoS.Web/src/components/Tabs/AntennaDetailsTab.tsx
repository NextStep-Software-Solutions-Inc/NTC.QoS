"use client";

import React from "react";
import { AntennaIcon, PlusIcon } from "lucide-react";

import { Button } from "@/components/ui/button";

const AntennaDetailsTab = () => {
  return (
    <div>
      {/* Empty state */}
      <div className="flex flex-col items-center justify-center text-center py-12 px-6 bg-white border border-gray-200 rounded-lg">
        <div className="w-16 h-16 flex items-center justify-center bg-gray-100 rounded-full mb-4">
          <AntennaIcon className="w-8 h-8 text-gray-400" />
        </div>
        <h3 className="text-lg font-semibold text-gray-500">No Antennas Added</h3>
        <p className="text-sm text-gray-400 mt-1 max-w-xs">
          Add antennas to this site to start monitoring signal coverage and performance.
        </p>
      </div>

      {/* Trigger */}
      <Button className="mt-6 w-full flex items-center justify-center">
        <PlusIcon className="w-5 h-5 mr-2" />
        Add Antenna
      </Button>

      {/* Dialog */}
      {/* <CreateAntennaDialog
        open={isDialogOpen}
        onOpenChange={setIsDialogOpen}
        onCreated={handleCreated}
      /> */}
    </div>
  );
};

export default AntennaDetailsTab;
