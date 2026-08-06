"use client";

import React, { useCallback } from "react";
import { useParams } from "next/navigation";
import { ArrowUpDown, RadioTower, Workflow } from "lucide-react";

import AdminRouteToolbar from "@/components/AdminRouteToolbar";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent } from "@/components/ui/tabs";
import TopThreeProviders from "@/components/ui/TopThreeProviders";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { getCellsRequest } from "../../cell/handlers/getCellsRequest";
import { getNAPsRequest } from "../../NAP/handlers/getNAPsRequest";
import { getSpeedtestsRequest } from "../../speedtest/handlers/getSpeedtestsRequest";
import { getBarangaysRequest } from "../handlers/getBarangaysRequest";

const useCell = createQueryHookFromTemplate(getCellsRequest);
const useSpeedtest = createQueryHookFromTemplate(getSpeedtestsRequest);
const useNAPs = createQueryHookFromTemplate(getNAPsRequest);
const useBaranggays = createQueryHookFromTemplate(getBarangaysRequest);

const ToolbarBarangays = () => {
  const { search, setSearch } = useDebounceContext();

  const { region, province, table, municipal } = useParams<{
    region: string;
    province: string;
    table: string;
    municipal: string;
  }>();

  const { data: cells } = useCell({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: [municipal],
    },
  });
  const { data: speedtests } = useSpeedtest({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: [municipal],
    },
  });
  const { data: naps } = useNAPs({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: [municipal],
    },
  });

  const { data: barangays } = useBaranggays({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, municipalityIds: [municipal] },
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
      // Get only the first word of the provider name
      const firstWord = name.split(" ")[0];
      return (
        <Badge key={name} style={{ backgroundColor: color }}>
          {`${firstWord}`}
        </Badge>
      );
    });
  }, [naps]);

  return (
    <Tabs value={table} className="w-full">
      <TabsContent value="cell">
        <AdminRouteToolbar
          boundaryType="Total Barangays"
          totalPlace={barangays?.data?.length}
          installedName={" Total Cell Sites"}
          installedIcon={<RadioTower className="w-4 h-4 text-muted-foreground" />}
          totalInstalled={cells?.data?.length ?? "0"}
          topISPs={<>{<TopThreeProviders data={cells?.data} />}</>}
          searchValue={search}
          placeholder="Search Barangays"
          setSearchValue={setSearch}
        />
      </TabsContent>

      <TabsContent value="speedtest">
        <AdminRouteToolbar
          boundaryType="Total Barangays"
          totalPlace={barangays?.data?.length}
          installedName={" Total Speed Tests in area"}
          installedIcon={<ArrowUpDown className="w-4 h-4 text-muted-foreground" />}
          totalInstalled={speedtests?.data?.length ?? "0"}
          topISPs={<>{<TopThreeProviders data={speedtests?.data} />}</>}
          searchValue={search}
          placeholder="Search Barangays"
          setSearchValue={setSearch}
        />
      </TabsContent>

      <TabsContent value="nap">
        <AdminRouteToolbar
          boundaryType="Total Barangays"
          totalPlace={barangays?.data?.length}
          installedName={"Total Network Access Points in area"}
          installedIcon={<Workflow className="w-4 h-4 text-muted-foreground" />}
          totalInstalled={naps?.data?.length ?? "0"}
          topISPs={<>{getTop3ProvidersNap()}</>}
          searchValue={search}
          placeholder="Search Barangays"
          setSearchValue={setSearch}
        />
      </TabsContent>
    </Tabs>
  );
};

export default ToolbarBarangays;
