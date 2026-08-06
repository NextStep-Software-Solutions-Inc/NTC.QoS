"use client";

import React, { useEffect, useState } from "react";
import type { MapMouseEvent } from "@maptiler/sdk";

import { useMap } from "@/hooks/useMap";
import useReverseGeoCoding from "@/hooks/useReverseGeoCoding";
import InternetServices from "./public-map/InternetServices";
import { SidebarGroupLabel, SidebarMenu, SidebarMenuItem } from "./ui/sidebar";
import { Skeleton } from "./ui/skeleton";
import WithPlaceholder from "./WithPlaceholder";

function LoadInternetServices() {
  const { map, isMapLoaded } = useMap();
  const [lat, setLat] = useState(0);
  const [lng, setLng] = useState(0);

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const locatePlace = (e: MapMouseEvent & {}) => {
      setLat(e.lngLat.lat);
      setLng(e.lngLat.lng);
    };

    map.on("click", locatePlace);

    return () => {
      map.off("click", locatePlace);
    };
  }, [map, isMapLoaded]);

  return (
    <div>
      {!!lat && !!lng ? (
        <>
          <PlaceDetails lat={lat} lng={lng} />
          <SidebarGroupLabel>Internet Services Available</SidebarGroupLabel>
          <InternetServices lat={lat} lng={lng} />
        </>
      ) : (
        <div className="text-xs text-primary ml-2">Click on the map to see the address details</div>
      )}
    </div>
  );
}

export default LoadInternetServices;

const PlaceDetails = ({ lat, lng }: { lat: number; lng: number }) => {
  const { results, isLoading } = useReverseGeoCoding({ lng, lat });
  const keys = Object.keys(results ?? {});
  if ((!isLoading && keys.length === 1 && keys.includes("country")) || keys.length === 0) return null;

  return (
    <>
      <SidebarGroupLabel>Place Details</SidebarGroupLabel>
      <SidebarMenu className="mb-2 rounded-md border px-4 py-2 shadow-md">
        <SidebarMenuItem>
          <WithPlaceholder
            isLoading={isLoading}
            placeholder={<Skeleton className="my-1 h-4 w-1/2 rounded-md bg-muted" />}>
            <div className="text-base font-semibold text-primary">{results?.address}</div>
          </WithPlaceholder>
          <div>
            <WithPlaceholder isLoading={isLoading} placeholder={<Skeleton className="my-1 h-4 w-full" />}>
              <div className="text-sm text-muted-foreground">
                {results?.place} {results?.municipality}
              </div>
            </WithPlaceholder>
            <WithPlaceholder isLoading={isLoading} placeholder={<Skeleton className="my-1 h-4 w-full" />}>
              <div className="text-xs text-primary">
                {results?.region} {results?.postal_code}
              </div>
            </WithPlaceholder>
          </div>
        </SidebarMenuItem>
      </SidebarMenu>
    </>
  );
};
