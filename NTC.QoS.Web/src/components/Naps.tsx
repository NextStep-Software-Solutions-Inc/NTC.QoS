import { useMemo } from "react";
import { useParams, usePathname, useSearchParams } from "next/navigation";
import { Workflow } from "lucide-react";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { formatNumber } from "@/lib/format-number";
import { useLocationMap } from "@/providers/LocationMapContext";
import AdminSearchToolbar from "./AdminSearchToolbar";
import AdminSearchToolbarSkeleton from "./AdminSearchToolbarSkeleton";
import AnimatedStat from "./AnimatedStat";
import BoundaryDetails from "./BoundaryDetails";
import CardMunicipalNAPs from "./features/municipality/card/CardMunicipalNAPs";
import BoundaryNoType from "./features/municipality/tabs/BoundaryNoType";
import AddNAPSDialog from "./features/NAP/dialogs/AddNAPSDialog";
import { getNAPsRequest } from "./features/NAP/handlers/getNAPsRequest";
import FilterProviders from "./features/provider/dropdown-menu/FilterProviders";
import { getProvidersRequest } from "./features/provider/handlers/getProvidersRequest";
import { getServiceProvidersRequest } from "./features/service-provider/handlers/getServiceProviders";
import { ScrollArea } from "./ui/scroll-area";
import { Separator } from "./ui/separator";
import WithPlaceholder from "./WithPlaceholder";

const useNaps = createQueryHookFromTemplate(getNAPsRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useProviders = createQueryHookFromTemplate(getProvidersRequest);

export default function Naps() {
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const { province, brgy } = useParams<{ province: string; municipal: string; brgy: string }>();
  const { providerName, setGenericSearch, genericSearch } = useDebounceContext();
  const { resolveUuid } = useLocationMap();
  const municipalId = searchParams.get("municipalId");
  const barangayId = searchParams.get("barangayId");
  const externalLink = `${pathname}/${municipalId ?? barangayId ?? brgy ?? ""}`;
  const filterServiceProviderIds = searchParams.getAll("filterServiceProviderId");
  const sortParam = (searchParams.get("sort") ?? "asc") as "asc" | "desc";

  const locationName = resolveUuid(municipalId ?? barangayId ?? brgy ?? "") ?? "Unknown Location";
  const { data: serviceProviders, isLoading: isLoadingServiceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, serviceTypes: ["Wired"] },
  });
  const { data: providers, isLoading: isLoadingProviders } = useProviders({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, names: providerName ? [providerName] : [] },
  });
  const { data: naps, isLoading: isLoadingNaps } = useNaps({
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

  const sortedNaps = useMemo(() => {
    if (!naps?.data) {
      return [];
    }

    // First filter by search term
    let filtered = naps.data;
    if (genericSearch && genericSearch.trim()) {
      const searchLower = genericSearch.toLowerCase().trim();
      filtered = naps.data.filter((nap) => {
        const ispName = nap.ISP?.toLowerCase() || "";
        const coords = `${nap.lat},${nap.lng}`.toLowerCase();
        const napId = nap.id?.toLowerCase() || "";
        
        return (
          ispName.includes(searchLower) ||
          coords.includes(searchLower) ||
          napId.includes(searchLower)
        );
      });
    }

    // Then sort the filtered results
    const isAscending = sortParam === "asc";
    return [...filtered].sort((a, b) => {
      const napA = a?.dist ?? 0;
      const napB = b?.dist ?? 0;
      const comparisonResult = napA - napB;

      return isAscending ? comparisonResult : -comparisonResult;
    });
  }, [naps, sortParam, genericSearch]);

  // Count unique providers serving this location (by provider ID)
  const uniqueProviderCount = useMemo(() => {
    if (!naps?.data) return 0;
    const uniqueProviders = new Set<string>();
    naps.data.forEach((nap) => {
      // const providerId = nap?.serviceProvider?.provider?.id;
      const providerId = nap?.ISP;
      if (providerId) {
        uniqueProviders.add(providerId);
      }
    });
    return uniqueProviders.size;
  }, [naps]);

  const isLoading = isLoadingServiceProviders || isLoadingNaps || isLoadingProviders;
  return (
    <WithPlaceholder isLoading={isLoading} placeholder={<AdminSearchToolbarSkeleton />}>
      <BoundaryDetails label={`${locationName} NAPs`} addButton={<AddNAPSDialog />} externalLink={externalLink}>
        <AdminSearchToolbar
          broadbandName={`Service Providers`}
          statusLabel={`Total Network Access Points`}
          status={<AnimatedStat end={formatNumber(sortedNaps.length)} start={0} duration={2000} />}
          dataLength={`${uniqueProviderCount} Providers serving this area`}
          searchValue={genericSearch}
          setSearchValue={setGenericSearch}
          filterFunction={<FilterProviders serviceProviders={serviceProviders?.data ?? []} />}
          leftIcon={<Workflow className="h-5 w-5 text-muted-foreground" />}
          placeholder="Search ISP name, NAP ID, or coordinates..."
        />
        <Separator className="mb-2" />
        <ScrollArea className="h-96" style={{ scrollbarWidth: "none", msOverflowStyle: "none" }}>
          {sortedNaps.length > 0 ? (
            <CardMunicipalNAPs data={sortedNaps} />
          ) : (
            <BoundaryNoType 
              info={
                genericSearch 
                  ? `No NAPs match "${genericSearch}". Try searching for ISP name (e.g., PLDT, Globe), NAP ID, or coordinates.`
                  : "No NAPs found in this location."
              } 
              dialog={<AddNAPSDialog />} 
            />
          )}
        </ScrollArea>
      </BoundaryDetails>
    </WithPlaceholder>
  );
}
