"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { AlertTriangle, Download, FileText, Loader2 } from "lucide-react";

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
import { getConnectivityGapReportRequest } from "../handlers/getConnectivityGapReportRequest";
import { ConnectivityStatusEnum, ReportFormat } from "../types/Report";
import { determineReportScope, generateReportFilename } from "../utils/generateReportFilename";

const useConnectivityGapReport = createQueryHookFromTemplate(getConnectivityGapReportRequest);
const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);

export function ConnectivityGapReport() {
  const { region: regionParam } = useParams<{ region?: string }>();
  const [regionId, setRegionId] = useState<string>(() => regionParam ?? "all");
  const [provinceId, setProvinceId] = useState<string>("all");
  const [municipalityId, setMunicipalityId] = useState<string>("all");
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [downloadingFormat, setDownloadingFormat] = useState<ReportFormat | null>(null);
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
      isWirelessCovered: true
    }
  });

  const { data: provincesData } = useProvinces({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      regionIds: regionId !== "all" && regionId ? [regionId] : [],
      isWiredCovered: true,
      isWirelessCovered: true
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
  } = useConnectivityGapReport({
    pathParams: {},
    queryParams: {
      regionId: regionId !== "all" ? regionId : undefined,
      provinceId: provinceId !== "all" ? provinceId : undefined,
      municipalityId: municipalityId !== "all" ? municipalityId : undefined,
      pageNumber,
      pageSize,
    },
    enabled: false, // Disable automatic fetching to prevent crashes on mount
  });

  const reportData = reportResponse?.data;
  const regions = regionsData?.data || [];
  const provinces = provincesData?.data || [];
  const municipalities = municipalitiesData?.data || [];
  const currentRegion = regions.find((r) => r.id === regionId);
  const currentProvince = provinces.find((p) => p.id === provinceId);
  const currentMunicipality = municipalities.find((m) => m.id === municipalityId);

  // Auto-fetch when page number changes
  useEffect(() => {
    if (reportData) {
      refetch();
    }
  }, [pageNumber]);

  const handleDownload = async (format: ReportFormat) => {
    setDownloadingFormat(format);
    try {
      const params = new URLSearchParams({
        format: format,
        ...(regionId !== "all" && { regionId }),
        ...(provinceId !== "all" && { provinceId }),
        ...(municipalityId !== "all" && { municipalityId }),
      });

      const backendUrl = getBackendUrl();
      const response = await fetch(`${backendUrl}/reports/connectivity-gap/download?${params.toString()}`, {
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
      
      // Generate filename based on report type, scope, and location
      const { scope, locationName } = determineReportScope(
        regionId,
        provinceId,
        municipalityId,
        currentRegion?.name,
        currentProvince?.name,
        currentMunicipality?.name
      );
      
      const filename = generateReportFilename({
        reportType: 'ConnectivityGap',
        scope,
        locationName,
        extension: format === ReportFormat.Excel ? 'xlsx' : 'pdf'
      });
      
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error("Download error:", error);
      // You could add a toast notification here to inform the user
    } finally {
      setDownloadingFormat(null);
    }
  };

  const handleExportPdf = () => handleDownload(ReportFormat.Pdf);
  const handleExportExcel = () => handleDownload(ReportFormat.Excel);

  const getStatusBadge = (status: ConnectivityStatusEnum) => {
    // Log to debug what value we're actually receiving
    if (!status || !(status in ConnectivityStatusEnum)) {
      console.warn("Invalid or missing connectivityStatus:", status);
    }

    const variants: Record<
      ConnectivityStatusEnum,
      { className: string; label: string }
    > = {
      [ConnectivityStatusEnum.Served]: { className: "bg-green-100 text-green-700 hover:bg-green-100", label: "Served" },
      [ConnectivityStatusEnum.Underserved]: { className: "bg-orange-100 text-orange-700 hover:bg-orange-100", label: "Underserved" },
      [ConnectivityStatusEnum.Unserved]: { className: "bg-red-100 text-red-700 hover:bg-red-100", label: "Unserved" },
    };
    const config = variants[status];
    if (!config) {
      return <Badge variant="outline">Unknown ({String(status)})</Badge>;
    }
    return <Badge className={config.className}>{config.label}</Badge>;
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-orange-600" />
          <CardTitle>Connectivity Gap Report</CardTitle>
        </div>
        <CardDescription>Identify priority areas with connectivity gaps and underserved populations</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div className="space-y-2">
            <Label htmlFor="gap-region">Region</Label>
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
                <SelectTrigger id="gap-region" disabled={!!regionParam && !!currentRegion}>
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
            <Label htmlFor="gap-province">Province</Label>
            <Select
              value={provinceId}
              onValueChange={(value) => {
                setProvinceId(value);
                setMunicipalityId("all");
                setPageNumber(1);
              }}
              disabled={regionId === "all"}>
              <SelectTrigger id="gap-province">
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
            <Label htmlFor="gap-municipality">Municipality</Label>
            <Select
              value={municipalityId}
              onValueChange={(value) => {
                setMunicipalityId(value);
                setPageNumber(1);
              }}
              disabled={provinceId === "all"}>
              <SelectTrigger id="gap-municipality">
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
        </div>

        <div className="flex flex-col sm:flex-row justify-end gap-2">
          <Button
            variant="secondary"
            onClick={() => refetch()}
            disabled={isLoading || downloadingFormat !== null || regions.length === 0 || regionId === "all"}>
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Loading...
              </>
            ) : (
              <>
                <AlertTriangle className="mr-2 h-4 w-4" />
                Load Report
              </>
            )}
          </Button>
          <Button
            variant="outline"
            onClick={handleExportPdf}
            disabled={isLoading || downloadingFormat !== null || regions.length === 0 || regionId === "all"}>
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
          <Button
            onClick={handleExportExcel}
            disabled={isLoading || downloadingFormat !== null || regions.length === 0 || regionId === "all"}>
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

        {(isLoading || downloadingFormat !== null) && (
          <div className="space-y-2">
            <Progress value={undefined} className="h-2" />
            <p className="text-sm text-muted-foreground text-center">
              {downloadingFormat !== null
                ? "Downloading report... This may take a few moments."
                : "Loading connectivity gap data... This may take a few moments."}
            </p>
          </div>
        )}

        {!isLoading && reportData && (
          <Tabs defaultValue="summary" className="w-full">
            <TabsList>
              <TabsTrigger value="summary">Summary</TabsTrigger>
              <TabsTrigger value="priority-areas">Priority Areas</TabsTrigger>
            </TabsList>

            <TabsContent value="summary" className="space-y-4">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <Card className="border-red-200 bg-red-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-red-700">Unserved Areas</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-red-900">{reportData.summary.totalUnservedAreas}</div>
                  </CardContent>
                </Card>

                <Card className="border-orange-200 bg-orange-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-orange-700">Underserved Areas</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-orange-900">{reportData.summary.totalUnderservedAreas}</div>
                  </CardContent>
                </Card>

                <Card className="border-red-200 bg-red-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-red-700">Unserved Population</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-red-900">
                      {reportData.summary.unservedPopulation.toLocaleString()}
                    </div>
                  </CardContent>
                </Card>

                <Card className="border-orange-200 bg-orange-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-orange-700">Underserved Population</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-orange-900">
                      {reportData.summary.underservedPopulation.toLocaleString()}
                    </div>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm font-medium">Total Priority Areas</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{reportData.totalPriorityAreas}</div>
                  <p className="text-xs text-muted-foreground mt-1">
                    Areas requiring immediate attention
                  </p>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="priority-areas" className="space-y-4">
              <div className="space-y-3">
                {reportData.priorityAreas && reportData.priorityAreas.length > 0 ? (
                  reportData.priorityAreas.map((area) => {
                    // Format complete location name
                    const locationParts = [area.locationName];
                    if (area.municipalityName) locationParts.push(area.municipalityName);
                    if (area.provinceName) locationParts.push(area.provinceName);
                    const fullLocationName = locationParts.filter(Boolean).join(", ");
                    
                    return (
                    <Card key={area.locationId}>
                      <CardContent className="pt-6">
                        <div className="flex items-center justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-2">
                              <h4 className="font-medium text-lg">{fullLocationName}</h4>
                              {getStatusBadge(area.connectivityStatus)}
                            </div>
                            <div className="flex gap-4 mt-2 text-sm text-muted-foreground">
                              <span>Population: {area.population?.toLocaleString() || "N/A"}</span>
                              <span>Priority Score: {area.totalScore}</span>
                              <span>Last Assessed: {new Date(area.lastAssessed).toLocaleDateString()}</span>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  )})
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <AlertTriangle className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>No priority areas identified for the selected filters</p>
                  </div>
                )}
              </div>

              {reportData.totalPages > 1 && (
                <div className="flex items-center justify-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                    disabled={pageNumber === 1 || isLoading}>
                    Previous
                  </Button>
                  <span className="text-sm text-muted-foreground">
                    Page {reportData.pageNumber} of {reportData.totalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber((p) => p + 1)}
                    disabled={pageNumber >= reportData.totalPages || isLoading}>
                    Next
                  </Button>
                </div>
              )}
            </TabsContent>
          </Tabs>
        )}

        {!isLoading && reportData && (
          <div className="text-xs text-muted-foreground text-center">
            Generated at: {new Date(reportData.generatedAt).toLocaleString()}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
