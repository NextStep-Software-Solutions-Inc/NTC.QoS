"use client";

import * as React from "react";
import Link from "next/link";
import { useAuth } from "@clerk/nextjs";
import { ChevronsUpDown, GalleryVerticalEnd, Plus } from "lucide-react";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from "@/components/ui/sidebar";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useLocationMap } from "@/providers/LocationMapContext";
import { getUserRegionsRequest } from "../features/user-region/handlers/getUserRegionsRequest";

const useUserRegions = createQueryHookFromTemplate(getUserRegionsRequest);

export default function RegionSwitcher() {
  const { isMobile } = useSidebar();
  const { userId } = useAuth();
  const { data: userRegions } = useUserRegions({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, authUserIds: userId ? [userId] : [], statuses: ["Active"] },
  });
  const { resolveUuid } = useLocationMap();
  const data = {
    current: {
      name: "shadcn",
      email: "m@example.com",
      avatar: "/avatars/shadcn.jpg",
      logo: GalleryVerticalEnd,
      plan: "Enterprise",
    },
    regions: (userRegions?.data ?? []).map((userRegion) => ({
      id: userRegion.region.id,
      name: userRegion.region.name,
      logo: GalleryVerticalEnd,
    })),
  };

  const [activeRegion, setActiveRegion] = React.useState(data.regions[0]);

  React.useEffect(() => {
    if (!activeRegion) {
      setActiveRegion(data.regions[0]);
    }
  }, [activeRegion, data.regions]);

  if (!activeRegion) {
    return null;
  }

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton
              size="lg"
              className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground">
              <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                <activeRegion.logo className="size-4" />
              </div>
              <div className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-semibold">{resolveUuid(activeRegion.id)}</span>
                <span className="truncate text-xs">{activeRegion.id}</span>
              </div>
              <ChevronsUpDown className="ml-auto" />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="w-[--radix-dropdown-menu-trigger-width] min-w-56 rounded-lg"
            align="start"
            side={isMobile ? "bottom" : "right"}
            sideOffset={4}>
            <DropdownMenuLabel className="text-xs text-muted-foreground">Regions</DropdownMenuLabel>
            {data.regions.map((region, index) => {
              return (
                <DropdownMenuItem asChild key={index} onClick={() => setActiveRegion(region)} className="gap-2 p-2">
                  <Link href={`/${region.id}/dashboard`}>
                    <div className="flex size-6 items-center justify-center rounded-sm border">
                      <region.logo className="size-4 shrink-0" />
                    </div>
                    {region.name}
                  </Link>
                </DropdownMenuItem>
              );
            })}
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild className="gap-2 p-2">
              <Link href="/select-region">
                <div className="flex size-6 items-center justify-center rounded-md border bg-background">
                  <Plus className="size-4" />
                </div>
                <div className="font-medium text-muted-foreground">Add region</div>
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}
