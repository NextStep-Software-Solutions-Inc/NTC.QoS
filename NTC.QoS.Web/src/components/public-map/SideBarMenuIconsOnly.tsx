"use client";

import React from "react";
import { Search, Settings2 } from "lucide-react";

import { cn } from "@/lib/utils";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from "../ui/sidebar";

export default function SideBarMenuIconsOnly() {
  const { open, setOpen } = useSidebar();

  return (
    <SidebarMenu className="mx-auto">
      <SidebarMenuItem>
        <SidebarMenuButton>
          <Search onClick={() => setOpen(!open)} className={cn("w-[18px]")} />
        </SidebarMenuButton>
      </SidebarMenuItem>
      <SidebarMenuItem>
        <SidebarMenuButton>
          <Settings2 onClick={() => setOpen(!open)} className={cn("w-[18px]")} />
        </SidebarMenuButton>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}
