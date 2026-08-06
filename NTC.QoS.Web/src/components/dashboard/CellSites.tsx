"use client";

import React from "react";

import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import Each from "../Each";
import CellSiteItem from "./CellSiteItem";

const useProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
export default function CellSites() {
  // const handler = useMemo(() => getProvidersHandler("?serviceIds=0195d54d-75c0-7c7e-b7b4-88f208388d17"), []);
  // const { data: providers } = useCreateQuery(handler);
  const { data: providers } = useProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceIds: ["0195d54d-75c0-7c7e-b7b4-88f208388d17"] },
  });

  return (
    <div className="mb-3 space-y-2">
      <h4 className="mb-2 text-xs font-semibold sticky top-0 text-gray-500 bg-white z-10">Cell Sites</h4>
      <Each of={providers?.data ?? []} keyExtractor={(item) => item.id}>
        {(provider) => <CellSiteItem provider={provider} />}
      </Each>
    </div>
  );
}
