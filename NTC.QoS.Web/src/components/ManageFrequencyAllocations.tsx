"use client";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";
import { getFrequencyAllocationsRequest } from "./features/frequency-allocation/handlers/getFrequencyAllocationsRequest";
import { FrequencyAllocationsTable } from "./features/frequency-allocation/tables/FrequencyAllocationsTable";

const useGetFrequencyAllocations = createQueryHookFromTemplate(getFrequencyAllocationsRequest);

export default function ManageFrequencyAllocations() {
  const actions: MetaActions = ["write", "delete"];
  const { data } = useGetFrequencyAllocations({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });

  return (
    <FrequencyAllocationsTable
      title="Frequency Allocation"
      description="List of frequency allocations for the entire system."
      actions={actions}
      readonly={false}
      data={data?.data ?? []}
    />
  );
}
