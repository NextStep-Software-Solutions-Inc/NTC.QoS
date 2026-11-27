"use client";

import AreaStudiesTable from "@/components/features/area-study/tables/AreaStudiesTable";
import { getSignalLevelsRequest } from "@/components/features/area-study/handlers/getSignalLevelsRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";
import { getAreaStudiesRequest } from "./features/area-study/handlers/getAreaStudiesRequest";
import columns from "./features/area-study/tables/AreaStudiesTable/columns";

const useGetAreaStudies = createQueryHookFromTemplate(getAreaStudiesRequest);
const useGetSignalLevels = createQueryHookFromTemplate(getSignalLevelsRequest);

export default function ManageAreaStudies() {
  const actions: MetaActions = ["write", "delete"];
  const { data: areaStudies } = useGetAreaStudies({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });
  const { data: signalLevels } = useGetSignalLevels({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  const mappedData = (areaStudies?.data ?? []).map((item) => {
    const signalLevel = (signalLevels?.data ?? []).find((level) => level.areaStudyId === item.id);

    return {
      ...item,
      signalLevelId: signalLevel?.id,
    };
  });

  return (
    <div className="">
      <AreaStudiesTable
        title="Area Study Types"
        description="List of area study types for the entire system."
        data={mappedData}
        actions={actions}
        readonly={false} // Set to true if the table should be read-only
        columns={columns}
      />
    </div>
  );
}
