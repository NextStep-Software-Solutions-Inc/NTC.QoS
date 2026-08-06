"use client";

import { getServicesRequest } from "@/components/features/service/handlers/getServicesRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import Each from "../Each";
import { SidebarMenu, SidebarMenuItem } from "../ui/sidebar";
import WirelessProvider from "./WirelessProvider";

const useServices = createQueryHookFromTemplate(getServicesRequest);

export const WirelessList = () => {
  const { data: wireless } = useServices({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <SidebarMenu>
      <Each of={wireless?.data ?? []} keyExtractor={(item) => item.id}>
        {(service) => {
          return (
            <SidebarMenuItem>
              <WirelessProvider key={service.id} service={{ name: service.name ?? "Unknown Service" }} />
            </SidebarMenuItem>
          );
        }}
      </Each>
    </SidebarMenu>
  );
};
