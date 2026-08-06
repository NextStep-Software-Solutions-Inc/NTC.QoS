import React from "react";
import { type Metadata } from "next";

import AdminMapClient from "@/components/AdminMapClient";
import { WebWorkerDebugPanel } from "@/components/WebWorkerDebugPanel";

export const metadata: Metadata = {
  title: "Admin Map | NTC QoS",
  description: "Philippine National Broadband Map Search for broadband locations.",
  // icons: [{ rel: "icon", url: "/favicon.ico" }],
};

const MapPage = () => {
  return (
    <>
      <AdminMapClient />
      <WebWorkerDebugPanel />
    </>
  );
};

export default MapPage;
