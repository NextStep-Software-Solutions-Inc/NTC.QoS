import { useMemo } from "react";
import { useParams, usePathname, useSearchParams } from "next/navigation";
import { ArrowUpDown } from "lucide-react";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { useLocationMap } from "@/providers/LocationMapContext";
import AdminSearchToolbar from "./AdminSearchToolbar";
import BoundaryDetails from "./BoundaryDetails";
// import { BoundaryDetailsSkeleton } from "./BoundaryDetailsSkeleton";
// import { BoundaryDetailsSpeedtestsSkeleton } from "./BoundaryDetailsCellsSkeleton copy";
import { BoundaryDetailsSpeedtestsSkeleton } from "./BoundaryDetailsCellsSkeleton";
import BoundaryNoType from "./features/municipality/tabs/BoundaryNoType";
import FilterProviders from "./features/provider/dropdown-menu/FilterProviders";
import { getServiceProvidersRequest } from "./features/service-provider/handlers/getServiceProvidersRequest";
import ProvidersSpeedTestResult from "./features/speedtest/card/ProvidersSpeedTestResult";
import ProvidersSpeedTestResultSkeleton from "./features/speedtest/card/ProvidersSpeedTestResultSkeleton";
import AddSpeedtestDialog from "./features/speedtest/dialog/AddSpeedtestDialog";
import { getSpeedtestsRequest } from "./features/speedtest/handlers/getSpeedtestsRequest";
import { ScrollArea } from "./ui/scroll-area";
import { Separator } from "./ui/separator";
import WithPlaceholder from "./WithPlaceholder";

const useSpeedTests = createQueryHookFromTemplate(getSpeedtestsRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);

export default function SpeedTests() {
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const { setGenericSearch, genericSearch } = useDebounceContext();
  const { resolveUuid } = useLocationMap();
  const { province, brgy } = useParams<{ province: string; municipal: string; brgy: string }>();
  const municipalId = searchParams.get("municipalId");
  const barangayId = searchParams.get("barangayId");
  const externalLink = `${pathname}/${municipalId ?? barangayId ?? brgy ?? ""}`;
  const filterServiceProviderIds = searchParams.getAll("filterServiceProviderId");
  const sortParam = (searchParams.get("sort") ?? "asc") as "asc" | "desc";

  const locationName = resolveUuid(municipalId ?? barangayId ?? brgy ?? "") ?? "Unknown Location";

  const { data: serviceProviders, isLoading: isLoadingServiceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1 },
  });

  const { data: speedtests, isLoading: isLoadingSpeedtests } = useSpeedTests({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      provinceIds: [province],
      municipalityIds: municipalId ? [municipalId] : [],
      barangayIds: barangayId ? [barangayId] : brgy ? [brgy] : [],
      serviceProviderIds: filterServiceProviderIds,
    },
  });

  // Count unique providers (only Wireless + Wired)
  const uniqueProviderCount = useMemo(() => {
    if (!speedtests?.data || !serviceProviders?.data) return 0;

    // Collect provider IDs that are either Wireless or Wired
    const allowedProviderIds = new Set(
      serviceProviders.data
        .filter((sp) => sp?.service.type === "Wireless" || sp?.service.type === "Wired")
        .map((sp) => sp.provider?.id)
        .filter(Boolean),
    );

    // Collect unique providers from speedtests that match allowed IDs
    const uniqueProviders = new Set<string>();
    speedtests.data.forEach((test) => {
      const providerId = test?.serviceProvider?.provider?.id;
      if (providerId && allowedProviderIds.has(providerId)) {
        uniqueProviders.add(providerId);
      }
    });

    return uniqueProviders.size;
  }, [speedtests, serviceProviders]);

  const sortedSpeedtests = useMemo(() => {
    if (!speedtests?.data) {
      return [];
    }
    const isAscending = sortParam === "asc";
    return [...speedtests.data].sort((a, b) => {
      const speedA = a?.downloadSpeed ?? 0;
      const speedB = b?.downloadSpeed ?? 0;
      const comparisonResult = speedA - speedB;
      return isAscending ? comparisonResult : -comparisonResult;
    });
  }, [speedtests, sortParam]);
  const isLoading = isLoadingServiceProviders || isLoadingSpeedtests;

  return (
    <WithPlaceholder isLoading={isLoading} placeholder={<BoundaryDetailsSpeedtestsSkeleton />}>
      <BoundaryDetails
        label={`${locationName} Speedtests`}
        addButton={<AddSpeedtestDialog />}
        externalLink={externalLink}>
        <AdminSearchToolbar
          broadbandName={`Service Providers`}
          statusLabel={`Total Speed Tests`}
          status={speedtests?.data?.length}
          dataLength={`${uniqueProviderCount} Providers serving this area`}
          searchValue={genericSearch}
          setSearchValue={setGenericSearch}
          filterFunction={<FilterProviders serviceProviders={serviceProviders?.data ?? []} />}
          leftIcon={<ArrowUpDown className="h-5 w-5 text-muted-foreground" />}
        />
        <Separator className="mb-2" />
        <ScrollArea className="h-96" style={{ scrollbarWidth: "none", msOverflowStyle: "none" }}>
          {isLoadingSpeedtests ? (
            <ProvidersSpeedTestResultSkeleton />
          ) : sortedSpeedtests.length > 0 ? (
            <ProvidersSpeedTestResult data={sortedSpeedtests} />
          ) : (
            <BoundaryNoType info={"No Speed Tests found in this location."} dialog={<AddSpeedtestDialog />} />
          )}
        </ScrollArea>
      </BoundaryDetails>
    </WithPlaceholder>
  );
}
