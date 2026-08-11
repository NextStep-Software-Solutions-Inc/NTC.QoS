"use client";

import { useEffect, useState, useMemo } from "react";
import { useParams } from "next/navigation";
import { Activity, Download, FileText, Loader2, ChevronDown, ChevronRight } from "lucide-react";

import { getMunicipalitiesRequest } from "@/components/features/municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "@/components/features/province/handlers/getProvincesRequest";
import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getAuthHeaders, getBackendUrl } from "@/lib/api-utils";
import { getConnectivityReportRequest } from "../handlers/getConnectivityReportRequest";
import { ConnectivityStatusEnum, ReportFormat, type AreaDto } from "../types/Report";
import { determineReportScope, generateReportFilename } from "../utils/generateReportFilename";

const useConnectivityReport = createQueryHookFromTemplate(getConnectivityReportRequest);
const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);

export function ConnectivityAndGapReport() {
  const { region: regionParam } = useParams<{ region?: string }>();
  const [regionId, setRegionId] = useState<string>(() => regionParam ?? "all");
  const [provinceId, setProvinceId] = useState<string>("all");
  const [municipalityId, setMunicipalityId] = useState<string>("all");
  const [filterByStatus, setFilterByStatus] = useState<string>("all");
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [downloadingFormat, setDownloadingFormat] = useState<ReportFormat | null>(null);
  const [expandedRegions, setExpandedRegions] = useState<Set<string>>(new Set());
  const [expandedProvinces, setExpandedProvinces] = useState<Set<string>>(new Set());
  const [expandedMunicipalities, setExpandedMunicipalities] = useState<Set<string>>(new Set());
  const [breakdownView, setBreakdownView] = useState<"regional" | "provincial" | "municipality" | "barangay">(
    "regional",
  );
  const pageSize = 10;

  useEffect(() => {
    if (!regionParam || regionParam === regionId) return;
    setRegionId(regionParam);
    setProvinceId("all");
    setMunicipalityId("all");
    setPageNumber(1);
  }, [regionParam, regionId]);

  const { data: regionsData } = useRegions({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      isWiredCovered: true,
      isWirelessCovered: true,
    },
  });

  const { data: provincesData } = useProvinces({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      regionIds: regionId !== "all" && regionId ? [regionId] : [],
      isWiredCovered: true,
      isWirelessCovered: true,
    },
  });

  const { data: municipalitiesData } = useMunicipalities({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      provinceId: provinceId !== "all" ? provinceId : undefined,
      isWiredCovered: true,
      isWirelessCovered: true,
    },
  });

  const {
    data: reportResponse,
    isLoading,
    refetch,
  } = useConnectivityReport({
    pathParams: {},
    queryParams: {
      regionId: regionId !== "all" ? regionId : undefined,
      provinceId: provinceId !== "all" ? provinceId : undefined,
      municipalityId: municipalityId !== "all" ? municipalityId : undefined,
      filterByStatus: filterByStatus !== "all" ? (filterByStatus as ConnectivityStatusEnum) : undefined,
      pageNumber,
      pageSize,
    },
    enabled: false,
  });

  const connectivityData = reportResponse?.data;

  const regions = regionsData?.data || [];
  const provinces = provincesData?.data || [];
  const municipalities = municipalitiesData?.data || [];
  const currentRegion = regions.find((r) => r.id === regionId);
  const currentProvince = provinces.find((p) => p.id === provinceId);
  const currentMunicipality = municipalities.find((m) => m.id === municipalityId);

  // Auto-fetch connectivity when page changes
  useEffect(() => {
    if (connectivityData) {
      refetch();
    }
  }, [pageNumber]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleLoadReport = () => {
    refetch();
  };

  const handleDownload = async (format: ReportFormat) => {
    setDownloadingFormat(format);
    try {
      const params = new URLSearchParams({
        format,
        ...(regionId !== "all" && { regionId }),
        ...(provinceId !== "all" && { provinceId }),
        ...(municipalityId !== "all" && { municipalityId }),
        ...(filterByStatus !== "all" && { filterByStatus }),
      });

      const backendUrl = getBackendUrl();
      const urlPath = `${backendUrl}/reports/connectivity/download?${params.toString()}`;

      const response = await fetch(urlPath, {
        method: "GET",
        headers: { Accept: "application/octet-stream", ...getAuthHeaders() },
      });

      if (!response.ok) {
        const errorText = await response.text().catch(() => "Unknown error");
        throw new Error(`Download failed: ${response.status} ${response.statusText}. ${errorText}`);
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;

      const { scope, locationName } = determineReportScope(
        regionId,
        provinceId,
        municipalityId,
        currentRegion?.name,
        currentProvince?.name,
        currentMunicipality?.name,
      );

      const filename = generateReportFilename({
        reportType: "Connectivity",
        scope,
        locationName,
        extension: format === ReportFormat.Excel ? "xlsx" : "pdf",
      });

      a.download = filename;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error("Download error:", error);
    } finally {
      setDownloadingFormat(null);
    }
  };

  const getStatusBadge = (status: ConnectivityStatusEnum) => {
    const variants: Record<ConnectivityStatusEnum, { className: string; label: string }> = {
      [ConnectivityStatusEnum.Served]: {
        className: "bg-green-100 text-green-700 hover:bg-green-100",
        label: "Served",
      },
      [ConnectivityStatusEnum.Underserved]: {
        className: "bg-orange-100 text-orange-700 hover:bg-orange-100",
        label: "Underserved",
      },
      [ConnectivityStatusEnum.Unserved]: {
        className: "bg-red-100 text-red-700 hover:bg-red-100",
        label: "Unserved",
      },
    };
    const config = variants[status];
    if (!config) return <Badge variant="outline">Unknown ({String(status)})</Badge>;
    return <Badge className={config.className}>{config.label}</Badge>;
  };

  const getScoreLevel = (score: number) => {
    if (score >= 80) return { label: "Excellent", className: "bg-green-100 text-green-700" };
    if (score >= 60) return { label: "Good", className: "bg-blue-100 text-blue-700" };
    if (score >= 40) return { label: "Moderate", className: "bg-yellow-100 text-yellow-700" };
    return { label: "Fair", className: "bg-orange-100 text-orange-700" };
  };

  // Helper function to format population display
  const formatPopulation = (population: number | null | undefined): string => {
    if (population === null || population === undefined || population === 0) {
      return "N/A";
    }
    return population.toLocaleString();
  };

  const getMunicipalityPopulation = (municipalityName: string, population: number | null | undefined): number => {
    // If population is provided and > 0, use it
    if (population && population > 0) {
      return population;
    }
    
    // Otherwise return 0 (will be formatted as "Undetermined" by formatPopulation)
    return 0;
  };

  const isActionDisabled = isLoading || downloadingFormat !== null || regions.length === 0 || regionId === "all";

  const toggleRegion = (regionName: string) => {
    const newSet = new Set(expandedRegions);
    if (newSet.has(regionName)) {
      newSet.delete(regionName);
    } else {
      newSet.add(regionName);
    }
    setExpandedRegions(newSet);
  };

  const toggleProvince = (provinceName: string) => {
    const newSet = new Set(expandedProvinces);
    if (newSet.has(provinceName)) {
      newSet.delete(provinceName);
    } else {
      newSet.add(provinceName);
    }
    setExpandedProvinces(newSet);
  };

  const toggleMunicipality = (municipalityName: string) => {
    const newSet = new Set(expandedMunicipalities);
    if (newSet.has(municipalityName)) {
      newSet.delete(municipalityName);
    } else {
      newSet.add(municipalityName);
    }
    setExpandedMunicipalities(newSet);
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Activity className="h-5 w-5 text-blue-600" />
          <CardTitle>Connectivity &amp; Gap Report</CardTitle>
        </div>
        <CardDescription>
          Unified connectivity assessment with quality scores, infrastructure analysis, and gap identification
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Filters */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="space-y-2">
            <Label htmlFor="cag-region">Region</Label>
            {regions.length === 0 ? (
              <div className="flex h-10 w-full items-center justify-center rounded-md border border-input bg-muted px-3 py-2 text-sm text-muted-foreground">
                No region covered
              </div>
            ) : (
              <Select
                value={regionId}
                disabled={!!regionParam && !!currentRegion}
                onValueChange={(value) => {
                  setRegionId(value);
                  setProvinceId("all");
                  setMunicipalityId("all");
                  setPageNumber(1);
                }}>
                <SelectTrigger id="cag-region" disabled={!!regionParam && !!currentRegion}>
                  <SelectValue placeholder={regionParam && currentRegion ? currentRegion.name : "Select region"} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Regions</SelectItem>
                  {regions.map((region) => (
                    <SelectItem key={region.id} value={region.id}>
                      {region.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="cag-province">Province</Label>
            <Select
              value={provinceId}
              onValueChange={(value) => {
                setProvinceId(value);
                setMunicipalityId("all");
                setPageNumber(1);
              }}
              disabled={regionId === "all"}>
              <SelectTrigger id="cag-province">
                <SelectValue placeholder="Select province" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Provinces</SelectItem>
                {provinces.map((province) => (
                  <SelectItem key={province.id} value={province.id}>
                    {province.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="cag-municipality">Municipality</Label>
            <Select
              value={municipalityId}
              onValueChange={(value) => {
                setMunicipalityId(value);
                setPageNumber(1);
              }}
              disabled={provinceId === "all"}>
              <SelectTrigger id="cag-municipality">
                <SelectValue placeholder="Select municipality" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Municipalities</SelectItem>
                {municipalities.map((municipality) => (
                  <SelectItem key={municipality.id} value={municipality.id}>
                    {municipality.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="cag-status">Filter by Status</Label>
            <Select
              value={filterByStatus}
              onValueChange={(value) => {
                setFilterByStatus(value);
                setPageNumber(1);
              }}>
              <SelectTrigger id="cag-status">
                <SelectValue placeholder="All statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value={ConnectivityStatusEnum.Served}>Served</SelectItem>
                <SelectItem value={ConnectivityStatusEnum.Underserved}>Underserved</SelectItem>
                <SelectItem value={ConnectivityStatusEnum.Unserved}>Unserved</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row justify-end gap-2">
          <Button variant="secondary" onClick={handleLoadReport} disabled={isActionDisabled}>
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Loading...
              </>
            ) : (
              <>
                <Activity className="mr-2 h-4 w-4" />
                Load Report
              </>
            )}
          </Button>

          <Button
            variant="outline"
            onClick={() => handleDownload(ReportFormat.Pdf)}
            disabled={isActionDisabled}>
            {downloadingFormat === ReportFormat.Pdf ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Downloading...
              </>
            ) : (
              <>
                <FileText className="mr-2 h-4 w-4" />
                Export PDF
              </>
            )}
          </Button>
          <Button onClick={() => handleDownload(ReportFormat.Excel)} disabled={isActionDisabled}>
            {downloadingFormat === ReportFormat.Excel ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Downloading...
              </>
            ) : (
              <>
                <Download className="mr-2 h-4 w-4" />
                Export Excel
              </>
            )}
          </Button>
        </div>

        {/* Loading indicator */}
        {isLoading && (
          <div className="space-y-2">
            <Progress value={undefined} className="h-2" />
            <p className="text-sm text-muted-foreground text-center">
              Loading connectivity data... This may take a few moments.
            </p>
          </div>
        )}

        {/* Report Tabs */}
        {!isLoading && connectivityData && (
          <Tabs 
            defaultValue="summary" 
            className="w-full"
            onValueChange={(value) => {
              if (value === "breakdown" && pageNumber !== 1) {
                setPageNumber(1);
              }
            }}>
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="summary">Summary</TabsTrigger>
              <TabsTrigger value="breakdown">Breakdown</TabsTrigger>
            </TabsList>

            {/* Summary Tab */}
            <TabsContent value="summary" className="space-y-4">
              {/* Main Summary Layout */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Left: Connectivity Overview */}
                <Card className="bg-white border-gray-200">
                  <CardHeader className="pb-3 border-b">
                    <CardTitle className="text-xs font-bold text-gray-900 uppercase tracking-widest">
                      Connectivity Overview — {currentRegion?.name || "Region X"} 
                      {currentProvince && `(${currentProvince.name})`}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-6 pt-4">
                    {/* Top Summary */}
                    <div className="space-y-3">
                      <div className="flex items-baseline justify-between">
                        <div>
                          <p className="text-sm text-gray-600">barangays assessed</p>
                          <p className="text-3xl font-bold text-gray-900">
                            {connectivityData.totalBarangays || 0}
                          </p>
                        </div>
                        <div className="text-right space-y-3">
                          <div>
                            <p className="text-xs text-gray-600">Aggregated Status:</p>
                            <p className={`text-lg font-bold ${
                              connectivityData.regionSummary?.overallConnectivityStatus === ConnectivityStatusEnum.Served
                                ? "text-green-600"
                                : connectivityData.regionSummary?.overallConnectivityStatus === ConnectivityStatusEnum.Underserved
                                  ? "text-orange-600"
                                  : "text-red-600"
                            }`}>
                              {connectivityData.regionSummary?.overallConnectivityStatus || "Unknown"}
                            </p>
                          </div>
                          <div>
                            <p className="text-xs text-gray-600">Assessed Status:</p>
                            <p className={`text-lg font-bold ${
                              connectivityData.regionSummary?.assessedConnectivityStatus === ConnectivityStatusEnum.Served
                                ? "text-green-600"
                                : connectivityData.regionSummary?.assessedConnectivityStatus === ConnectivityStatusEnum.Underserved
                                  ? "text-orange-600"
                                  : "text-red-600"
                            }`}>
                              {connectivityData.regionSummary?.assessedConnectivityStatus || "Unknown"}
                            </p>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Status Breakdown */}
                    <div className="space-y-4">
                      {/* Served */}
                      <div className="space-y-1">
                        <div className="flex items-center justify-between">
                          <div className="flex items-baseline gap-2">
                            <span className="text-xs font-semibold text-green-700">Served</span>
                            <div className="text-xs text-gray-600">Score 11–15 · fully connected</div>
                          </div>
                          <div className="text-right">
                            <div className="text-xs text-gray-600">Pop. {(connectivityData.regionSummary?.servedPopulation || 0).toLocaleString()}</div>
                            <div className="text-lg font-bold text-green-700">{connectivityData.regionSummary?.servedBarangays}</div>
                          </div>
                        </div>
                        <div className="w-full bg-gray-200 h-1 rounded-full overflow-hidden">
                          <div
                            className="bg-green-500 h-full rounded-full"
                            style={{
                              width: `${
                                connectivityData.totalBarangays > 0
                                  ? (connectivityData.regionSummary?.servedBarangays || 0) / connectivityData.totalBarangays * 100
                                  : 0
                              }%`,
                            }}
                          />
                        </div>
                      </div>

                      {/* Underserved */}
                      <div className="space-y-1">
                        <div className="flex items-center justify-between">
                          <div className="flex items-baseline gap-2">
                            <span className="text-xs font-semibold text-orange-700">Underserved</span>
                            <div className="text-xs text-gray-600">Score 6–10 · partial access</div>
                          </div>
                          <div className="text-right">
                            <div className="text-xs text-gray-600">Pop. {(connectivityData.regionSummary?.underservedPopulation || 0).toLocaleString()}</div>
                            <div className="text-lg font-bold text-orange-700">{connectivityData.regionSummary?.underservedBarangays}</div>
                          </div>
                        </div>
                        <div className="w-full bg-gray-200 h-1 rounded-full overflow-hidden">
                          <div
                            className="bg-orange-400 h-full rounded-full"
                            style={{
                              width: `${
                                connectivityData.totalBarangays > 0
                                  ? (connectivityData.regionSummary?.underservedBarangays || 0) / connectivityData.totalBarangays * 100
                                  : 0
                              }%`,
                            }}
                          />
                        </div>
                      </div>

                      {/* Unserved */}
                      <div className="space-y-1">
                        <div className="flex items-center justify-between">
                          <div className="flex items-baseline gap-2">
                            <span className="text-xs font-semibold text-red-700">Unserved</span>
                            <div className="text-xs text-gray-600">Score 5 · no infrastructure</div>
                          </div>
                          <div className="text-right">
                            <div className="text-xs text-gray-600">Pop. {(connectivityData.regionSummary?.unservedPopulation || 0).toLocaleString()}</div>
                            <div className="text-lg font-bold text-red-700">{connectivityData.regionSummary?.unservedBarangays}</div>
                          </div>
                        </div>
                        <div className="w-full bg-gray-200 h-1 rounded-full overflow-hidden">
                          <div
                            className="bg-red-500 h-full rounded-full"
                            style={{
                              width: `${
                                connectivityData.totalBarangays > 0
                                  ? (connectivityData.regionSummary?.unservedBarangays || 0) / connectivityData.totalBarangays * 100
                                  : 0
                              }%`,
                            }}
                          />
                        </div>
                      </div>
                    </div>

                    {/* Infrastructure */}
                    <div className="border-t pt-4">
                      <p className="text-xs font-bold text-gray-900 uppercase tracking-widest mb-4">
                        Infrastructure
                      </p>
                      <div className="grid grid-cols-3 gap-4 text-center">
                        <div>
                          <p className="text-2xl font-bold text-gray-900">
                            {connectivityData.scoreSummary.totalCellTowers}
                          </p>
                          <p className="text-xs text-gray-600 mt-1">Cell towers</p>
                        </div>
                        <div>
                          <p className="text-2xl font-bold text-gray-900">
                            {connectivityData.scoreSummary.totalFiberNAPs}
                          </p>
                          <p className="text-xs text-gray-600 mt-1">Fiber NAPs</p>
                        </div>
                        <div>
                          <p className="text-2xl font-bold text-gray-900">
                            {connectivityData.scoreSummary.totalServiceProviders}
                          </p>
                          <p className="text-xs text-gray-600 mt-1">Service providers</p>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>

                {/* Right: Rubric Scores & Gap Indicators */}
                <Card className="bg-white border-gray-200">
                  <CardHeader className="pb-3 border-b">
                    <CardTitle className="text-xs font-bold text-gray-900 uppercase tracking-widest">
                      Avg Rubric Scores (OUT OF 3)
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4 pt-4">
                    {/* Score Items with Progress Bars */}
                    <div className="space-y-4">
                      {/* Technology Type */}
                      <div>
                        <div className="flex justify-between items-baseline mb-1">
                          <span className="text-sm text-gray-900 font-medium">Technology type</span>
                          <span className="text-sm font-bold text-gray-900">
                            {connectivityData.scoreSummary.averageTechnologyTypeScore.toFixed(2)}
                          </span>
                        </div>
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className="bg-blue-500 h-full rounded-full"
                            style={{
                              width: `${(connectivityData.scoreSummary.averageTechnologyTypeScore / 3) * 100}%`,
                            }}
                          />
                        </div>
                      </div>

                      {/* Fiber Coverage */}
                      <div>
                        <div className="flex justify-between items-baseline mb-1">
                          <span className="text-sm text-gray-900 font-medium">Fiber coverage</span>
                          <span className="text-sm font-bold text-gray-900">
                            {connectivityData.scoreSummary.averageFiberCoverageScore.toFixed(2)}
                          </span>
                        </div>
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className="bg-blue-500 h-full rounded-full"
                            style={{
                              width: `${(connectivityData.scoreSummary.averageFiberCoverageScore / 3) * 100}%`,
                            }}
                          />
                        </div>
                      </div>

                      {/* Coverage Availability */}
                      <div>
                        <div className="flex justify-between items-baseline mb-1">
                          <span className="text-sm text-gray-900 font-medium">Coverage avail.</span>
                          <span className="text-sm font-bold text-gray-900">
                            {connectivityData.scoreSummary.averageCoverageAvailabilityScore.toFixed(2)}
                          </span>
                        </div>
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className="bg-blue-500 h-full rounded-full"
                            style={{
                              width: `${(connectivityData.scoreSummary.averageCoverageAvailabilityScore / 3) * 100}%`,
                            }}
                          />
                        </div>
                      </div>

                      {/* Quality of Service */}
                      <div>
                        <div className="flex justify-between items-baseline mb-1">
                          <span className="text-sm text-gray-900 font-medium">Quality of service</span>
                          <span className="text-sm font-bold text-gray-900">
                            {connectivityData.scoreSummary.averageQualityOfServiceScore.toFixed(2)}
                          </span>
                        </div>
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className="bg-orange-500 h-full rounded-full"
                            style={{
                              width: `${(connectivityData.scoreSummary.averageQualityOfServiceScore / 3) * 100}%`,
                            }}
                          />
                        </div>
                      </div>

                      {/* Internet Access */}
                      <div>
                        <div className="flex justify-between items-baseline mb-1">
                          <span className="text-sm text-gray-900 font-medium">Internet access</span>
                          <span className="text-sm font-bold text-gray-900">
                            {connectivityData.scoreSummary.averageInternetAccessScore.toFixed(2)}
                          </span>
                        </div>
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className="bg-orange-500 h-full rounded-full"
                            style={{
                              width: `${(connectivityData.scoreSummary.averageInternetAccessScore / 3) * 100}%`,
                            }}
                          />
                        </div>
                      </div>
                    </div>

                    {/* Average Total Score */}
                    <div className="border-t pt-4">
                      <p className="text-xs text-gray-600 text-center mb-2">Average total</p>
                      <p className="text-4xl font-bold text-center text-gray-900">
                        {connectivityData.scoreSummary.averageTotalScore.toFixed(2)}
                        <span className="text-lg text-gray-600 ml-1">/15</span>
                      </p>
                    </div>

                    {/* Gap Indicators */}
                    <div className="border-t pt-4 space-y-3">
                      <p className="text-xs font-bold text-gray-900 uppercase tracking-widest">
                        Gap Indicators
                      </p>
                      <div className="space-y-2 text-sm">
                        <div className="flex justify-between items-center">
                          <span className="text-gray-700">Internet access deficit</span>
                          <span className="font-medium text-red-600">-1.4 vs tech</span>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-gray-700">QoS deficit</span>
                          <span className="font-medium text-red-600">-1.35 vs tech</span>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-gray-700">Provinces fully unserved</span>
                          <span className="font-medium text-red-600">1 of 5</span>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            {/* Breakdown Tab */}
            <TabsContent value="breakdown" className="space-y-4">
              {/* Provincial Level Display with Area Details */}
              <div className="space-y-3 max-h-[800px] overflow-y-auto">
                {connectivityData.provinceSummaries && connectivityData.provinceSummaries.length > 0 ? (
                  connectivityData.provinceSummaries.map((province) => {
                    const municipalitiesInProvince = connectivityData.municipalities?.filter(
                      (m) => m.provinceName === province.name
                    ) || [];

                    return (
                      <Card key={province.name} className="border bg-white overflow-hidden">
                        {/* Province Header */}
                        <div
                          className="p-4 bg-gradient-to-r from-gray-50 to-gray-100 cursor-pointer hover:from-gray-100 hover:to-gray-150 transition-colors border-b"
                          onClick={() => toggleProvince(province.name)}>
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              {expandedProvinces.has(province.name) ? (
                                <ChevronDown className="h-5 w-5 text-gray-600" />
                              ) : (
                                <ChevronRight className="h-5 w-5 text-gray-600" />
                              )}
                              <div>
                                <h3 className="font-semibold text-gray-900">{province.name}</h3>
                                <p className="text-xs text-gray-600 mt-1">
                                  {province.totalBarangays} barangays • {formatPopulation(province.totalPopulation)} population
                                </p>
                              </div>
                            </div>
                            <div className="flex gap-3 items-center">
                              <div className="text-right">
                                <div className="flex gap-1 text-xs font-semibold">
                                  <span className="text-green-700">{province.servedBarangays}</span>
                                  <span className="text-gray-400">/</span>
                                  <span className="text-orange-700">{province.underservedBarangays}</span>
                                  <span className="text-gray-400">/</span>
                                  <span className="text-red-700">{province.unservedBarangays}</span>
                                </div>
                                <p className="text-xs text-gray-500 mt-1">S/UD/US barangays</p>
                              </div>
                            </div>
                          </div>
                        </div>

                        {/* Province Content */}
                        {expandedProvinces.has(province.name) && (
                          <div className="p-4">
                            {municipalitiesInProvince.length > 0 ? (
                              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                                {municipalitiesInProvince.map((municipality) => {
                                  return (
                                    <div
                                      key={municipality.municipalityId}
                                      className="border rounded-lg p-3 bg-gradient-to-br from-white to-gray-50 hover:from-gray-50 hover:to-gray-100 transition-colors">
                                      {/* Municipality Header */}
                                      <div
                                        className="cursor-pointer"
                                        onClick={() => toggleMunicipality(municipality.municipalityName)}>
                                        <div className="flex items-start justify-between gap-2 mb-3">
                                          <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-2">
                                              {expandedMunicipalities.has(municipality.municipalityName) ? (
                                                <ChevronDown className="h-4 w-4 text-gray-600 flex-shrink-0" />
                                              ) : (
                                                <ChevronRight className="h-4 w-4 text-gray-600 flex-shrink-0" />
                                              )}
                                              <h4 className="font-medium text-sm text-gray-900 truncate">
                                                {municipality.municipalityName}
                                              </h4>
                                            </div>
                                            <p className="text-xs text-gray-600 mt-2 ml-6">
                                              {municipality.totalBarangays} barangays • {formatPopulation(getMunicipalityPopulation(municipality.municipalityName, municipality.totalPopulation))} pop.
                                            </p>
                                          </div>
                                          <div className="flex flex-col items-end gap-1 flex-shrink-0">
                                            <div className="flex gap-0.5 text-xs font-semibold">
                                              <span className="text-green-700 w-5 text-center">
                                                {municipality.servedBarangays}
                                              </span>
                                              <span className="text-orange-700 w-5 text-center">
                                                {municipality.underservedBarangays}
                                              </span>
                                              <span className="text-red-700 w-5 text-center">
                                                {municipality.unservedBarangays}
                                              </span>
                                            </div>
                                            <p className="text-xs text-gray-500">S/UD/US</p>
                                          </div>
                                        </div>
                                      </div>

                                      {/* Municipality Details - Expanded View */}
                                      {expandedMunicipalities.has(municipality.municipalityName) && (
                                        <div className="mt-3 pt-3 border-t space-y-2">
                                          {municipality.barangays && municipality.barangays.length > 0 ? (
                                            <div className="space-y-3">
                                              {municipality.barangays.map((barangay) => (
                                                <div key={barangay.locationId} className="p-3 bg-gray-50 rounded text-xs border-l-2 border-gray-200">
                                                  <div className="flex items-start justify-between gap-3 mb-2">
                                                    <div className="flex-1 min-w-0">
                                                      <p className="font-semibold text-gray-900 truncate">{barangay.locationName}</p>
                                                      <p className="text-gray-600 mt-0.5">Pop: {formatPopulation(barangay.population)}</p>
                                                    </div>
                                                    <div className="flex-shrink-0">
                                                      {getStatusBadge(barangay.connectivityStatus)}
                                                    </div>
                                                  </div>
                                                  
                                                  {/* Infrastructure & Service Info */}
                                                  <div className="grid grid-cols-2 gap-2 mt-2 pt-2 border-t border-gray-200">
                                                    {barangay.fiberNAPCount > 0 && (
                                                      <div><span className="text-gray-600">Fiber NAPs:</span> <span className="font-semibold text-blue-700">{barangay.fiberNAPCount}</span></div>
                                                    )}
                                                    {barangay.cellTowerCount > 0 && (
                                                      <div><span className="text-gray-600">Towers:</span> <span className="font-semibold text-blue-700">{barangay.cellTowerCount}</span></div>
                                                    )}
                                                    {barangay.wifiHotspotCount > 0 && (
                                                      <div><span className="text-gray-600">WiFi:</span> <span className="font-semibold text-blue-700">{barangay.wifiHotspotCount}</span></div>
                                                    )}
                                                    {barangay.serviceProviderCount > 0 && (
                                                      <div><span className="text-gray-600">Providers:</span> <span className="font-semibold text-blue-700">{barangay.serviceProviderCount}</span></div>
                                                    )}
                                                  </div>
                                                  
                                                  {/* Quality Scores */}
                                                  {barangay.totalScore > 0 && (
                                                    <div className="mt-2 pt-2 border-t border-gray-200">
                                                      <div className="flex items-center justify-between">
                                                        <span className="text-gray-600">Score:</span>
                                                        <span className="font-semibold">{barangay.totalScore}</span>
                                                      </div>
                                                    </div>
                                                  )}
                                                </div>
                                              ))}
                                            </div>
                                          ) : (
                                            <p className="text-xs text-gray-600 italic">No barangays data available</p>
                                          )}
                                        </div>
                                      )}
                                    </div>
                                  );
                                })}
                              </div>
                            ) : (
                              <div className="text-center py-6 text-muted-foreground">
                                <p className="text-sm">No municipalities in this province</p>
                              </div>
                            )}
                          </div>
                        )}
                      </Card>
                    );
                  })
                ) : (
                  <div className="text-center py-12 text-muted-foreground">
                    <Activity className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>No areas found for the selected filters</p>
                  </div>
                )}
              </div>

              {/* Pagination - Only for Summary Tab */}
              {connectivityData.totalPages > 1 && (
                <div className="flex items-center justify-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                    disabled={pageNumber === 1 || isLoading}>
                    Previous
                  </Button>
                  <span className="text-sm text-muted-foreground">
                    Page {connectivityData.pageNumber} of {connectivityData.totalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber((p) => p + 1)}
                    disabled={pageNumber >= connectivityData.totalPages || isLoading}>
                    Next
                  </Button>
                </div>
              )}
            </TabsContent>
          </Tabs>
        )}

        {/* Status Explanation Footnote */}
        {!isLoading && connectivityData && (
          <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-md">
            <p className="text-xs text-blue-900">
              <span className="font-semibold">Status Definitions:</span> <br />
              <span className="inline-block mt-2"><strong>Aggregated Status</strong> is derived from the average CMTS score of all child barangay assessments.</span><br />
              <span className="inline-block mt-1"><strong>Assessed Status</strong> is the pre-computed status from the direct infrastructure assessment of this geographic unit. Differences between the two may occur when assessments were conducted at different times.</span>
            </p>
          </div>
        )}

        {/* Generated At */}
        {!isLoading && connectivityData && (
          <div className="text-xs text-muted-foreground text-center">
            Generated at: {new Date(connectivityData.generatedAt).toLocaleString()}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
