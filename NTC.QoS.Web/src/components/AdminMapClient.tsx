"use client";

import dynamic from "next/dynamic";

const Map = dynamic(() => import("@/components/Map/AdminViewMap"), { ssr: false });

export default function AdminMapClient() {
  return <Map />;
}
