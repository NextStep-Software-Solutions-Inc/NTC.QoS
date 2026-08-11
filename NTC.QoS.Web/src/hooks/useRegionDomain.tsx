"use client";

import { createContext, useContext, useState } from "react";

import { type BarangayDetails } from "@/components/features/barangay/types/Barangay";
import { getProvincesRequest } from "@/components/features/province/handlers/getProvincesRequest";
import { type Province } from "@/components/features/province/types/Province";
import { getRegionsDetailsRequest } from "@/components/features/region/handlers/getRegionDetailsRequest";
import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { type Region } from "@/components/features/region/types/Region";
import { createQueryHookFromTemplate } from "./createQueryHookFromTemplate";

interface RegionDomainContextProps {
  regionDetails: Region;
  provinces?: Province[];
  selectedRegion?: string;
  setSelectedRegion?: (region: string) => void;
  clickedBarangay: BarangayDetails | null;
  setClickedBarangay: (barangay: BarangayDetails | null) => void;
  isBarangayListOpen?: boolean;
  setIsBarangayListOpen?: (open: boolean) => void;
  municipalId: string;
  setMunicipalId: (id: string) => void;
}

const RegionDomainContext = createContext<RegionDomainContextProps | undefined>(undefined);

const useRegion = createQueryHookFromTemplate(getRegionsRequest);
const useRegionDetails = createQueryHookFromTemplate(getRegionsDetailsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);

export const RegionDomainProvider = ({ children }: { children: React.ReactNode }) => {
  const [municipalId, setMunicipalId] = useState<string>("");
  const [clickedBarangay, setClickedBarangay] = useState<BarangayDetails | null>(null);
  const [isBarangayListOpen, setIsBarangayListOpen] = useState<boolean>(false);

  const { data: regions } = useRegion({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 100 } });
  const regionData = regions?.data[16];

  const { data: regionDetails } = useRegionDetails({
    pathParams: { id: regionData?.id ?? "" },
    queryParams: { pageNumber: 1, pageSize: 100 },
  });

  const { data: provinces } = useProvinces({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <RegionDomainContext.Provider
      value={{
        regionDetails: regionDetails?.data ?? ({} as Region),
        provinces: provinces?.data ?? [],
        clickedBarangay,
        setClickedBarangay,
        isBarangayListOpen,
        setIsBarangayListOpen,
        municipalId,
        setMunicipalId,
      }}>
      {children}
    </RegionDomainContext.Provider>
  );
};

export const useRegionDomain = () => {
  const context = useContext(RegionDomainContext);
  if (!context) {
    throw new Error("useRegionDomain must be used within a RegionDomainProvider");
  }
  return context;
};
