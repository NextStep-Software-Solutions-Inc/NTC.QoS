"use client";

import { useEffect, useMemo, useState } from "react";
import type * as maptilersdk from "@maptiler/sdk";
import { ChevronDown, ChevronRight, Wifi } from "lucide-react";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";
import { type Service } from "../features/service/types/Service";
import { getSpeedtestsRequest } from "../features/speedtest/handlers/getSpeedtestsRequest";
import { Skeleton } from "../ui/skeleton";
import WithPlaceholder from "../WithPlaceholder";

interface InternetServiceProps {
  service: Service;
  children: React.ReactNode;
}

const useSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);

const InternetServiceType = ({ service, children }: InternetServiceProps) => {
  const [openDetails, setOpenDetails] = useState(false);
  const { map, isMapLoaded } = useMap();
  const [lat, setLat] = useState(0);
  const [lng, setLng] = useState(0);

  const { data: speedtests, isLoading } = useSpeedtests({
    pathParams: {},
    queryParams: { pageSize: 10, latitude: lat, longitude: lng, distance: 100, serviceIds: [service.id] },
  });

  const providers = speedtests?.data.reduce((acc, item) => {
    const providerName = item.serviceProvider.provider.name;
    if (providerName) {
      acc.add(providerName);
    }
    return acc;
  }, new Set<string>());

  const isAvailable = useMemo(() => (providers?.size ?? 0) > 0, [providers]);

  const ArrowDirection = useMemo(() => (openDetails ? ChevronDown : ChevronRight), [openDetails]);

  useEffect(() => {
    if (!map || !isMapLoaded) return;
    const locatePlace = (e: maptilersdk.MapMouseEvent & {}) => {
      setLat(e.lngLat.lat);
      setLng(e.lngLat.lng);
    };

    map.on("click", locatePlace);

    return () => {
      map.off("click", locatePlace);
    };
  }, [map, isMapLoaded]);

  // open details when the service is available
  useEffect(() => {
    setOpenDetails(isAvailable);
  }, [isAvailable]);

  return (
    <>
      <section
        onClick={() => setOpenDetails((prev) => isAvailable && !prev)}
        className={`flex w-full cursor-default items-center justify-between py-1`}>
        <div className="flex items-center gap-2">
          <span className="flex items-center justify-center rounded-full bg-blue-500 p-[6px] text-white">
            <Wifi size={16} className="" />
          </span>
          <span className="text-[13px] font-medium">{service.name}</span>
        </div>
        <div className="flex items-center justify-between gap-1">
          <WithPlaceholder
            isLoading={isLoading}
            placeholder={
              <>
                <Skeleton className="h-5 w-[80px] rounded-full" />
                <Skeleton className="h-5 w-5 rounded-full" />
              </>
            }>
            <span
              className={cn("rounded-full px-3 py-[2px] text-[11px] text-white bg-red-500", {
                "bg-green-500": isAvailable,
              })}>
              {isAvailable ? "Available" : "Not Available"}
            </span>

            <ArrowDirection className="text-muted-foreground" size={16} />
          </WithPlaceholder>
        </div>
      </section>
      {openDetails && children}
    </>
  );
};

export default InternetServiceType;
