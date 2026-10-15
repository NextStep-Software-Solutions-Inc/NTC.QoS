"use client";

import React from "react";

import { getServicesRequest } from "@/components/features/service/handlers/getServicesRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import Each from "../Each";
import InternetCoverageItem from "./InternetCoverageItem";

const useServices = createQueryHookFromTemplate(getServicesRequest);

const InternetCoverages = () => {
  const { data: services } = useServices({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <div className="mb-3">
      <h4 className="mb-2 text-xs font-semibold text-gray-500 sticky top-0 bg-white z-10">Internet Coverage</h4>
      <Each of={services?.data ?? []} keyExtractor={(item) => item.id}>
        {(service) => <InternetCoverageItem service={service} />}
      </Each>
    </div>
  );
};

export default InternetCoverages;
