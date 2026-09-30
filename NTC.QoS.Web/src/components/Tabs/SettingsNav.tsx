"use client";

import { useMemo } from "react";
import { usePathname } from "next/navigation";
import { Building2, Cpu, Link as LinkIcon, MapPin, Radio, Settings, Users, Zap, BarChart3 } from "lucide-react";

import { usePermissions } from "@/providers/PermissionsContext";
import TabsNav, { type TabItem } from "./TabsNav";

const baseTabs: TabItem[] = [
  {
    label: "Providers & Services",
    shortLabel: "Providers",
    value: "providers-and-services",
    icon: <Building2 className="h-3 w-3" />,
  },
  { label: "Generations", value: "generations", icon: <Zap className="h-3 w-3" /> },
  { label: "Technologies", shortLabel: "Tech", value: "technologies", icon: <Cpu className="h-3 w-3" /> },
  { label: "Bands", value: "bands", icon: <Radio className="h-3 w-3" /> },
  { label: "System Types", shortLabel: "Systems", value: "system-types", icon: <Settings className="h-3 w-3" /> },
  { label: "Area Study Types", shortLabel: "Studies", value: "area-studies", icon: <MapPin className="h-3 w-3" /> },
  { label: "Frequency Allocation", shortLabel: "Frequency", value: "links", icon: <LinkIcon className="h-3 w-3" /> },
  { label: "Barangay Coverage", shortLabel: "Coverage", value: "barangay-coverage", icon: <BarChart3 className="h-3 w-3" /> },
];

export default function SettingsNav() {
  const pathname = usePathname();
  const { hasPermission } = usePermissions();

  const tabs = useMemo(() => {
    const list = [...baseTabs];
    if (hasPermission("users.view")) {
      list.push({
        label: "Regional Personnel",
        shortLabel: "Personnel",
        value: "personnel",
        icon: <Users className="h-3 w-3" />,
      });
    }
    return list;
  }, [hasPermission]);

  // the current tab is determined by the next segment after /settings/ so you need to extract it and check if /settings/ is present in the pathname
  const currentTab =
    pathname.split("/").find((segment) => tabs.some((tab) => tab.value === segment)) ?? "providers-and-services";

  const getTabHref = (tab: string) => {
    if (pathname.includes("/settings")) {
      return `${tab}`;
    }
    return `settings`;
  };

  return <TabsNav tabs={tabs} currentTab={currentTab} getTabHref={getTabHref} className="p-2" />;
}
