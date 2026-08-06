"use client";

import { useState, useMemo, useRef, useEffect, useCallback, lazy, Suspense } from "react";
import { useParams } from "next/navigation";
import { Loader2, AlertCircle, ArrowUp } from "lucide-react";
import { toast } from "sonner";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { getBarangayCoveragesRequest } from "@/components/features/barangay-coverage/handlers/getBarangayCoverageByRegionRequest";
import { getProvincesRequest } from "@/components/features/province/handlers/getProvincesRequest";
import { createBarangayCoverageRequest } from "@/components/features/barangay-coverage/handlers/createBarangayCoverageRequest";
import { updateBarangayCoverageRequest } from "@/components/features/barangay-coverage/handlers/updateBarangayCoverageRequest";
import { deleteBarangayCoverageRequest } from "@/components/features/barangay-coverage/handlers/deleteBarangayCoverageRequest";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { type BarangayWithCoverage } from "./types";
import { type BarangayCoverageLightDto } from "@/components/features/barangay-coverage/types/BarangayCoverage";

// Lazy load GroupedView for better initial load performance
const GroupedView = lazy(() => import("./GroupedView").then(m => ({ default: m.GroupedView })));

const useBarangayCoverage = createQueryHookFromTemplate(getBarangayCoveragesRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useCreateCoverage = createMutationHookFromTemplate(createBarangayCoverageRequest);
const useUpdateCoverage = createMutationHookFromTemplate(updateBarangayCoverageRequest);
const useDeleteCoverage = createMutationHookFromTemplate(deleteBarangayCoverageRequest);

const MAX_COVERAGE_PERCENTAGE = 100;
const DEFAULT_UPDATED_BY = "Current User";

// Local coverage state to track newly created coverage IDs before refetch
// Also tracks updates and deletions for optimistic UI updates
type LocalCoverageState = {
    coverageId: string;
    wiredFtthPercentage: number;
    wirelessPercentage: number;
    deleted?: boolean; // If true, coverage was deleted and should not be shown
};

// Helper function to update local coverage state
const createLocalCoverageUpdater = (
    barangayId: string,
    coverageId: string,
    values: { wiredPercentage: number; wirelessPercentage: number }
) => (prev: Map<string, LocalCoverageState>) => {
    const next = new Map(prev);
    next.set(barangayId, {
        coverageId,
        wiredFtthPercentage: values.wiredPercentage,
        wirelessPercentage: values.wirelessPercentage,
        deleted: false, // Explicitly set to false to clear any previous deletion flag
    });
    return next;
};

export default function ManageBarangayCoverage() {
    const { region } = useParams<{ region: string }>();
    const [savingBarangays, setSavingBarangays] = useState<Set<string>>(new Set());
    const [showScrollTop, setShowScrollTop] = useState(false);

    // Track locally created coverage IDs by barangayId
    const [localCoverages, setLocalCoverages] = useState<Map<string, LocalCoverageState>>(new Map());

    const scrollContainerRef = useRef<HTMLDivElement>(null);

    // Fetch provinces for the region
    const { data: provincesData, isLoading: isLoadingProvinces } = useProvinces({
        pathParams: {},
        queryParams: {
            pageSize: 100,
            regionIds: region ? [region] : [],
        },
    });

    // Fetch coverage data using endpoint with regionIds query param
    const { data: coverageData } = useBarangayCoverage({
        pathParams: {},
        queryParams: { regionIds: [region] },
    });

    const createMutation = useCreateCoverage();
    const updateMutation = useUpdateCoverage();
    const deleteMutation = useDeleteCoverage();

    // Create coverage map
    const coverageMap = useMemo(() => {
        const covMap = new Map<string, BarangayCoverageLightDto>();

        // Add server coverage data
        if (coverageData?.data) {
            coverageData.data.forEach((coverage) => {
                covMap.set(coverage.barangayId, coverage);
            });
        }

        // Merge with local coverage state for optimistic updates
        localCoverages.forEach((localCoverage, barangayId) => {
            if (localCoverage.deleted) {
                covMap.delete(barangayId);
            } else {
                const serverCoverage = covMap.get(barangayId);
                const combinedPercentage = Math.min(
                    localCoverage.wiredFtthPercentage + localCoverage.wirelessPercentage,
                    MAX_COVERAGE_PERCENTAGE
                );

                covMap.set(barangayId, {
                    id: localCoverage.coverageId,
                    barangayId: barangayId,
                    wiredFtthPercentage: localCoverage.wiredFtthPercentage,
                    wirelessPercentage: localCoverage.wirelessPercentage,
                    combinedPercentage: combinedPercentage,
                    isWiredCovered: localCoverage.wiredFtthPercentage === MAX_COVERAGE_PERCENTAGE,
                    isWirelessCovered: localCoverage.wirelessPercentage === MAX_COVERAGE_PERCENTAGE,
                    updatedAt: new Date().toISOString(),
                    updatedBy: serverCoverage?.updatedBy ?? DEFAULT_UPDATED_BY,
                });
            }
        });

        return covMap;
    }, [coverageData, localCoverages]);

    // Get provinces for grouped view
    const provinces = useMemo(() => {
        return provincesData?.data?.map(p => ({ id: p.id, name: p.name })) ?? [];
    }, [provincesData]);

    // Track scroll position for scroll-to-top button
    useEffect(() => {
        const scrollContainer = scrollContainerRef.current;
        if (!scrollContainer) return;

        const handleScroll = () => {
            const shouldShow = scrollContainer.scrollTop > 200;
            setShowScrollTop(shouldShow);
        };

        scrollContainer.addEventListener('scroll', handleScroll);

        // Check initial scroll position
        handleScroll();

        return () => scrollContainer.removeEventListener('scroll', handleScroll);
    }, [provinces.length]); // Re-run when data changes

    const scrollToTop = () => {
        scrollContainerRef.current?.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    };

    // Get the coverage ID for a barangay (from server or local state)
    // Returns undefined if coverage is marked as deleted (so CREATE path is taken)
    const getCoverageId = useCallback((barangay: BarangayWithCoverage): string | undefined => {
        const localCoverage = localCoverages.get(barangay.id);
        // Don't return coverageId if marked as deleted - we want to create new coverage
        if (localCoverage?.deleted) {
            return undefined;
        }
        return barangay.coverage?.id ?? barangay.localCoverageId ?? localCoverage?.coverageId;
    }, [localCoverages]);

    const handleSave = async (
        barangay: BarangayWithCoverage,
        values: { wiredPercentage: number; wirelessPercentage: number },
    ) => {
        setSavingBarangays((prev) => new Set(prev).add(barangay.id));

        try {
            const payload = {
                wiredFtthPercentage: values.wiredPercentage,
                wirelessPercentage: values.wirelessPercentage,
            };

            const coverageId = getCoverageId(barangay);

            if (coverageId) {
                // Update existing coverage using coverage ID (not barangay ID)
                await updateMutation.mutateAsync({
                    pathParams: { id: coverageId },
                    payload,
                });

                // Update local state with new values using helper (no refetch needed)
                setLocalCoverages(createLocalCoverageUpdater(barangay.id, coverageId, values));

                toast.success(`Updated coverage for ${barangay.name}`);
            } else {
                // Create new coverage
                const result = await createMutation.mutateAsync({
                    pathParams: {},
                    payload: {
                        barangayId: barangay.id,
                        ...payload,
                    },
                });

                // Store the returned coverage ID for future updates/deletes (no refetch needed)
                // The create endpoint returns APIResponse<string> where data is the new coverage ID
                if (result?.succeeded && result?.data) {
                    setLocalCoverages(createLocalCoverageUpdater(barangay.id, result.data, values));
                }

                toast.success(`Created coverage for ${barangay.name}`);
            }
        } catch (error) {
            console.error("Error saving coverage:", error);
            toast.error(`Failed to save coverage for ${barangay.name}`);
            // On failure, form values are kept for retry (handled by react-hook-form)
        } finally {
            setSavingBarangays((prev) => {
                const newSet = new Set(prev);
                newSet.delete(barangay.id);
                return newSet;
            });
        }
    };

    const handleDelete = async (barangay: BarangayWithCoverage) => {
        const coverageId = getCoverageId(barangay);

        if (!coverageId) {
            // No coverage to delete, just reset the form values
            return;
        }

        setSavingBarangays((prev) => new Set(prev).add(barangay.id));

        try {
            await deleteMutation.mutateAsync({
                pathParams: { id: coverageId },
            });

            // Mark as deleted in local state (no refetch needed)
            // This ensures the UI shows the coverage as deleted even if server data is stale
            setLocalCoverages(prev => {
                const next = new Map(prev);
                next.set(barangay.id, {
                    coverageId,
                    wiredFtthPercentage: 0,
                    wirelessPercentage: 0,
                    deleted: true,
                });
                return next;
            });

            toast.success(`Deleted coverage for ${barangay.name}`);
        } catch (error) {
            console.error("Error deleting coverage:", error);
            toast.error(`Failed to delete coverage for ${barangay.name}`);
            // On failure, keep the form values for retry
        } finally {
            setSavingBarangays((prev) => {
                const newSet = new Set(prev);
                newSet.delete(barangay.id);
                return newSet;
            });
        }
    };

    return (
        <div className="flex flex-col flex-1 min-h-0 overflow-hidden px-5 pb-5">
            <Card className="flex-1 flex flex-col min-h-0 overflow-hidden border-2 border-purple-200 bg-gradient-to-br from-purple-50/50 via-pink-50/50 to-blue-50/50">
                <CardHeader className="border-b border-purple-200 bg-white/80 backdrop-blur">
                    <CardTitle className="text-2xl font-bold bg-gradient-to-r from-purple-600 to-pink-600 bg-clip-text text-transparent">
                        Barangay Coverage Management
                    </CardTitle>
                    <CardDescription className="text-base text-gray-600">
                        Manage broadband validation coverage data for barangays grouped by province and municipality. Coverage data loads in
                        the background.
                    </CardDescription>
                </CardHeader>

                <CardContent ref={scrollContainerRef} className="flex-1 overflow-y-auto p-4 relative">
                    {/* Loading Overlay */}
                    {isLoadingProvinces && (
                        <div className="absolute inset-0 bg-white/80 backdrop-blur-sm z-10 flex items-center justify-center">
                            <div className="flex flex-col items-center gap-2">
                                <Loader2 className="h-8 w-8 animate-spin text-purple-600" />
                                <span className="text-sm font-medium text-gray-600">
                                    Loading provinces...
                                </span>
                            </div>
                        </div>
                    )}

                    {provinces.length === 0 && !isLoadingProvinces ? (
                        <div className="flex flex-col items-center justify-center py-12 text-gray-500">
                            <AlertCircle className="h-12 w-12 mb-3 text-gray-400" />
                            <p className="text-lg font-medium">No provinces found in this region</p>
                        </div>
                    ) : (
                        <>
                            {/* Grouped View - Only view available */}
                            <Suspense fallback={
                                <div className="flex items-center justify-center py-12">
                                    <Loader2 className="h-8 w-8 animate-spin text-purple-600" />
                                    <span className="ml-2 text-gray-600">Loading view...</span>
                                </div>
                            }>
                                <GroupedView
                                    provinces={provinces}
                                    regionId={region}
                                    coverageMap={coverageMap}
                                    onSave={handleSave}
                                    onDelete={handleDelete}
                                    savingBarangays={savingBarangays}
                                />
                            </Suspense>
                        </>
                    )}
                </CardContent>
            </Card>

            {/* Floating Scroll to Top Button */}
            {showScrollTop && (
                <Button
                    onClick={scrollToTop}
                    size="icon"
                    className="fixed bottom-8 left-1/2 -translate-x-1/2 h-12 w-12 rounded-full shadow-lg bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white z-50 transition-all duration-300 hover:scale-110"
                    aria-label="Scroll to top">
                    <ArrowUp className="h-6 w-6" />
                </Button>
            )}
        </div>
    );
}
