"use client";

import { getServicesRequest } from "@/components/features/service/handlers/getServicesRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import Each from "../Each";
import { SidebarMenu, SidebarMenuItem } from "../ui/sidebar";
import InternetServiceDetails from "./InternetServiceDetails";
import InternetServiceType from "./InternetServiceType";

const useServices = createQueryHookFromTemplate(getServicesRequest);

interface InternetServicesProps {
  lat: number;
  lng: number;
}

export default function InternetServices({ lat, lng }: InternetServicesProps) {
  const { data: services } = useServices({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <SidebarMenu>
      <Each of={services?.data ?? []} keyExtractor={(item) => item.id}>
        {(service) => (
          <SidebarMenuItem>
            <InternetServiceType key={service?.id} service={service}>
              <InternetServiceDetails service={service ?? []} lat={lat} lng={lng} />
            </InternetServiceType>
          </SidebarMenuItem>
        )}
      </Each>
    </SidebarMenu>
  );
}
