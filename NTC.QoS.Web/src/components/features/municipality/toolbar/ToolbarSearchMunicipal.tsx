import React from "react";
import { useParams, useSearchParams } from "next/navigation";
import { Tabs } from "@radix-ui/react-tabs";
import { Router, Signal } from "lucide-react";

import AdminSearchToolbar from "@/components/AdminSearchToolbar";
import { TabsContent } from "@/components/ui/tabs";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { getNAPUniqueProviderNames, getUniqueServiceProviders } from "@/lib/getUniqueProivders";
import { getCellsRequest } from "../../cell/handlers/getCellsRequest";
import { getNAPsRequest } from "../../NAP/handlers/getNAPsRequest";
import FilterProviders from "../../provider/dropdown-menu/FilterProviders";
import { getSpeedtestsRequest } from "../../speedtest/handlers/getSpeedtestsRequest";

const useCells = createQueryHookFromTemplate(getCellsRequest);
const useSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);
const useNAP = createQueryHookFromTemplate(getNAPsRequest);

export default function ToolbarSearch() {
  const searchParam = useSearchParams();
  const { genericSearch, setGenericSearch } = useDebounceContext();
  const municipalId = searchParam.get("municipalId");
  const { region, table, province } = useParams<{ region: string; table: string; province: string }>();

  const { data: cells } = useCells({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: municipalId ? [municipalId] : [],
    },
  });

  const { data: speedtests } = useSpeedtests({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: municipalId ? [municipalId] : [],
    },
  });

  const { data: NAPs } = useNAP({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: municipalId ? [municipalId] : [],
    },
  });

  return (
    <Tabs value={table} className="w-full">
      <TabsContent value="cell">
        <AdminSearchToolbar
          broadbandName={`Cell Sites Available`}
          statusLabel={`Total Cell Sites`}
          status={cells?.data?.length}
          dataLength={`${getUniqueServiceProviders(cells?.data ?? [])?.length || 0} Providers serving this municipality`}
          searchValue={genericSearch}
          setSearchValue={setGenericSearch}
          filterFunction={<FilterProviders serviceProviders={getUniqueServiceProviders(cells?.data ?? [])} />}
        />
        {/* <MunicipalCellsiteListCard data={sortedCells.length ? sortedCells : cells?.data} /> */}
      </TabsContent>
      <TabsContent value="speedtest">
        <AdminSearchToolbar
          leftIcon={<Signal className="h-4 w-4 text-muted-foreground" />}
          broadbandName={`Speed Test in this Area`}
          statusLabel={`Signal Status`}
          dataLength={`${getUniqueServiceProviders(speedtests?.data ?? [])?.length || 0} Providers serving this municipality`}
          searchValue={genericSearch}
          setSearchValue={setGenericSearch}
          filterFunction={<FilterProviders serviceProviders={getUniqueServiceProviders(speedtests?.data ?? [])} />}
        />
      </TabsContent>
      <TabsContent value="nap">
        <AdminSearchToolbar
          broadbandName={`NAP Available`}
          statusLabel={`Total Network Access Points in area`}
          status={NAPs?.data?.length}
          leftIcon={<Router className="h-4 w-4 text-muted-foreground" />}
          dataLength={`${getNAPUniqueProviderNames(NAPs?.data)?.length || 0} Providers serving this municipality`}
          searchValue={genericSearch}
          setSearchValue={setGenericSearch}
          // filterFunction={<FilterProviders serviceProviders={getNAPUniqueProviderNames(NAPs?.data)} />}
        />
      </TabsContent>
    </Tabs>
  );
}
