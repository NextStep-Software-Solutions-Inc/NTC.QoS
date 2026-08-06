"use client";

import AreaStudiesTable from "@/components/features/area-study/tables/AreaStudiesTable";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";
import { getAreaStudiesRequest } from "./features/area-study/handlers/getAreaStudiesRequest";
import columns from "./features/area-study/tables/AreaStudiesTable/columns";

const useGetAreaStudies = createQueryHookFromTemplate(getAreaStudiesRequest);

export default function ManageAreaStudies() {
  const actions: MetaActions = ["write", "delete"];
  const { data: areaStudies } = useGetAreaStudies({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <div className="">
      <AreaStudiesTable
        title="Area Study Types"
        description="List of area study types for the entire system."
        data={areaStudies?.data ?? []}
        actions={actions}
        readonly={false} // Set to true if the table should be read-only
        columns={columns}
      />
    </div>
  );
}
