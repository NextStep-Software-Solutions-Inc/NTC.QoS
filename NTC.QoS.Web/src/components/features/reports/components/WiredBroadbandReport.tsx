"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Download, FileText, Loader2 } from "lucide-react";

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
import { getWiredBroadbandReportRequest } from "../handlers/getWiredBroadbandReportRequest";
import { ReportFormat, ServiceStatusEnum } from "../types/Report";
import { determineReportScope, generateReportFilename } from "../utils/generateReportFilename";

const useWiredBroadbandReport = createQueryHookFromTemplate(getWiredBroadbandReportRequest);
const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);

export function WiredBroadbandReport() {
  const { region: regionParam } = useParams<{ region?: string }>();
  const [regionId, setRegionId] = useState<string>(() => regionParam ?? "all");
  const [provinceId, setProvinceId] = useState<string>("all");
  const [municipalityId, setMunicipalityId] = useState<string>("all");
  const [filterByStatus, setFilterByStatus] = useState<string>("all");
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [downloadingFormat, setDownloadingFormat] = useState<ReportFormat | null>(null);
  const [expandedMunicipalities, setExpandedMunicipalities] = useState<Set<string>>(new Set());
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
    }
  });

  const { data: provincesData } = useProvinces({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      regionIds: regionId !== "all" && regionId ? [regionId] : [],
      isWiredCovered: true,
    },
  });

  const { data: municipalitiesData } = useMunicipalities({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      provinceId: provinceId !== "all" ? provinceId : undefined,
      isWiredCovered: true,
    },
  });

  const {
    data: reportResponse,
    isLoading,
    refetch,
  } = useWiredBroadbandReport({
    pathParams: {},
    queryParams: {
      regionId: regionId !== "all" ? regionId : undefined,
      provinceId: provinceId !== "all" ? provinceId : undefined,
      municipalityId: municipalityId !== "all" ? municipalityId : undefined,
      filterByStatus: filterByStatus !== "all" ? (filterByStatus as ServiceStatusEnum) : undefined,
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

  // Auto-fetch data when page number changes (but not on initial render)
  useEffect(() => {
    if (reportData && pageNumber !== reportData.pageNumber) {
      refetch();
    }
  }, [pageNumber]);

  const toggleMunicipality = (municipalityId: string) => {
    setExpandedMunicipalities(prev => {
      const newSet = new Set(prev);
      if (newSet.has(municipalityId)) {
        newSet.delete(municipalityId);
      } else {
        newSet.add(municipalityId);
      }
      return newSet;
    });
  };

  const handleDownload = async (format: ReportFormat) => {
    setDownloadingFormat(format);
    try {
      const params = new URLSearchParams({
        format: format,
        ...(regionId !== "all" && { regionId }),
        ...(provinceId !== "all" && { provinceId }),
        ...(municipalityId !== "all" && { municipalityId }),
        ...(filterByStatus !== "all" && { filterByStatus }),
      });

      const backendUrl = getBackendUrl();
      const response = await fetch(`${backendUrl}/reports/wired-broadband/download?${params.toString()}`, {
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
        reportType: 'WiredBroadband',
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

  const getStatusBadge = (status: ServiceStatusEnum) => {
    // Log to debug what value we're actually receiving
    if (!status || !(status in ServiceStatusEnum)) {
      console.warn("Invalid or missing connectivityStatus:", status);
    }

    const variants: Record<
      ServiceStatusEnum,
      { className: string; label: string }
    > = {
      [ServiceStatusEnum.Served]: { className: "bg-green-100 text-green-700 hover:bg-green-100", label: "Served" },
      [ServiceStatusEnum.Underserved]: { className: "bg-orange-100 text-orange-700 hover:bg-orange-100", label: "Underserved" },
      [ServiceStatusEnum.Unserved]: { className: "bg-red-100 text-red-700 hover:bg-red-100", label: "Unserved" },
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
        <CardTitle>Wired Broadband Report</CardTitle>
        <CardDescription>
          View and export detailed wired broadband connectivity reports with barangay-level data
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="space-y-2">
            <Label htmlFor="wired-region">Region</Label>
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
                <SelectTrigger id="wired-region" disabled={!!regionParam && !!currentRegion}>
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
            <Label htmlFor="wired-province">Province</Label>
            <Select
              value={provinceId}
              onValueChange={(value) => {
                setProvinceId(value);
                setMunicipalityId("all");
                setPageNumber(1);
              }}
              disabled={regionId === "all"}>
              <SelectTrigger id="wired-province">
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
            <Label htmlFor="wired-municipality">Municipality</Label>
            <Select
              value={municipalityId}
              onValueChange={(value) => {
                setMunicipalityId(value);
                setPageNumber(1);
              }}
              disabled={provinceId === "all"}>
              <SelectTrigger id="wired-municipality">
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
            <Label htmlFor="wired-status">Filter by Status</Label>
            <Select
              value={filterByStatus}
              onValueChange={(value) => {
                setFilterByStatus(value);
                setPageNumber(1);
              }}>
              <SelectTrigger id="wired-status">
                <SelectValue placeholder="All statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value={ServiceStatusEnum.Served}>Served</SelectItem>
                <SelectItem value={ServiceStatusEnum.Underserved}>Underserved</SelectItem>
                <SelectItem value={ServiceStatusEnum.Unserved}>Unserved</SelectItem>
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
                <FileText className="mr-2 h-4 w-4" />
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
                : "Loading report data... This may take a few moments."}
            </p>
          </div>
        )}

        {!isLoading && reportData && (
          <Tabs defaultValue="summary" className="w-full">
            <TabsList>
              <TabsTrigger value="summary">Summary</TabsTrigger>
              <TabsTrigger value="municipalities">Municipalities</TabsTrigger>
            </TabsList>

            <TabsContent value="summary" className="space-y-4">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium">Total Municipalities</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{reportData.summary.totalMunicipalities}</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium">Total Barangays</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{reportData.summary.totalBarangays}</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-green-600">Served Barangays</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{reportData.summary.servedBarangays}</div>
                    <p className="text-xs text-muted-foreground">{reportData.summary.servedPercentage.toFixed(1)}%</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-yellow-600">Underserved Barangays</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{reportData.summary.underservedBarangays}</div>
                    <p className="text-xs text-muted-foreground">
                      {reportData.summary.underservedPercentage.toFixed(1)}%
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-red-600">Unserved Barangays</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{reportData.summary.unservedBarangays}</div>
                    <p className="text-xs text-muted-foreground">{reportData.summary.unservedPercentage.toFixed(1)}%</p>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm font-medium">Provider Coverage</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2">
                    {reportData.summary.providerCoverage &&
                      Object.entries(reportData.summary.providerCoverage).map(([provider, count]) => (
                        <div key={provider} className="flex justify-between items-center p-2 border rounded">
                          <span className="text-sm font-medium">{provider}</span>
                          <Badge variant="outline">{count}</Badge>
                        </div>
                      ))}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="municipalities" className="space-y-4">
              <div className="space-y-4">
                {reportData.municipalities?.map((municipality) => (
                  <Card key={municipality.id}>
                    <CardHeader>
                      <div className="flex justify-between items-center">
                        <CardTitle className="text-lg">{municipality.name}</CardTitle>
                        <Badge variant="outline">{municipality.code}</Badge>
                      </div>
                      <div className="flex gap-4 text-sm text-muted-foreground">
                        <span>Total: {municipality.totalBarangays}</span>
                        <span className="text-green-600">Served: {municipality.servedBarangays}</span>
                        <span className="text-yellow-600">Underserved: {municipality.underservedBarangays}</span>
                        <span className="text-red-600">Unserved: {municipality.unservedBarangays}</span>
                      </div>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
                        {municipality.barangays
                          ?.slice(0, expandedMunicipalities.has(municipality.id) ? undefined : 10)
                          .map((barangay) => (
                            <div
                              key={barangay.id}
                              className="flex justify-between items-center p-2 border rounded text-sm">
                              <span>{barangay.name}</span>
                              {getStatusBadge(barangay.serviceStatus)}
                            </div>
                          ))}
                      </div>
                      {municipality.barangays && municipality.barangays.length > 10 && (
                        <div className="mt-2 text-center">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => toggleMunicipality(municipality.id)}
                            className="text-xs">
                            {expandedMunicipalities.has(municipality.id) ? (
                              <>Show Less</>
                            ) : (
                              <>View All {municipality.barangays.length} Barangays</>
                            )}
                          </Button>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                ))}  
              </div>
            </TabsContent>
          </Tabs>
        )}

        {!isLoading && reportData && (
          <>
            {reportData.totalPages > 1 && (
              <div className="flex justify-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                  disabled={pageNumber === 1 || isLoading}>
                  Previous
                </Button>
                <span className="flex items-center px-4 text-sm">
                  Page {pageNumber} of {reportData.totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageNumber((p) => p + 1)}
                  disabled={!reportData.hasNextPage || isLoading}>
                  Next
                </Button>
              </div>
            )}

            <div className="text-xs text-muted-foreground text-center">
              Generated at: {new Date(reportData.summary.generatedAt).toLocaleString()}
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
