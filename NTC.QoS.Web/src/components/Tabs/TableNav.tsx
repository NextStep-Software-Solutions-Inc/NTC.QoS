"use client";

import { useParams } from "next/navigation";
import { Gauge, Router, Smartphone } from "lucide-react";

import TabsNav, { type TabItem } from "./TabsNav";

const tabs: TabItem[] = [
  { label: "Cell", value: "cell", icon: <Smartphone className="h-3 w-3" /> },
  { label: "Speedtest", value: "speedtest", icon: <Gauge className="h-3 w-3" /> },
  { label: "NAP", value: "nap", icon: <Router className="h-3 w-3" /> },
  // { label: "Cellular Coverage", value: "cellular-coverage" }
];

const TableNav = () => {
  const { region, table, province, municipal, brgy } = useParams<{
    region: string;
    table: string;
    province: string;
    municipal: string;
    brgy: string;
  }>();
  const currentTab = table || "cell";

  const getTabHref = (tab: string) => {
    const searchParams = new URLSearchParams(window.location.search);
    if (region) {
      return `/${region}/${tab}${province ? `/${province}` : ""}${municipal ? `/${municipal}` : ""}${brgy ? `/${brgy}` : ""}${searchParams.toString() ? `?${searchParams.toString()}` : ""}`;
    }
    return `/${tab}${searchParams.toString() ? `?${searchParams.toString()}` : ""}`;
  };

  return <TabsNav tabs={tabs} currentTab={currentTab} getTabHref={getTabHref} className="your-custom-class" />;
};

export default TableNav;
