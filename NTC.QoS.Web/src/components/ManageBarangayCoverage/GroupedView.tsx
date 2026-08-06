import { memo, useState } from "react";
import { MapPin, Loader2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { BarangayCard } from "./BarangayCard";
import { type BarangayWithCoverage } from "./types";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getMunicipalitiesRequest } from "@/components/features/municipality/handlers/getMunicipalitiesRequest";
import { getBarangaysInfiniteRequest } from "@/components/features/barangay/handlers/getBarangaysInfiniteRequest";
import { createInfiniteQueryHookFromTemplate } from "@/hooks/createInfiniteQueryHookFromTemplate";
import { type BarangayCoverageLightDto } from "@/components/features/barangay-coverage/types/BarangayCoverage";

const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
const useBarangaysInfinite = createInfiniteQueryHookFromTemplate(getBarangaysInfiniteRequest);

interface GroupedViewProps {
    provinces: Array<{ id: string; name: string }>;
    regionId: string;
    coverageMap: Map<string, BarangayCoverageLightDto>;
    onSave: (barangay: BarangayWithCoverage, values: { wiredPercentage: number; wirelessPercentage: number }) => void;
    onDelete: (barangay: BarangayWithCoverage) => void;
    savingBarangays: Set<string>;
}

// Municipality Section - loads barangays only when expanded
const MunicipalitySection = memo(function MunicipalitySection({
    municipality,
    coverageMap,
    onSave,
    onDelete,
    savingBarangays,
}: {
    municipality: { id: string; name: string };
    coverageMap: Map<string, BarangayCoverageLightDto>;
    onSave: (barangay: BarangayWithCoverage, values: { wiredPercentage: number; wirelessPercentage: number }) => void;
    onDelete: (barangay: BarangayWithCoverage) => void;
    savingBarangays: Set<string>;
}) {
    const [isExpanded, setIsExpanded] = useState(false);

    // Only fetch barangays when expanded
    const {
        data: barangaysData,
        isLoading,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
    } = useBarangaysInfinite({
        pathParams: {},
        queryParams: {
            pageSize: 50,
            municipalityIds: [municipality.id],
        },
        enabled: isExpanded,
    });

    const barangays = barangaysData?.pages.flatMap((page) => page.data) ?? [];

    // Create barangays with coverage
    const barangaysWithCoverage: BarangayWithCoverage[] = barangays.map((barangay) => {
        const coverage = coverageMap.get(barangay.id);
        return {
            ...barangay,
            coverage,
            wiredPercentage: coverage?.wiredFtthPercentage ?? 0,
            wirelessPercentage: coverage?.wirelessPercentage ?? 0,
            isDirty: false,
        };
    });

    return (
        <AccordionItem
            key={municipality.id}
            value={municipality.id}
            className="border border-blue-200 rounded-lg bg-blue-50/30">
            <AccordionTrigger 
                className="px-3 py-2 hover:no-underline hover:bg-blue-100/50 rounded-t-lg"
                onClick={() => setIsExpanded(!isExpanded)}
            >
                <div className="flex items-center gap-2">
                    <MapPin className="h-4 w-4 text-blue-600" />
                    <span className="font-medium text-base text-blue-900">{municipality.name}</span>
                    {barangays.length > 0 && (
                        <Badge variant="outline" className="ml-2 bg-blue-100 text-blue-700 border-blue-300">
                            {barangays.length} barangays
                        </Badge>
                    )}
                </div>
            </AccordionTrigger>
            <AccordionContent className="px-3 pb-2">
                {isLoading ? (
                    <div className="flex items-center justify-center py-8">
                        <Loader2 className="h-5 w-5 animate-spin text-blue-600" />
                        <span className="ml-2 text-sm text-gray-600">Loading barangays...</span>
                    </div>
                ) : barangaysWithCoverage.length === 0 ? (
                    <div className="text-center py-4 text-gray-500 text-sm">No barangays found</div>
                ) : (
                    <>
                        <div className="space-y-1 mt-2" style={{ contentVisibility: 'auto' }}>
                            {barangaysWithCoverage.map((barangay) => (
                                <BarangayCard
                                    key={barangay.id}
                                    barangay={barangay}
                                    onSave={onSave}
                                    onDelete={onDelete}
                                    isSaving={savingBarangays.has(barangay.id)}
                                />
                            ))}
                        </div>
                        {hasNextPage && (
                            <div className="flex justify-center py-3">
                                <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    onClick={() => fetchNextPage()}
                                    disabled={isFetchingNextPage}
                                    className="border-blue-300 text-blue-700 hover:bg-blue-100"
                                >
                                    {isFetchingNextPage ? (
                                        <>
                                            <Loader2 className="h-3 w-3 animate-spin mr-1" />
                                            Loading...
                                        </>
                                    ) : (
                                        'Load more barangays'
                                    )}
                                </Button>
                            </div>
                        )}
                    </>
                )}
            </AccordionContent>
        </AccordionItem>
    );
});

// Province Section - loads municipalities only when expanded
const ProvinceSection = memo(function ProvinceSection({
    province,
    regionId,
    coverageMap,
    onSave,
    onDelete,
    savingBarangays,
}: {
    province: { id: string; name: string };
    regionId: string;
    coverageMap: Map<string, BarangayCoverageLightDto>;
    onSave: (barangay: BarangayWithCoverage, values: { wiredPercentage: number; wirelessPercentage: number }) => void;
    onDelete: (barangay: BarangayWithCoverage) => void;
    savingBarangays: Set<string>;
}) {
    const [isExpanded, setIsExpanded] = useState(false);

    // Only fetch municipalities when expanded
    const { data: municipalitiesData, isLoading } = useMunicipalities({
        pathParams: {},
        queryParams: {
            pageSize: 100,
            provinceId: province.id,
        },
        enabled: isExpanded,
    });

    const municipalities = municipalitiesData?.data ?? [];

    return (
        <AccordionItem
            key={province.id}
            value={province.id}
            className="border-2 border-purple-200 rounded-lg bg-white shadow-sm">
            <AccordionTrigger 
                className="px-4 py-3 hover:no-underline hover:bg-purple-50 rounded-t-lg"
                onClick={() => setIsExpanded(!isExpanded)}
            >
                <div className="flex items-center gap-2">
                    <MapPin className="h-5 w-5 text-purple-600" />
                    <span className="font-semibold text-lg text-purple-900">{province.name}</span>
                    {municipalities.length > 0 && (
                        <Badge variant="outline" className="ml-2 bg-purple-100 text-purple-700 border-purple-300">
                            {municipalities.length} municipalities
                        </Badge>
                    )}
                </div>
            </AccordionTrigger>
            <AccordionContent className="px-4 pb-3">
                {isLoading ? (
                    <div className="flex items-center justify-center py-8">
                        <Loader2 className="h-6 w-6 animate-spin text-purple-600" />
                        <span className="ml-2 text-gray-600">Loading municipalities...</span>
                    </div>
                ) : municipalities.length === 0 ? (
                    <div className="text-center py-4 text-gray-500">No municipalities found</div>
                ) : (
                    <Accordion type="multiple" className="space-y-2 mt-2">
                        {municipalities.map((municipality) => (
                            <MunicipalitySection
                                key={municipality.id}
                                municipality={municipality}
                                coverageMap={coverageMap}
                                onSave={onSave}
                                onDelete={onDelete}
                                savingBarangays={savingBarangays}
                            />
                        ))}
                    </Accordion>
                )}
            </AccordionContent>
        </AccordionItem>
    );
});

export const GroupedView = memo(function GroupedView({ 
    provinces, 
    regionId,
    coverageMap,
    onSave, 
    onDelete, 
    savingBarangays 
}: GroupedViewProps) {
    return (
        <Accordion type="multiple" className="space-y-2">
            {provinces.map((province) => (
                <ProvinceSection
                    key={province.id}
                    province={province}
                    regionId={regionId}
                    coverageMap={coverageMap}
                    onSave={onSave}
                    onDelete={onDelete}
                    savingBarangays={savingBarangays}
                />
            ))}
        </Accordion>
    );
});
