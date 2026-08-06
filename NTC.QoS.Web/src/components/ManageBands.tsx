"use client";

import { getBandsRequest } from "@/components/features/band/handlers/getBandsRequest";
import columns from "@/components/features/band/tables/BandsTable/columns";
import BandsTable from "@/components/features/band/tables/BandsTable/table";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";

const useGetBands = createQueryHookFromTemplate(getBandsRequest);

export default function ManageBands() {
  const actions: MetaActions = ["write", "delete"];
  const { data: bands } = useGetBands({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <>
      <BandsTable
        columns={columns}
        title="Bands"
        description="List of bands for the entire system."
        data={bands?.data ?? []}
        actions={actions}
        readonly={false}
      />
    </>
  );
}
