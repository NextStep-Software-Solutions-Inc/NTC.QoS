import { useMemo } from "react";
import { useParams, usePathname, useSearchParams } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { useLocationMap } from "@/providers/LocationMapContext";
import AdminSearchToolbar from "./AdminSearchToolbar";
import AnimatedStat from "./AnimatedStat";
import BoundaryDetails from "./BoundaryDetails";
// import { BoundaryDetailsCellsSkeleton } from "./BoundaryDetailsCellsSkeleton";
// import { BoundaryDetailsCellsSkeleton } from "./BoundaryDetailsSpeedtestsSkeleton";
import { BoundaryDetailsCellsSkeleton } from "./BoundaryDetailsSpeedtestsSkeleton";
import { getCellsRequest } from "./features/cell/handlers/getCellsRequest";
import MunicipalCellsiteListCard from "./features/municipality/card/MunicipalCellsiteListCard";
import BoundaryNoType from "./features/municipality/tabs/BoundaryNoType";
import FilterProviders from "./features/provider/dropdown-menu/FilterProviders";
import { getProvidersRequest } from "./features/provider/handlers/getProvidersRequest";
import { getServiceProvidersRequest } from "./features/service-provider/handlers/getServiceProviders";
import AddCellSiteDialog from "./Tables/AddCellSiteDialog";
import { ScrollArea } from "./ui/scroll-area";
import { Separator } from "./ui/separator";
import WithPlaceholder from "./WithPlaceholder";

const useCells = createQueryHookFromTemplate(getCellsRequest);
const useProviders = createQueryHookFromTemplate(getProvidersRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);

export default function CellSites() {
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const { providerName, setGenericSearch, genericSearch } = useDebounceContext();
  const { resolveUuid } = useLocationMap();
  const { province, brgy } = useParams<{ province: string; municipal: string; brgy: string }>();
  const filterServiceProviderIds = searchParams.getAll("filterServiceProviderId");
  const sortParam = (searchParams.get("sort") ?? "asc") as "asc" | "desc";
  const municipalId = searchParams.get("municipalId");
  const barangayId = searchParams.get("barangayId");
  const externalLink = `${pathname}/${municipalId ?? barangayId ?? brgy ?? ""}`;

  const { data: serviceProviders, isLoading: isLoadingServiceProviders } = useServiceProviders({
    pathParams: {},
      queryParams: { pageSize: 1000, pageNumber: 1, serviceTypes: ["Wireless"] },
  });

  const { data: providers, isLoading: isLoadingProviders } = useProviders({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, names: providerName ? [providerName] : [] },
  });

  const { data: cells, isLoading: isLoadingCells } = useCells({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      provinceIds: [province],
      municipalityIds: municipalId ? [municipalId] : [],
      barangayIds: barangayId ? [barangayId] : brgy ? [brgy] : [],
      serviceProviderIds: filterServiceProviderIds,
      providerIds: providers?.data.map((p) => p.id),
    },
  });

  // Count number of cell sites per provider
  const providerCellCounts = useMemo(() => {
    const counts = new Map<string, number>();
    if (cells?.data) {
      cells.data.forEach((cell) => {
        const providerName = cell?.serviceProvider?.provider?.name || "Unknown Provider";
        counts.set(providerName, (counts.get(providerName) ?? 0) + 1);
      });
    }
    return counts;
  }, [cells]);

  const uniqueProviderCount = useMemo(() => {
    if (!cells?.data || !serviceProviders?.data) return 0;

    const wirelessProviderIds = new Set(serviceProviders.data.map((sp) => sp.provider?.id).filter(Boolean));
    const uniqueProviders = new Set<string>();
    cells.data.forEach((cell) => {
      const providerId = cell?.serviceProvider?.provider?.id;
      if (providerId && wirelessProviderIds.has(providerId)) {
        uniqueProviders.add(providerId);
      }
    });

    return uniqueProviders.size;
  }, [cells, serviceProviders]);

  // Sort cell sites by provider cell count
  const sortedCells = useMemo(() => {
    if (!cells?.data) return [];
    const isAscending = sortParam === "asc";
    return [...cells.data].sort((a, b) => {
      const providerA = a?.serviceProvider?.provider?.name || "Unknown Provider";
      const providerB = b?.serviceProvider?.provider?.name || "Unknown Provider";
      const countA = providerCellCounts.get(providerA) ?? 0;
      const countB = providerCellCounts.get(providerB) ?? 0;
      return isAscending ? countA - countB : countB - countA;
    });
  }, [cells, sortParam, providerCellCounts]);
  const isLoading = isLoadingServiceProviders || isLoadingProviders || isLoadingCells;

  const locationName = resolveUuid(municipalId ?? barangayId ?? brgy ?? "") ?? "Unknown Location";

  return (
    <WithPlaceholder isLoading={isLoading} placeholder={<BoundaryDetailsCellsSkeleton />}>
      <BoundaryDetails
        label={`${locationName} Cell Sites`}
        addButton={<AddCellSiteDialog />}
        externalLink={externalLink}>
        <AdminSearchToolbar
          broadbandName="Service Providers"
          statusLabel="Total Cell Sites"
          status={<AnimatedStat start={0} end={cells?.data?.length.toString() ?? "0"} duration={500} />}
          dataLength={`${uniqueProviderCount} Providers serving this area`}
          searchValue={genericSearch}
          setSearchValue={setGenericSearch}
          filterFunction={<FilterProviders serviceProviders={serviceProviders?.data ?? []} />}
        />
        <Separator className="mb-2" />
        <ScrollArea className="h-96" style={{ scrollbarWidth: "none", msOverflowStyle: "none" }}>
          {sortedCells.length > 0 ? (
            <MunicipalCellsiteListCard data={sortedCells} />
          ) : (
            <BoundaryNoType info="No Cell Sites found in this location." dialog={<AddCellSiteDialog />} />
          )}
        </ScrollArea>
      </BoundaryDetails>
    </WithPlaceholder>
  );
}
