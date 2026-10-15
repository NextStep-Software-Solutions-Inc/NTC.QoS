import React from "react";

import "@/styles/globals.css";

import { SignedIn } from "@clerk/nextjs";

// import AppSidebar from "@/components/admin-panel/AppSidebar";
import DashboardNavbar from "@/components/dashboard/DashboardNavbar";
// import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { DebounceProvider } from "@/hooks/useDebounceContext";
import { MapProvider } from "@/hooks/useMap";
import { RegionDomainProvider } from "@/hooks/useRegionDomain";
import { LocationMapProvider } from "@/providers/LocationMapContext";

const MAPTILER_API_KEY = process.env.MAPTILER_API_KEY ?? "";
function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  return (
    <SignedIn>
      <LocationMapProvider>
        <RegionDomainProvider>
          <DebounceProvider>
            <MapProvider apiKey={MAPTILER_API_KEY}>
              <div className="flex flex-col h-screen">
                <DashboardNavbar />
                <div className="flex-1 min-h-0">
                  {children}
                </div>
              </div>
            </MapProvider>
          </DebounceProvider>
        </RegionDomainProvider>
      </LocationMapProvider>
    </SignedIn>
  );
}

export default AuthenticatedLayout;
