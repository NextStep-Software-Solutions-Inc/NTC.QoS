"use client";

import React, { useCallback, useEffect, useMemo } from "react";
import { useSearchParams } from "next/navigation";
import { Radiation } from "lucide-react";

import { Switch } from "@/components/ui/switch";
import { SidebarMenuButton } from "../ui/sidebar";

interface WirelessServiceProps {
  service: { name: string };
}

export default function WirelessService({ service }: WirelessServiceProps) {
  const searchParams = useSearchParams();
  const serviceName = searchParams.get(service.name.toLowerCase());

  const active = useMemo(() => serviceName === null || serviceName === "true", [serviceName]);

  const handleEnable = useCallback(
    (status: boolean) => {
      const searchParams = new URLSearchParams(window.location.search);
      searchParams.set(service.name.toLowerCase(), String(status));
      window.history.replaceState({}, "", `${window.location.pathname}?${searchParams}`);
    },
    [service.name],
  );

  useEffect(() => {
    handleEnable(active);
  }, [active, handleEnable]);

  return (
    <SidebarMenuButton tooltip={service.name} asChild>
      <div>
        <div>
          <Radiation size={28} className="bg-primary rounded-full text-white p-2" />
        </div>
        <div className="flex items-center justify-between w-full">
          <span className="">{service.name}</span>
          <Switch checked={active} onCheckedChange={handleEnable} />
        </div>
      </div>
    </SidebarMenuButton>
  );
}
