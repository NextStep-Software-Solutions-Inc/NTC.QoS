"use client";

import { getGenerationsRequest } from "@/components/features/generation/handlers/getGenerationsRequest";
import GenerationsTable from "@/components/features/generation/tables/GenerationsTable";
import columns from "@/components/features/generation/tables/GenerationsTable/columns";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";

const useGetGenerations = createQueryHookFromTemplate(getGenerationsRequest);

export default function ManageGenerations() {
  const actions: MetaActions = ["write", "delete"];
  const { data: generations } = useGetGenerations({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <>
      <GenerationsTable
        columns={columns}
        title="Generations"
        description="List of generations for the entire system."
        data={generations?.data ?? []}
        actions={actions}
        readonly={false}
      />
    </>
  );
}
