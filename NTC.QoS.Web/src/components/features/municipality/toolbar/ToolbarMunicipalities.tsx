"use client";

import React, { useCallback } from "react";
import { useParams } from "next/navigation";
import { RadioTower } from "lucide-react";

import AdminRouteToolbar from "@/components/AdminRouteToolbar";
import AdminRouteToolbarSkeleton from "@/components/AdminRouteToolbarSkeleton";
import AnimatedStat from "@/components/AnimatedStat";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent } from "@/components/ui/tabs";
import TopThreeProviders from "@/components/ui/TopThreeProviders";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { getCellsRequest } from "../../cell/handlers/getCellsRequest";
import { getNAPsRequest } from "../../NAP/handlers/getNAPsRequest";
import { getSpeedtestsRequest } from "../../speedtest/handlers/getSpeedtestsRequest";
import { getMunicipalitiesRequest } from "../handlers/getMunicipalitiesRequest";

const useCell = createQueryHookFromTemplate(getCellsRequest);
const useSpeedtest = createQueryHookFromTemplate(getSpeedtestsRequest);
const useNAPs = createQueryHookFromTemplate(getNAPsRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
const ANIMATED_STAT_DURATION = 2000;
const ToolbarMunicipalities = () => {
  const { region, province, table } = useParams<{ region: string; province: string; table: string }>();
  const { search, setSearch } = useDebounceContext();

  const { data: cells, isLoading: isCellsLoading } = useCell({
    pathParams: {},
    queryParams: { pageSize: 100000, pageNumber: 1, regionIds: [region], provinceIds: [province] },
  });

  const { data: speedtests, isLoading: isSpeedtestsLoading } = useSpeedtest({
    pathParams: {},
    queryParams: { pageSize: 100000, pageNumber: 1, regionIds: [region], provinceIds: [province] },
  });

  const { data: naps, isLoading: isNapsLoading } = useNAPs({
    pathParams: {},
    queryParams: { pageSize: 100000, pageNumber: 1, regionIds: [region], provinceIds: [province] },
  });

  const { data: municipalities, isLoading: isMunicipalitiesLoading } = useMunicipalities({
    pathParams: {},
    queryParams: { pageSize: 100000, pageNumber: 1, provinceId: province },
  });

  const getTop3ProvidersNap = useCallback(() => {
    if (!naps?.data || naps.data.length === 0) {
      return <p className="text-sm">No ISP available</p>;
    }

    const providerCountMap = naps.data.reduce(
      (acc, nap) => {
        const name = nap.ISP;
        const color = nap.ISPColor;
        acc[name] ??= { count: 0, color };
        acc[name].count += 1;
        return acc;
      },
      {} as Record<string, { count: number; color: string }>,
    );

    const top3 = Object.entries(providerCountMap)
      .sort((a, b) => b[1].count - a[1].count)
      .slice(0, 3);

    return top3.map(([name, { color }]) => {
      const firstWord = name.split(" ")[0];
      return (
        <Badge key={name} style={{ backgroundColor: color }}>
          {firstWord}
        </Badge>
      );
    });
  }, [naps]);

  const showSkeleton = isCellsLoading || isSpeedtestsLoading || isNapsLoading || isMunicipalitiesLoading;

  return (
    <Tabs value={table} className="w-full">
      <TabsContent value="cell">
        {showSkeleton ? (
          <AdminRouteToolbarSkeleton />
        ) : (
          <AdminRouteToolbar
            boundaryType="Total Municipality"
            totalPlace={
              <AnimatedStat
                start={0}
                end={municipalities?.data?.length.toString() ?? "0"}
                duration={ANIMATED_STAT_DURATION}
              />
            }
            installedName="Total Cell Sites"
            installedIcon={<RadioTower className="w-4 h-4 text-muted-foreground" />}
            totalInstalled={
              <AnimatedStat start={0} end={cells?.data?.length.toString() ?? "0"} duration={ANIMATED_STAT_DURATION} />
            }
            topISPs={<TopThreeProviders data={cells?.data} />}
            searchValue={search}
            placeholder="Search Municipalities"
            setSearchValue={setSearch}
          />
        )}
      </TabsContent>

      <TabsContent value="speedtest">
        {showSkeleton ? (
          <AdminRouteToolbarSkeleton />
        ) : (
          <AdminRouteToolbar
            boundaryType="Total Municipality"
            totalPlace={
              <AnimatedStat
                start={0}
                end={municipalities?.data?.length.toString() ?? "0"}
                duration={ANIMATED_STAT_DURATION}
              />
            }
            installedName="Total Speed Tests in area"
            installedIcon={<RadioTower className="w-4 h-4 text-muted-foreground" />}
            totalInstalled={
              <AnimatedStat
                start={0}
                end={speedtests?.data?.length.toString() ?? "0"}
                duration={ANIMATED_STAT_DURATION}
              />
            }
            topISPs={<TopThreeProviders data={speedtests?.data} />}
            searchValue={search}
            placeholder="Search Municipalities"
            setSearchValue={setSearch}
          />
        )}
      </TabsContent>

      <TabsContent value="nap">
        {showSkeleton ? (
          <AdminRouteToolbarSkeleton />
        ) : (
          <AdminRouteToolbar
            boundaryType="Total Municipality"
            totalPlace={
              <AnimatedStat
                start={0}
                end={municipalities?.data?.length.toString() ?? "0"}
                duration={ANIMATED_STAT_DURATION}
              />
            }
            installedName="Total Network Access Points in area"
            installedIcon={<RadioTower className="w-4 h-4 text-muted-foreground" />}
            totalInstalled={
              <AnimatedStat start={0} end={naps?.data?.length.toString() ?? "0"} duration={ANIMATED_STAT_DURATION} />
            }
            topISPs={getTop3ProvidersNap()}
            searchValue={search}
            placeholder="Search Municipalities"
            setSearchValue={setSearch}
          />
        )}
      </TabsContent>
    </Tabs>
  );
};

export default ToolbarMunicipalities;
