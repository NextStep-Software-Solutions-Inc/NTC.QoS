"use client";

import { useParams } from "next/navigation";

import { ProvinceTable } from "@/components/features/province/tables/ProvinceTable/table";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";
import { getProvincesRequest } from "./features/province/handlers/getProvincesRequest";
import columns from "./features/province/tables/ProvinceTable/columns";
import { type Province } from "./features/province/types/Province";
import { type ProvinceColumn } from "./features/province/types/ProvinceColumn";

const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
export default function ProvinceManagement() {
  const actions: MetaActions = ["write", "delete"];
  const { region } = useParams<{ region: string }>();
  const { data: provinces } = useProvinces({
    pathParams: {},
    queryParams: { pageSize: 10, pageNumber: 1, regionIds: region ? [region] : [] },
  });

  // Transform Province data to ProvinceColumn format
  const transformedData: ProvinceColumn[] = (provinces?.data ?? []).map((province: Province) => ({
    id: province.id,
    name: province.name,
    municipalityCount: province.municipalityCount ?? 0,
    barangayCount: 0, // This would need to come from API if available
    ispCount: 0, // This would need to come from API if available
  }));

  return (
    <div className="flex flex-col flex-1 min-h-0 overflow-hidden px-5">
      <ProvinceTable
        actions={actions}
        readonly={false} // Set to true if the table should be read-only
        title="Provinces"
        description="List of provinces with their municipalities, barangays, and ISPs."
        data={transformedData}
        columns={columns}
      />
    </div>
  );
}
