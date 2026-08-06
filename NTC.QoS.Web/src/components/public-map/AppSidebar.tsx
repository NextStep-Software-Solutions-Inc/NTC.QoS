import * as React from "react";
import Link from "next/link";

import LoadInternetServices from "@/components/LoadInternetServices";
import SideBarMenuIcons from "@/components/public-map/SideBarMenuIconsOnly";
import { WirelessList } from "@/components/public-map/WirelessList";
import { QoSLogo } from "@/components/sections/IconsImages";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import PublicSearchPHLocations from "../Map/PublicSearchPHLocations";

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar variant="inset" {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              size="lg"
              className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground">
              <div className="flex aspect-square size-9 items-center justify-center rounded-lg text-sidebar-primary-foreground">
                <QoSLogo className="w-full" />
              </div>
              <Link href="/" className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-semibold">QoS & Broadband Map</span>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        <PublicSearchPHLocations />
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
          <SidebarGroupLabel className="text-nowrap">Coverage Type</SidebarGroupLabel>
          <WirelessList />
        </SidebarGroup>
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
          <LoadInternetServices />
        </SidebarGroup>
        <SidebarGroup className="hidden group-data-[collapsible=icon]:block">
          <SideBarMenuIcons />
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  );
}
