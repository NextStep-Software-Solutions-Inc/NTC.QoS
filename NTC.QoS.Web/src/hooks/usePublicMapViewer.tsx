"use client";

import React, { createContext, useContext, useState, type ReactNode } from "react";

interface PublicViewerProps {
  searchQuery: string;
  setSearchQuery: (query: string) => void;
  locationFound: boolean;
  showIconTracker: boolean;
  setShowIconTracker: (type: boolean) => void;
  zoomLevel: number;
  setZoomLevel: (zoom: number) => void;
  mapType: string;
  setMapType: (type: string) => void;
  selectedCoordinates: [number, number] | null;
  setSelectedCoordinates: (coords: [number, number] | null) => void;
  setLocationFound: (type: boolean) => void;
}

const PublicMapViewerContext = createContext<PublicViewerProps | undefined | null>(null);

export const PublicMapViewerProvider = ({ children }: { children: ReactNode }) => {
  const [searchQuery, setSearchQuery] = useState("");
  const [locationFound, setLocationFound] = useState(false);
  const [showIconTracker, setShowIconTracker] = useState(false);
  const [zoomLevel, setZoomLevel] = useState(6);
  const [mapType, setMapType] = useState("street");
  const [selectedCoordinates, setSelectedCoordinates] = useState<[number, number] | null>(null);

  return (
    <PublicMapViewerContext.Provider
      value={{
        searchQuery,
        setSearchQuery,
        locationFound,
        setLocationFound,
        showIconTracker,
        setShowIconTracker,
        zoomLevel,
        setZoomLevel,
        mapType: mapType,
        setMapType,
        selectedCoordinates,
        setSelectedCoordinates,
      }}>
      {children}
    </PublicMapViewerContext.Provider>
  );
};

export const usePublicMapViewer = () => {
  const context = useContext(PublicMapViewerContext);
  if (!context) {
    throw new Error("usePublicMapViewer must be used within or wrap as child to parent PublicMapViewerProvider");
  }
  return context;
};
