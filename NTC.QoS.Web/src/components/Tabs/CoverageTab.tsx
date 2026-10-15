/* eslint-disable @next/next/no-img-element */
import React from "react";
import { Globe, Signal, Wifi } from "lucide-react";

const CoverageTab = () => {
  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div>
        <h2 className="text-xl font-semibold text-gray-800 flex items-center gap-2">
          <Globe className="w-5 h-5 text-blue-500" />
          Coverage Map
        </h2>
        <p className="text-gray-500 text-sm">Estimated coverage range and population served</p>
      </div>

      {/* Coverage Info */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 text-center">
        <div className="flex flex-col items-center">
          <Globe className="w-6 h-6 text-blue-500 mb-2" />
          <p className="text-gray-700 font-medium">Coverage Area</p>
          <p className="text-gray-500 text-sm">72.5 sq.km</p>
        </div>
        <div className="flex flex-col items-center">
          <Signal className="w-6 h-6 text-green-500 mb-2" />
          <p className="text-gray-700 font-medium">Coverage Quality</p>
          <p className="text-gray-500 text-sm">Mostly Green</p>
        </div>
        <div className="flex flex-col items-center">
          <Wifi className="w-6 h-6 text-purple-500 mb-2" />
          <p className="text-gray-700 font-medium">Barangays Covered</p>
          <p className="text-gray-500 text-sm">~25 Barangays</p>
        </div>
      </div>

      {/* Static Map */}
      <div className="relative rounded-2xl overflow-hidden shadow-md">
        <img src="/assets/images/coverage-map1.png" alt="Coverage Map" className="w-full h-96 object-cover" />
        {/* View in Map button */}
        <button className="absolute bottom-4 right-4 bg-white text-gray-700 text-sm px-4 py-2 rounded-md shadow hover:bg-gray-100">
          View in Map
        </button>
      </div>
    </div>
  );
};

export default CoverageTab;
