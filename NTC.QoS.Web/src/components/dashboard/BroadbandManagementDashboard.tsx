"use client";
"use client";

import React from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { Activity, ArrowUpDown, BarChart3, RadioTower } from "lucide-react";
import { type DateRange } from "react-day-picker";

import { getDashboardAllRequest } from "@/components/features/dashboard/handlers/getDashboardAllRequest";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { formatNumber } from "@/lib/format-number";

const useDashboardAll = createQueryHookFromTemplate(getDashboardAllRequest);

interface BroadbandManagementDashboardProps {
  dateRange?: DateRange;
}

const BroadbandManagementDashboard: React.FC<BroadbandManagementDashboardProps> = ({ dateRange }) => {
  const startDate = dateRange?.from?.toISOString();
  const endDate = dateRange?.to?.toISOString();
  const { region } = useParams<{ region: string }>();
  const _router = null; // Removed unused router

  const { data: all } = useDashboardAll({
    pathParams: {},
    queryParams: { startDate: startDate ?? "", endDate: endDate ?? "", regionId: region },
  });

  const broadbandStats = [
    {
      title: "Network Access Points",
      value: all?.totalNAPsCount?.data ? formatNumber(all.totalNAPsCount.data) : "0",
      description: "Total NAPs validated in the region",
      icon: ArrowUpDown,
      action: "Manage NAPs",
      href: `/${region}/broadband/naps`,
    },
    {
      title: "Cell Sites",
      value: all?.totalCellsitesCount?.data?.totalCells ? formatNumber(all.totalCellsitesCount.data.totalCells) : "0",
      description: `${all?.totalCellsitesCount?.data?.verifiedCells || 0} verified, ${all?.totalCellsitesCount?.data?.unverifiedCells || 0} unverified`,
      icon: RadioTower,
      action: "Manage Cells",
      href: `/${region}/broadband/cells`,
    },
    {
      title: "Speedtest Results",
      value: "Loading...", // TODO: Add speedtest count query
      description: "Performance measurements collected",
      icon: Activity,
      action: "View Speedtests",
      href: `/${region}/broadband/speedtests`,
    },
  ];

  return (
    <div className="space-y-6">
      <div className="mx-5">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Broadband Management</h1>
        <p className="text-gray-600">
          Manage network infrastructure, cell sites, and performance monitoring for NTC broadband services.
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mx-5">
        {broadbandStats.map((stat, index) => (
          <Card key={index} className="hover:shadow-md transition-shadow">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">{stat.title}</CardTitle>
              <stat.icon className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground">{stat.description}</p>
              <Button variant="outline" size="sm" className="mt-3 w-full" asChild>
                <Link href={stat.href}>{stat.action}</Link>
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="mx-5">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BarChart3 className="h-5 w-5" />
              Quick Actions
            </CardTitle>
            <CardDescription>Common broadband management tasks</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              <Button variant="outline" asChild>
                <Link href={`/${region}/broadband/naps`}>Add New NAP</Link>
              </Button>
              <Button variant="outline" asChild>
                <Link href={`/${region}/broadband/cells`}>Register Cell Site</Link>
              </Button>
              <Button variant="outline" asChild>
                <Link href={`/${region}/broadband/speedtests`}>View Performance</Link>
              </Button>
              <Button variant="outline" asChild>
                <Link href={`/${region}/reports`}>Generate Reports</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default BroadbandManagementDashboard;
