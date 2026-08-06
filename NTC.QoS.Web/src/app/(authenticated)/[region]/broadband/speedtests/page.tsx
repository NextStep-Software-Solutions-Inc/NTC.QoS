import { Suspense } from "react";
import { type Metadata } from "next";
import dynamic from "next/dynamic";
import { Activity } from "lucide-react";

// Use new Speedtest Management feature component
const ManageSpeedtests = dynamic(() => import("@/components/features/speedtest/ManageSpeedtests"), {
  loading: () => (
    <div className="flex flex-col h-full w-full items-center justify-center min-h-[400px] gap-4">
      <div className="rounded-full p-4 bg-gradient-to-br from-cyan-500 to-blue-600 animate-pulse">
        <Activity className="h-8 w-8 text-white" />
      </div>
      <div className="text-center">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-200">Loading Speedtest Management</h2>
        <p className="text-sm text-gray-500 dark:text-gray-400">Please wait...</p>
      </div>
    </div>
  ),
});

interface SpeedtestsPageProps {
  params: Promise<{ region: string }>;
}

export async function generateMetadata({ params }: SpeedtestsPageProps): Promise<Metadata> {
  const { region } = await params;
  return {
    title: `Speedtest Management | Region ${region}`,
    description: "Monitor, analyze, and manage broadband speedtest results with analytics and visualizations.",
  };
}

export default async function SpeedtestsPage({ params }: SpeedtestsPageProps) {
  const { region } = await params;

  return (
    <div className="flex flex-col h-full w-full overflow-hidden">
      <Suspense
        fallback={
          <div className="flex flex-col h-full w-full items-center justify-center min-h-[400px] gap-4">
            <div className="rounded-full p-4 bg-gradient-to-br from-cyan-500 to-blue-600 animate-pulse">
              <Activity className="h-8 w-8 text-white" />
            </div>
            <div className="text-center">
              <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-200">Loading Speedtest Management</h2>
              <p className="text-sm text-gray-500 dark:text-gray-400">Please wait...</p>
            </div>
          </div>
        }>
        <ManageSpeedtests regionId={region} />
      </Suspense>
    </div>
  );
}
