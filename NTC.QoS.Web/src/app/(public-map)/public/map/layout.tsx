import React from "react";

import "@/styles/globals.css";

import { MapProvider } from "@/hooks/useMap";

const MAPTILER_API_KEY = process.env.MAPTILER_API_KEY ?? "";

function PublicMapLayout({ children }: { children: React.ReactNode }) {
  return (
    <MapProvider apiKey={MAPTILER_API_KEY}>
      <div className="fixed inset-0 overflow-hidden">{children}</div>
    </MapProvider>
  );
}

export default PublicMapLayout;
