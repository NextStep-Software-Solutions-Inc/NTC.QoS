"use client";

import { useEffect, useMemo } from "react";
import { useRouter } from "next/navigation";
import { Gauge, Network, RadioTower, Signal, Wifi } from "lucide-react";

import { getCellSitesByProviderRequest } from "@/components/features/dashboard/handlers/getCellSitesByProviderRequest";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getConnectivityAnalysisRequest } from "../../analytics/handlers/getConnectivityAnalysisRequest";
import { getCellsTopProvinceRequest } from "../../dashboard/handlers/getCellsTopProvinceRequest";
import { AverageSpeedCard } from "./AverageSpeedCard";
import { CMTSScoresChart } from "./CMTSScoresChart";
import { DemographicsCard } from "./DemographicsCard";
import { ServiceProviderCard } from "./ServiceProviderCard";
import { StatCard } from "./StatCard";
import { TopProvincesList } from "./TopProvincesList";

const useCellSitesByProvider = createQueryHookFromTemplate(getCellSitesByProviderRequest);
const useConnectivityAnalysis = createMutationHookFromTemplate(getConnectivityAnalysisRequest);
const useCellTopProvinces = createQueryHookFromTemplate(getCellsTopProvinceRequest);
interface BroadbandDashboardProps {
  region: string;
}

export function BroadbandDashboard({ region }: BroadbandDashboardProps) {
  const router = useRouter();
  const { mutateAsync, data: connectivityData, isPending: isConnectivityLoading } = useConnectivityAnalysis();

  const { data: topProvincesData } = useCellTopProvinces({ pathParams: {}, queryParams: { regionId: region } });
  const { data: cellSitesData } = useCellSitesByProvider({ pathParams: {}, queryParams: { regionId: region } });

  // Auto-fetch connectivity analysis on mount
  useEffect(() => {
    mutateAsync({
      pathParams: {},
      payload: {
        locationIds: [region], // You might want to make this dynamic
        locationType: "Regional",
        includeSpeedTestData: true,
        includeInfrastructureData: true,
      },
    });
  }, [mutateAsync, region]);

  const fiberCoverage = useMemo(() => {
    const percentage = connectivityData?.data.locations[0]?.coverage.fiberCoveragePercentage ?? 0;
    return {
      percentage,
      description:
        percentage >= 90
          ? "High-speed internet available in major cities."
          : percentage >= 70
            ? "Moderate fiber coverage across the region."
            : percentage >= 50
              ? "Limited fiber coverage; expansion needed."
              : "Minimal fiber coverage; underserved area.",
    };
  }, [connectivityData]);

  const mobileCoverage = useMemo(() => {
    const percentage = connectivityData?.data.locations[0]?.coverage.mobileCoveragePercentage ?? 0;
    return {
      percentage,
      description:
        percentage >= 90
          ? "LTE/5G connectivity in most areas."
          : percentage >= 70
            ? "Moderate mobile coverage across the region."
            : percentage >= 50
              ? "Limited mobile coverage; expansion needed."
              : "Minimal mobile coverage; underserved area.",
    };
  }, [connectivityData]);

  const population = connectivityData?.data.locations[0]?.population ?? 0;
  const areaSizeKm2 = connectivityData?.data.locations[0]?.areaSizeKm2 ?? 0;

  const density = useMemo(() => {
    return connectivityData && connectivityData.data.locations[0] && areaSizeKm2 && population
      ? (population / areaSizeKm2).toFixed(2)
      : "N/A";
  }, [connectivityData, areaSizeKm2, population]);

  const cmtsScores = useMemo(() => {
    const scores = connectivityData?.data.summary.averageCMTSScores;
    if (!scores || !Array.isArray(scores)) return [];

    // API now returns an array of CMTS scores with structure { subject, a, fullMark }
    return scores;
  }, [connectivityData]);

  const averageSpeed = useMemo(() => {
    return {
      download: connectivityData?.data.locations[0]?.speedStats?.averageDownloadSpeedMbps || 0,
      upload: connectivityData?.data.locations[0]?.speedStats?.averageUploadSpeedMbps || 0,
      totalTests: connectivityData?.data.locations[0]?.speedStats?.speedTestCount || 0,
      meetsRequirements: connectivityData?.data.locations[0]?.speedStats?.meetsMinimumSpeedRequirement || false,
    };
  }, [connectivityData]);

  const topProvinces = useMemo(() => {
    return topProvincesData?.data.map((province) => ({ name: province.name, count: province.cellCount })) || [];
  }, [topProvincesData]);

  const cellSitesByProvider = useMemo(() => {
    return (
      cellSitesData?.data.providers.map((provider) => ({
        name: provider.providerName,
        count: provider.cellCount,
        fill: provider.providerColor || "#8884d8",
      })) || []
    );
  }, [cellSitesData]);

  return (
    <div className="space-y-6 p-6 bg-gray-50/50 min-h-screen">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900">Broadband Overview</h1>
          <p className="text-muted-foreground mt-1">Monitor network performance and infrastructure.</p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <StatCard
          title="Cell Sites"
          value={connectivityData?.data.summary.totalInfrastructure.cellTowerCount.toLocaleString() || "Loading..."}
          subtext="Total Operational Sites"
          icon={RadioTower}
          color="blue"
          action={{ label: "View Details", onClick: () => router.push(`/${region}/broadband/cellsites`) }}
        />
        <StatCard
          title="Network Access Points"
          value={connectivityData?.data.summary.totalInfrastructure.fiberNAPCount.toLocaleString() || "Loading..."}
          subtext="Total Network Access Points Validated"
          icon={Network}
          color="purple"
          action={{ label: "View Details", onClick: () => router.push(`/${region}/broadband/naps`) }}
        />
        <StatCard
          title="Speedtests"
          value={averageSpeed.totalTests.toLocaleString() || "Loading..."}
          subtext="Total Speedtests Conducted"
          icon={Gauge}
          color="blue"
          action={{ label: "View Details", onClick: () => router.push(`/${region}/broadband/speedtests`) }}
        />
        <StatCard
          title="Fiber Coverage"
          value={`${fiberCoverage.percentage}%`}
          subtext={fiberCoverage.description}
          icon={Wifi}
          color="green"
        />
        <StatCard
          title="Mobile Coverage"
          value={`${mobileCoverage.percentage}%`}
          subtext={mobileCoverage.description}
          icon={Signal}
          color="orange"
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <DemographicsCard
          population={connectivityData?.data.locations[0]?.population ?? 0}
          households={connectivityData?.data.locations[0]?.householdCount ?? 0}
          density={density}
          area={connectivityData?.data.locations[0]?.areaSizeKm2.toLocaleString() ?? ""}
        />
        <ServiceProviderCard providers={cellSitesByProvider} />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <TopProvincesList provinces={topProvinces} />
        <CMTSScoresChart
          data={cmtsScores}
          totalScore={connectivityData?.data.summary.averageScore}
          connectivityStatus={connectivityData?.data.locations[0]?.connectivityStatus ?? ""}
        />
      </div>

      <AverageSpeedCard
        download={averageSpeed.download}
        upload={averageSpeed.upload}
        speedTestCount={averageSpeed.totalTests}
        meetsRequirements={averageSpeed.meetsRequirements}
        action={{ label: "View Details", onClick: () => router.push(`/${region}/broadband/speedtests`) }}
      />
    </div>
  );
}
