"use client";

import { getSystemTypesRequest } from "@/components/features/system-type/handlers/getSystemTypesRequest";
import SystemTypesTable from "@/components/features/system-type/tables/SystemTypesTable";
import columns from "@/components/features/system-type/tables/SystemTypesTable/columns";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";

const useGetSystemTypes = createQueryHookFromTemplate(getSystemTypesRequest);

export default function ManageSystemTypes() {
  const actions: MetaActions = ["write", "delete"];
  const { data: systemTypes } = useGetSystemTypes({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <>
      <SystemTypesTable
        columns={columns}
        title="System Types"
        description="List of system types for the entire system."
        data={systemTypes?.data ?? []}
        actions={actions}
        readonly={false}
      />
    </>
  );
}
