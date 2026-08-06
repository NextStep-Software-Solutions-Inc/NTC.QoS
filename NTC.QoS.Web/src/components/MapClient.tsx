"use client";

import dynamic from "next/dynamic";

const Map = dynamic(() => import("@/components/Map/PublicViewMap"), { ssr: false });

export default function MapClient() {
  return <Map />;
}
