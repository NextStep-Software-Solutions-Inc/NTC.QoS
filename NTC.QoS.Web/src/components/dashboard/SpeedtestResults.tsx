"use client";

import React from "react";

import { getServicesRequest } from "@/components/features/service/handlers/getServicesRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import Each from "../Each";
import SpeedtestResultItem from "./SpeedtestResultItem";

const useServices = createQueryHookFromTemplate(getServicesRequest);

export default function SpeedtestResults() {
  const { data: services } = useServices({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <div className="mb-3">
      <h4 className="mb-2 text-xs font-semibold text-gray-500 sticky top-0 z-10 bg-white">Speedtest Results</h4>
      <Each of={services?.data ?? []} keyExtractor={(item) => item.id}>
        {(service) => <SpeedtestResultItem service={service} />}
      </Each>
    </div>
  );
}
