import React from "react";
import { useParams } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useRegionDomain } from "@/hooks/useRegionDomain";
import getHumanReadableString from "@/lib/getHumanReadableString";
import { getMunicipalitiesRequest } from "../features/municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "../features/province/handlers/getProvincesRequest";
import { Card, CardContent } from "../ui/card";

const useProvince = createQueryHookFromTemplate(getProvincesRequest);
const useGetMunicipal = createQueryHookFromTemplate(getMunicipalitiesRequest);

const Municipal = () => {
  const { province } = useParams<{ province: string }>();
  const { municipalId } = useRegionDomain();

  const { data: provinces } = useProvince({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });
  const provinceId = provinces?.data.find(
    (prov) => getHumanReadableString(prov.name) === getHumanReadableString(province),
  )?.id;

  const { data: municipals } = useGetMunicipal({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, provinceId },
  });
  const municipal = municipals?.data.find((mun) => mun.id === municipalId);

  // Fetch barangays based on the selected municipal

  return (
    <div>
      <Card className="w-full h-full p-4">
        <h1 className="text-2xl font-semibold">{municipal?.name} Table</h1>
        <p className="text-gray-500 mt-2">This is the municipal table where you can view and manage municipal data.</p>
        <CardContent className="mt-4">
          {/* <h1>
              {(filteredBarangays?.length ?? 0)} {(filteredBarangays?.length ?? 0) > 1 ? "Barangays" : "Barangay"}
            </h1> */}
        </CardContent>
      </Card>
    </div>
  );
};

export default Municipal;
