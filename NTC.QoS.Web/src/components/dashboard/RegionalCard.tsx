"use client";

import React, { useMemo } from "react";
import { useParams } from "next/navigation";
import { ArrowUpDown, RadioTower, Rss, Wifi, type LucideProps } from "lucide-react";
import { type DateRange } from "react-day-picker";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { formatNumber } from "@/lib/format-number";
import { cn } from "@/lib/utils";
import AnimatedStat from "../AnimatedStat";
import { getDashboardAllRequest } from "../features/dashboard/handlers/getDashboardAllRequest";
import { Card } from "../ui/card";
import { Skeleton } from "../ui/skeleton";
import WithPlaceholder from "../WithPlaceholder";

type DashboardTileType = {
  label: string;
  value: string | undefined;
  details: string;
  icon: React.ForwardRefExoticComponent<Omit<LucideProps, "ref"> & React.RefAttributes<SVGSVGElement>>;
};

const useDashboardAll = createQueryHookFromTemplate(getDashboardAllRequest);

interface RegionalCardsProps {
  dateRange?: DateRange;
}

const RegionalCards: React.FC<RegionalCardsProps> = ({ dateRange }) => {
  const startDate = dateRange?.from?.toISOString();
  const endDate = dateRange?.to?.toISOString();
  const { region } = useParams<{ region: string }>();

  const { data: all } = useDashboardAll({
    pathParams: {},
    queryParams: { startDate: startDate ?? "", endDate: endDate ?? "", regionId: region },
  });

  const dashboardTileType: DashboardTileType[] = useMemo(
    () => [
      {
        label: "Total Cell Sites",
        value: all?.totalCellsitesCount?.data?.totalCells
          ? formatNumber(all.totalCellsitesCount.data.totalCells)
          : undefined,
        details: `${all?.totalCellsitesCount?.data?.verifiedCells ? formatNumber(all.totalCellsitesCount.data.verifiedCells) : 0} Verified, ${all?.totalCellsitesCount?.data?.unverifiedCells ? formatNumber(all.totalCellsitesCount.data.unverifiedCells) : 0} Unverified`,
        icon: RadioTower,
      },
      {
        label: "Network Access Points",
        value: all?.totalNAPsCount?.data ? formatNumber(all.totalNAPsCount.data) : undefined,
        details: "Total Network Access Points Validateds",
        icon: ArrowUpDown,
      },
      { label: "Fiber Coverage", value: "43%", details: "High-speed internet available in major cities.", icon: Rss },
      { label: "Wireless Coverage", value: "70.23%", details: "LTE/5G connectivity in most areas.", icon: Wifi },
    ],
    [all],
  );

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-2 2xl:grid-cols-4 mx-5 gap-4">
      {dashboardTileType.map((item, index) => {
        const Icon = item.icon;
        return (
          <Card
            key={index}
            className={cn(
              `max-h-max min-w-[300px] cursor-default p-3 shadow-sm space-y-2 rounded-md border border-solid border-White/20 px-4 sm:px-5 md:px-6 py-4 w-full h-full flex flex-col justify-between`,
            )}>
            <section className="flex items-center justify-between">
              <p className="text-[13px] font-medium">{item.label}</p>
              <Icon size={18} className="text-gray-500" />
            </section>
            <section>
              <WithPlaceholder isLoading={!item.value} placeholder={<Skeleton className="h-6 w-1/4" />}>
                <p className="pt-1 text-xl font-bold">
                  <AnimatedStat end={item.value?.toString() ?? "0"} start={0} />
                </p>
              </WithPlaceholder>
              <p className="mt-[.5rem] text-sm text-gray-500 truncate">{item.details}</p>
            </section>
          </Card>
        );
      })}
    </div>
  );
};

export default RegionalCards;
