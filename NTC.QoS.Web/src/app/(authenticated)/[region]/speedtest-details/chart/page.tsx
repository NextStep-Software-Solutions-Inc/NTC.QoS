import React from "react";
import { type Metadata } from "next";

import SpeedtestChart from "@/components/Tables/SpeedtestChart";

export const metadata: Metadata = {
  title: "Speedtest Chart",
  description: "This page contains speedtest details and chart",
};
export default function page() {
  return <SpeedtestChart />;
}
