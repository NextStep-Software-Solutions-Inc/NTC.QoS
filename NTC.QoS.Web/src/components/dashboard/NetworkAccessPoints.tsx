"use client";

import React from "react";

import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import Each from "../Each";
import NetworkAccessPointItem from "./NetworkAccessPointItem";

const useProviders = createQueryHookFromTemplate(getServiceProvidersRequest);

const NetworkAccessPoints = () => {
  const { data: providers } = useProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceIds: ["019523bf-49d9-718f-a6a2-805da62c730e"] },
  });

  return (
    <div className="mb-3">
      <h4 className="mb-2 text-xs font-semibold text-gray-500 sticky top-0 bg-white z-10">Network Access Point</h4>
      <Each of={providers?.data ?? []} keyExtractor={(item) => item.id}>
        {(provider) => <NetworkAccessPointItem provider={provider} />}
      </Each>
    </div>
  );
};

export default NetworkAccessPoints;
