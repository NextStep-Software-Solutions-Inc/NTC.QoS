import { useEffect, useRef } from "react";
import { useSearchParams } from "next/navigation";

import { useMap } from "./useMap";

export const useMapFly = () => {
  const { map, isMapLoaded } = useMap();
  const searchParams = useSearchParams();
  const hasInitialized = useRef(false);

  useEffect(() => {
    // Only fly to position on initial mount, not on every URL change
    if (!map || !isMapLoaded || hasInitialized.current) return;

    const params = Object.fromEntries(searchParams.entries());

    map.flyTo({
      center: [params.lng ? parseFloat(params.lng) : 122.405211, params.lat ? parseFloat(params.lat) : 12.796927],
      zoom: params.zoom ? parseFloat(params.zoom) : 5.38,
      essential: true, // this animation is considered essential with respect to prefers-reduced-motion
    });

    hasInitialized.current = true;
  }, [isMapLoaded, map, searchParams]);
};
