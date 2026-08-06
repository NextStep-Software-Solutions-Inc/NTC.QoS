import { type Metadata } from "next";

import SpeedtestMapPage from "@/components/features/speedtest/pages/SpeedtestMapPage";

interface SpeedtestMapRouteProps {
  params: Promise<{ region: string }>;
}

export async function generateMetadata({ params }: SpeedtestMapRouteProps): Promise<Metadata> {
  const { region } = await params;
  return {
    title: `Speedtest Map View | Region ${region}`,
    description: "Geographic view and management of speedtest results.",
  };
}

export default async function SpeedtestsMapRoute({ params }: SpeedtestMapRouteProps) {
  const { region } = await params;

  return <SpeedtestMapPage regionId={region} />;
}
