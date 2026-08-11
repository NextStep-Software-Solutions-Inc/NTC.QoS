"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Check, ChevronDown, Loader2, Play, Zap } from "lucide-react";

import { getBarangaysRequest } from "@/components/features/barangay/handlers/getBarangaysRequest";
import { getMunicipalitiesRequest } from "@/components/features/municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "@/components/features/province/handlers/getProvincesRequest";
import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Progress } from "@/components/ui/progress";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import {
    useConnectivityAssessmentStatus,
    useStartConnectivityAssessment,
} from "../hooks/useConnectivityAssessment";
import type { JobStatus } from "../types/connectivity-assessment";

const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
const useBarangays = createQueryHookFromTemplate(getBarangaysRequest);

export function ConnectivityAssessmentTrigger() {
    const { region: regionParam } = useParams<{ region?: string }>();
    const [isOpen, setIsOpen] = useState(false);
    const [provinceIds, setProvinceIds] = useState<string[]>([]);
    const [municipalityIds, setMunicipalityIds] = useState<string[]>([]);
    const [barangayIds, setBarangayIds] = useState<string[]>([]);
    const [currentJobId, setCurrentJobId] = useState<string | null>(null);
    
    // Restore job ID from localStorage on mount
    useEffect(() => {
        const storageKey = `connectivity-assessment-job-${regionParam}`;
        const storedJobId = localStorage.getItem(storageKey);
        if (storedJobId) {
            setCurrentJobId(storedJobId);
            setIsOpen(true); // Auto-open the collapsible when restoring a job
        }
    }, [regionParam]);
    
    // Popover states for multi-select
    const [provincePopoverOpen, setProvincePopoverOpen] = useState(false);
    const [municipalityPopoverOpen, setMunicipalityPopoverOpen] = useState(false);
    const [barangayPopoverOpen, setBarangayPopoverOpen] = useState(false);

    // Fetch location data
    const { data: regionsData } = useRegions({
        pathParams: {},
        queryParams: { pageSize: 100, pageNumber: 1 },
    });

    const { data: provincesData } = useProvinces({
        pathParams: {},
        queryParams: {
            pageSize: 100,
            pageNumber: 1,
            regionIds: regionParam ? [regionParam] : [],
            isWiredCovered: true,
            isWirelessCovered: true,
        },
    });

    const { data: municipalitiesData } = useMunicipalities({
        pathParams: {},
        queryParams: {
            pageSize: 100,
            pageNumber: 1,
            provinceIds: provinceIds.length > 0 ? provinceIds : undefined,
            isWiredCovered: true,
            isWirelessCovered: true,
        },
    });

    const { data: barangaysData } = useBarangays({
        pathParams: {},
        queryParams: {
            pageSize: 1000,
            pageNumber: 1,
            municipalityIds: municipalityIds.length > 0 ? municipalityIds : undefined,
            isWiredCovered: true,
            isWirelessCovered: true,
        },
    });

    const regions = regionsData?.data || [];
    const provinces = provincesData?.data || [];
    const municipalities = municipalitiesData?.data || [];
    const barangays = barangaysData?.data || [];

    const currentRegion = regions.find((r) => r.id === regionParam);

    // Mutations and queries
    const startAssessment = useStartConnectivityAssessment();
    const { data: statusData, isLoading: isLoadingStatus } =
        useConnectivityAssessmentStatus(currentJobId);

    const jobStatus = statusData?.data;

    // Clear localStorage when job is completed or failed
    useEffect(() => {
        if (jobStatus && (jobStatus.status === "Completed" || jobStatus.status === "Failed")) {
            localStorage.removeItem(`connectivity-assessment-job-${regionParam}`);
        }
    }, [jobStatus, regionParam]);

    const handleStartAssessment = () => {
        startAssessment.mutate(
            {
                pathParams: {},
                payload: {
                    regionIds: regionParam ? [regionParam] : undefined,
                    provinceIds: provinceIds.length > 0 ? provinceIds : undefined,
                    municipalityIds: municipalityIds.length > 0 ? municipalityIds : undefined,
                    barangayIds: barangayIds.length > 0 ? barangayIds : undefined,
                    forceRecalculate: true, // Always true in FE
                    generateHierarchicalAssessments: hierarchicalDepth > 0,
                    hierarchicalDepth: hierarchicalDepth > 0 ? hierarchicalDepth : undefined,
                },
            },
            {
                onSuccess: (response: any) => {
                    if (response.data?.jobId) {
                        const jobId = response.data.jobId;
                        const storageKey = `connectivity-assessment-job-${regionParam}`;
                        setCurrentJobId(jobId);
                        // Persist job ID to localStorage
                        localStorage.setItem(storageKey, jobId);
                    }
                },
            }
        );
    };

    const getAutomaticHierarchicalDepth = () => {
        // Automatically set depth to maximum scope based on selections
        if (barangayIds.length > 0) {
            return 0; // Selected barangays only (no sub-levels)
        }
        if (municipalityIds.length > 0) {
            return 1; // Municipalities → All barangays
        }
        if (provinceIds.length > 0) {
            return 2; // Provinces → Municipalities → Barangays
        }
        // Region level - maximum depth
        return 3; // Region → Provinces → Municipalities → Barangays
    };

    const hierarchicalDepth = getAutomaticHierarchicalDepth();

    const getStatusBadgeVariant = (status: JobStatus) => {
        switch (status) {
            case "Completed":
                return "default";
            case "Failed":
                return "destructive";
            case "Processing":
                return "secondary";
            case "Pending":
                return "secondary";
            default:
                return "outline";
        }
    };

    const isJobActive = jobStatus?.isRunning || 
                        startAssessment.isPending || 
                        jobStatus?.status === "Pending" || 
                        jobStatus?.status === "Processing";

    return (
        <Collapsible open={isOpen} onOpenChange={setIsOpen}>
            <Card>
                <CollapsibleTrigger className="w-full">
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <Zap className="h-5 w-5 text-primary" />
                                <CardTitle>Generate Connectivity Assessment</CardTitle>
                            </div>
                            <ChevronDown
                                className={`h-5 w-5 transition-transform ${isOpen ? "rotate-180" : ""}`}
                            />
                        </div>
                        <CardDescription className="text-left">
                            Start a new connectivity assessment job for selected locations. The
                            system will analyze broadband connectivity and generate comprehensive
                            reports.
                        </CardDescription>
                    </CardHeader>
                </CollapsibleTrigger>
                <CollapsibleContent>
                    <CardContent className="space-y-6 pt-6">
                        {/* Location Filters - Row Layout */}
                        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="region-display">Region</Label>
                                <div className="h-10 rounded-md border border-input bg-muted px-3 py-2 text-sm">
                                    {currentRegion?.name || "Loading..."}
                                </div>
                            </div>

                            {/* Province Multi-Select */}
                            <div className="space-y-2">
                                <Label>Province</Label>
                                <Popover open={provincePopoverOpen} onOpenChange={setProvincePopoverOpen}>
                                    <PopoverTrigger asChild>
                                        <Button
                                            variant="outline"
                                            role="combobox"
                                            disabled={isJobActive}
                                            className="w-full justify-between h-10"
                                        >
                                            <span className="truncate">
                                                {provinceIds.length === 0
                                                    ? "All Provinces"
                                                    : provinceIds.length === 1
                                                      ? provinces.find((p) => p.id === provinceIds[0])?.name
                                                      : `${provinceIds.length} selected`}
                                            </span>
                                            <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                        </Button>
                                    </PopoverTrigger>
                                    <PopoverContent className="w-[300px] p-3" align="start">
                                        <ScrollArea className="h-64">
                                            <div className="space-y-2">
                                                <div
                                                    className="flex items-center space-x-2 p-2 hover:bg-muted rounded cursor-pointer"
                                                    onClick={() => {
                                                        setProvinceIds([]);
                                                        setMunicipalityIds([]);
                                                        setBarangayIds([]);
                                                    }}
                                                >
                                                    <Checkbox
                                                        checked={provinceIds.length === 0}
                                                        onCheckedChange={() => {
                                                            setProvinceIds([]);
                                                            setMunicipalityIds([]);
                                                            setBarangayIds([]);
                                                        }}
                                                    />
                                                    <span className="text-sm font-medium">All Provinces</span>
                                                </div>
                                                {provinces.map((province) => (
                                                    <div
                                                        key={province.id}
                                                        className="flex items-center space-x-2 p-2 hover:bg-muted rounded cursor-pointer"
                                                        onClick={() => {
                                                            const isSelected = provinceIds.includes(province.id!);
                                                            if (isSelected) {
                                                                setProvinceIds(provinceIds.filter((id) => id !== province.id));
                                                            } else {
                                                                setProvinceIds([...provinceIds, province.id!]);
                                                            }
                                                            setMunicipalityIds([]);
                                                            setBarangayIds([]);
                                                        }}
                                                    >
                                                        <Checkbox
                                                            checked={provinceIds.includes(province.id!)}
                                                            onCheckedChange={(checked) => {
                                                                if (checked) {
                                                                    setProvinceIds([...provinceIds, province.id!]);
                                                                } else {
                                                                    setProvinceIds(provinceIds.filter((id) => id !== province.id));
                                                                }
                                                                setMunicipalityIds([]);
                                                                setBarangayIds([]);
                                                            }}
                                                        />
                                                        <span className="text-sm">{province.name}</span>
                                                    </div>
                                                ))}
                                            </div>
                                        </ScrollArea>
                                    </PopoverContent>
                                </Popover>
                            </div>

                            {/* Municipality Multi-Select */}
                            <div className="space-y-2">
                                <Label>Municipality</Label>
                                <Popover open={municipalityPopoverOpen} onOpenChange={setMunicipalityPopoverOpen}>
                                    <PopoverTrigger asChild>
                                        <Button
                                            variant="outline"
                                            role="combobox"
                                            disabled={isJobActive || provinceIds.length === 0}
                                            className="w-full justify-between h-10"
                                        >
                                            <span className="truncate">
                                                {municipalityIds.length === 0
                                                    ? "All Municipalities"
                                                    : municipalityIds.length === 1
                                                      ? municipalities.find((m) => m.id === municipalityIds[0])?.name
                                                      : `${municipalityIds.length} selected`}
                                            </span>
                                            <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                        </Button>
                                    </PopoverTrigger>
                                    <PopoverContent className="w-[300px] p-3" align="start">
                                        <ScrollArea className="h-64">
                                            <div className="space-y-2">
                                                <div
                                                    className="flex items-center space-x-2 p-2 hover:bg-muted rounded cursor-pointer"
                                                    onClick={() => {
                                                        setMunicipalityIds([]);
                                                        setBarangayIds([]);
                                                    }}
                                                >
                                                    <Checkbox
                                                        checked={municipalityIds.length === 0}
                                                        onCheckedChange={() => {
                                                            setMunicipalityIds([]);
                                                            setBarangayIds([]);
                                                        }}
                                                    />
                                                    <span className="text-sm font-medium">All Municipalities</span>
                                                </div>
                                                {municipalities.map((municipality) => (
                                                    <div
                                                        key={municipality.id}
                                                        className="flex items-center space-x-2 p-2 hover:bg-muted rounded cursor-pointer"
                                                        onClick={() => {
                                                            const isSelected = municipalityIds.includes(municipality.id!);
                                                            if (isSelected) {
                                                                setMunicipalityIds(
                                                                    municipalityIds.filter((id) => id !== municipality.id)
                                                                );
                                                            } else {
                                                                setMunicipalityIds([...municipalityIds, municipality.id!]);
                                                            }
                                                            setBarangayIds([]);
                                                        }}
                                                    >
                                                        <Checkbox
                                                            checked={municipalityIds.includes(municipality.id!)}
                                                            onCheckedChange={(checked) => {
                                                                if (checked) {
                                                                    setMunicipalityIds([...municipalityIds, municipality.id!]);
                                                                } else {
                                                                    setMunicipalityIds(
                                                                        municipalityIds.filter((id) => id !== municipality.id)
                                                                    );
                                                                }
                                                                setBarangayIds([]);
                                                            }}
                                                        />
                                                        <span className="text-sm">{municipality.name}</span>
                                                    </div>
                                                ))}
                                            </div>
                                        </ScrollArea>
                                    </PopoverContent>
                                </Popover>
                            </div>

                            {/* Barangay Multi-Select */}
                            <div className="space-y-2">
                                <Label>Barangay</Label>
                                <Popover open={barangayPopoverOpen} onOpenChange={setBarangayPopoverOpen}>
                                    <PopoverTrigger asChild>
                                        <Button
                                            variant="outline"
                                            role="combobox"
                                            disabled={isJobActive || municipalityIds.length === 0}
                                            className="w-full justify-between h-10"
                                        >
                                            <span className="truncate">
                                                {barangayIds.length === 0
                                                    ? "All Barangays"
                                                    : barangayIds.length === 1
                                                      ? barangays.find((b) => b.id === barangayIds[0])?.name
                                                      : `${barangayIds.length} selected`}
                                            </span>
                                            <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                        </Button>
                                    </PopoverTrigger>
                                    <PopoverContent className="w-[300px] p-3" align="start">
                                        <ScrollArea className="h-64">
                                            <div className="space-y-2">
                                                <div
                                                    className="flex items-center space-x-2 p-2 hover:bg-muted rounded cursor-pointer"
                                                    onClick={() => setBarangayIds([])}
                                                >
                                                    <Checkbox
                                                        checked={barangayIds.length === 0}
                                                        onCheckedChange={() => setBarangayIds([])}
                                                    />
                                                    <span className="text-sm font-medium">All Barangays</span>
                                                </div>
                                                {barangays.map((barangay) => (
                                                    <div
                                                        key={barangay.id}
                                                        className="flex items-center space-x-2 p-2 hover:bg-muted rounded cursor-pointer"
                                                        onClick={() => {
                                                            const isSelected = barangayIds.includes(barangay.id!);
                                                            if (isSelected) {
                                                                setBarangayIds(barangayIds.filter((id) => id !== barangay.id));
                                                            } else {
                                                                setBarangayIds([...barangayIds, barangay.id!]);
                                                            }
                                                        }}
                                                    >
                                                        <Checkbox
                                                            checked={barangayIds.includes(barangay.id!)}
                                                            onCheckedChange={(checked) => {
                                                                if (checked) {
                                                                    setBarangayIds([...barangayIds, barangay.id!]);
                                                                } else {
                                                                    setBarangayIds(barangayIds.filter((id) => id !== barangay.id));
                                                                }
                                                            }}
                                                        />
                                                        <span className="text-sm">
                                                            {barangay.name}
                                                            {barangay.municipality?.name && (
                                                                <span className="text-muted-foreground"> ({barangay.municipality.name})</span>
                                                            )}
                                                        </span>
                                                    </div>
                                                ))}
                                            </div>
                                        </ScrollArea>
                                    </PopoverContent>
                                </Popover>
                            </div>
                        </div>

                        {/* Start Button */}
                        <Button
                            onClick={handleStartAssessment}
                            disabled={isJobActive}
                            className="w-full"
                            size="lg"
                        >
                            {startAssessment.isPending ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Starting Assessment...
                                </>
                            ) : (
                                <>
                                    <Play className="mr-2 h-4 w-4" />
                                    Start Assessment
                                </>
                            )}
                        </Button>

                        {/* Job Status Display */}
                        {currentJobId && jobStatus && (
                            <div className="space-y-4 rounded-lg border p-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-1">
                                        <div className="flex items-center gap-2">
                                            <span className="text-sm font-medium">Job Status</span>
                                            <Badge variant={getStatusBadgeVariant(jobStatus.status)}>
                                                {jobStatus.status}
                                            </Badge>
                                        </div>
                                        <div className="flex items-center gap-3 text-xs text-muted-foreground">
                                            <span>Job ID: {currentJobId}</span>
                                            {jobStatus.processingTime && (
                                                <>
                                                    <span>•</span>
                                                    <span>{jobStatus.processingTime}</span>
                                                </>
                                            )}
                                        </div>
                                    </div>
                                    {jobStatus.isRunning && (
                                        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                                    )}
                                </div>

                                {/* Progress */}
                                {jobStatus.totalCount > 0 && (
                                    <div className="space-y-2">
                                        <div className="flex justify-between text-sm">
                                            <span className="text-muted-foreground">Progress</span>
                                            <span className="font-medium">
                                                {jobStatus.processedCount} / {jobStatus.totalCount}{" "}
                                                ({jobStatus.progressPercentage.toFixed(1)}%)
                                            </span>
                                        </div>
                                        <Progress value={jobStatus.progressPercentage} />
                                    </div>
                                )}

                                {/* Status Message */}
                                {jobStatus.isRunning && (
                                    <div className="rounded-lg border bg-muted/50 p-3 text-sm text-muted-foreground">
                                        <div className="flex items-center gap-2">
                                            <Loader2 className="h-4 w-4 animate-spin" />
                                            <span>Processing assessment data... This may take a while depending on the scope of your selection.</span>
                                        </div>
                                    </div>
                                )}

                                {/* Errors */}
                                {jobStatus.failedLocationCount > 0 && (
                                    <div className="rounded-lg border border-destructive bg-destructive/10 p-3 text-sm text-destructive">
                                        {jobStatus.failedLocationCount} location(s) failed to process
                                    </div>
                                )}
                            </div>
                        )}
                    </CardContent>
                </CollapsibleContent>
            </Card>
        </Collapsible>
    );
}
