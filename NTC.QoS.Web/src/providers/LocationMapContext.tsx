"use client";

import React, { createContext, useContext, useMemo } from "react";
import { useParams } from "next/navigation";

import { getBarangaysRequest } from "@/components/features/barangay/handlers/getBarangaysRequest";
import { getMunicipalitiesRequest } from "@/components/features/municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "@/components/features/province/handlers/getProvincesRequest";
import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

// Define the shape of the context
interface LocationMapContextProps {
  resolveUuid: (uuid: string) => string | undefined;
}

// Create the context
const LocationMapContext = createContext<LocationMapContextProps | undefined>(undefined);

const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
const useBarangays = createQueryHookFromTemplate(getBarangaysRequest);

// Provider component
export const LocationMapProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { region, province, municipal } = useParams<{ region: string; province: string; municipal: string }>();

  const { data: regions } = useRegions({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });
  const { data: provinces } = useProvinces({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, regionIds: region ? [region] : [] },
  });
  const { data: municipalities } = useMunicipalities({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, provinceId: province },
  });
  const { data: barangays } = useBarangays({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, ...(municipal ? { municipalityIds: [municipal] } : {}) },
  });

  // Combine all locations into a single map
  const locationMap = useMemo(() => {
    const map = new Map<string, string>();
    regions?.data.forEach((region) => {
      map.set(region.id, region.name);
    });
    provinces?.data.forEach((province) => {
      map.set(province.id, province.name);
    });
    municipalities?.data.forEach((municipality) => {
      map.set(municipality.id, municipality.name);
    });
    barangays?.data.forEach((barangay) => {
      map.set(barangay.id, barangay.name);
    });
    return map;
  }, [regions, provinces, municipalities, barangays]);

  const resolveUuid = (uuid: string) => locationMap.get(uuid);

  return <LocationMapContext.Provider value={{ resolveUuid }}>{children}</LocationMapContext.Provider>;
};

// Hook to use the context
export const useLocationMap = () => {
  const context = useContext(LocationMapContext);
  if (!context) {
    throw new Error("useLocationMap must be used within a LocationMapProvider");
  }
  return context;
};
// ...existing code...
