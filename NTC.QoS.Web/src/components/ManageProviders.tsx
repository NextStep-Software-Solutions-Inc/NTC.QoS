"use client";

import ProvidersTable from "@/components/features/provider/tables/ProvidersTable";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type MetaActions } from "@/types/global";
import { getProvidersRequest } from "./features/provider/handlers/getProvidersRequest";

const useGetProviders = createQueryHookFromTemplate(getProvidersRequest);
export default function ManageProviders() {
  const actions: MetaActions = ["write", "delete"];
  const { data: providers } = useGetProviders({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <div className="flex flex-col flex-1 min-h-0 overflow-hidden">
      <ProvidersTable
        title="Providers"
        description="List of providers for the entire system."
        data={providers?.data ?? []}
        actions={actions}
        readonly={false} // Set to true if the table should be read-only
      />
    </div>
  );
}
