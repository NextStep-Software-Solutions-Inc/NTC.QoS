"use client";

import { getTechnologiesRequest } from "@/components/features/technology/handlers/getTechnologiesRequest";
import TechnologiesTable from "@/components/features/technology/tables/TechnologiesTable";
import columns from "@/components/features/technology/tables/TechnologiesTable/columns";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";

const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);

export default function ManageTechnologies() {
  const actions: MetaActions = ["write", "delete"];
  const { data: technologies } = useGetTechnologies({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <>
      <TechnologiesTable
        title="Technologies"
        description="List of technologies for the entire system."
        data={technologies?.data ?? []}
        actions={actions}
        readonly={false}
        columns={columns}
      />
    </>
  );
}
