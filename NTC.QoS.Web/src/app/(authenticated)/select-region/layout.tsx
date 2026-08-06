import React from "react";

import "@/styles/globals.css";

import { SignedIn } from "@clerk/nextjs";

import { DebounceProvider } from "@/hooks/useDebounceContext";
import { MapProvider } from "@/hooks/useMap";
import { RegionDomainProvider } from "@/hooks/useRegionDomain";
import { LocationMapProvider } from "@/providers/LocationMapContext";

const MAPTILER_API_KEY = process.env.MAPTILER_API_KEY ?? "";

function SelectRegionLayout({ children }: { children: React.ReactNode }) {
  return (
    <SignedIn>
      <LocationMapProvider>
        <RegionDomainProvider>
          <DebounceProvider>
            <MapProvider apiKey={MAPTILER_API_KEY}>
              {/* Override parent layout - no navbar, full screen */}
              <div className="fixed inset-0 z-50 bg-background">{children}</div>
            </MapProvider>
          </DebounceProvider>
        </RegionDomainProvider>
      </LocationMapProvider>
    </SignedIn>
  );
}

export default SelectRegionLayout;
