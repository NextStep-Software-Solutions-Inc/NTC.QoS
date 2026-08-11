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
import { getWirelessBroadbandReportRequest } from "../handlers/getWirelessBroadbandReportViewRequest";
import { ReportFormat, ServiceStatusEnum } from "../types/Report";
import { determineReportScope, generateReportFilename } from "../utils/generateReportFilename";

const useWirelessBroadbandReport = createQueryHookFromTemplate(getWirelessBroadbandReportRequest);
const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);

export function WirelessBroadbandReport() {
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
      isWirelessCovered: true
    }
  });

  const { data: provincesData } = useProvinces({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      regionIds: regionId !== "all" && regionId ? [regionId] : [],
      isWirelessCovered: true
    },
  });

  const { data: municipalitiesData } = useMunicipalities({
    pathParams: {},
    queryParams: {
      pageSize: 100,
      pageNumber: 1,
      provinceId: provinceId !== "all" ? provinceId : undefined,
      isWirelessCovered: true
    },
  });

  const {
    data: reportResponse,
    isLoading,
    refetch,
  } = useWirelessBroadbandReport({
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
      const response = await fetch(`${backendUrl}/reports/wireless-broadband/download?${params.toString()}`, {
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
        reportType: 'WirelessBroadband',
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
      return <Badge variant="outline">Unknown</Badge>;
    }
    return <Badge className={config.className}>{config.label}</Badge>;
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Wireless Broadband Report</CardTitle>
        <CardDescription>
          Generate and export wireless broadband coverage reports by region, province, or municipality
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="space-y-2">
            <Label htmlFor="region">Region</Label>
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
                <SelectTrigger id="region" disabled={!!regionParam && !!currentRegion}>
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
            <Label htmlFor="province">Province</Label>
            <Select
              value={provinceId}
              onValueChange={(value) => {
                setProvinceId(value);
                setMunicipalityId("all");
                setPageNumber(1);
              }}
              disabled={regionId === "all"}>
              <SelectTrigger id="province">
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
            <Label htmlFor="municipality">Municipality</Label>
            <Select
              value={municipalityId}
              onValueChange={(value) => {
                setMunicipalityId(value);
                setPageNumber(1);
              }}
              disabled={provinceId === "all"}>
              <SelectTrigger id="municipality">
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
            <Label htmlFor="wireless-status">Filter by Status</Label>
            <Select
              value={filterByStatus}
              onValueChange={(value) => {
                setFilterByStatus(value);
                setPageNumber(1);
              }}>
              <SelectTrigger id="wireless-status">
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
                    <CardDescription>Total Municipalities</CardDescription>
                    <CardTitle className="text-3xl">{reportData.summary.totalMunicipalities}</CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Total Barangays</CardDescription>
                    <CardTitle className="text-3xl">{reportData.summary.totalBarangays}</CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Avg Connectivity Score</CardDescription>
                    <CardTitle className="text-3xl text-blue-600">
                      {reportData.summary.averageConnectivityScore.toFixed(1)}
                    </CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Served Barangays</CardDescription>
                    <CardTitle className="text-3xl text-green-600">
                      {reportData.summary.servedBarangays}
                    </CardTitle>
                    <CardDescription className="text-xs text-muted-foreground">
                      {reportData.summary.servedPercentage.toFixed(1)}%
                    </CardDescription>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Underserved Barangays</CardDescription>
                    <CardTitle className="text-3xl text-yellow-600">
                      {reportData.summary.underservedBarangays}
                    </CardTitle>
                    <CardDescription className="text-xs text-muted-foreground">
                      {reportData.summary.underservedPercentage.toFixed(1)}%
                    </CardDescription>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Unserved Barangays</CardDescription>
                    <CardTitle className="text-3xl text-red-600">
                      {reportData.summary.unservedBarangays}
                    </CardTitle>
                    <CardDescription className="text-xs text-muted-foreground">
                      {reportData.summary.unservedPercentage.toFixed(1)}%
                    </CardDescription>
                  </CardHeader>
                </Card>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <Card>
                  <CardHeader>
                    <CardTitle>Provider Coverage</CardTitle>
                    <CardDescription>Number of barangays covered by each provider</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {reportData.summary.providerCoverage &&
                        Object.entries(reportData.summary.providerCoverage).map(([provider, count]) => (
                          <div key={provider} className="flex justify-between items-center">
                            <span className="text-sm font-medium">{provider}</span>
                            <span className="text-sm text-muted-foreground">{count} barangays</span>
                          </div>
                        ))}
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Technology Distribution</CardTitle>
                    <CardDescription>Highest technology generation by barangays</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {reportData.summary.technologyDistribution &&
                        Object.entries(reportData.summary.technologyDistribution).map(([tech, count]) => (
                          <div key={tech} className="flex justify-between items-center">
                            <span className="text-sm font-medium">{tech}</span>
                            <span className="text-sm text-muted-foreground">{count} barangays</span>
                          </div>
                        ))}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="municipalities" className="space-y-4">
              <div className="space-y-4">
                {reportData.municipalities?.map((municipality) => (
                  <Card key={municipality.id}>
                    <CardHeader>
                      <CardTitle>{municipality.name}</CardTitle>
                      <div className="flex gap-4 text-sm">
                        <span>Total: {municipality.totalBarangays}</span>
                        <span className="text-green-600">Served: {municipality.servedBarangays}</span>
                        <span className="text-orange-600">Underserved: {municipality.underservedBarangays}</span>
                        <span className="text-red-600">Unserved: {municipality.unservedBarangays}</span>
                      </div>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-3">
                        {municipality.barangays
                          ?.slice(0, expandedMunicipalities.has(municipality.id) ? undefined : 10)
                          .map((barangay) => (
                          <div
                            key={barangay.id}
                            className="border rounded-lg p-3 space-y-2 hover:bg-muted/50 transition-colors">
                            <div className="flex justify-between items-start">
                              <div className="space-y-1">
                                <p className="text-sm font-medium">{barangay.name}</p>
                                <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                                  {barangay.highestTechnology && (
                                    <span className="bg-primary/10 px-2 py-0.5 rounded">
                                      {barangay.highestTechnology}
                                    </span>
                                  )}
                                  <span>Score: {barangay.connectivityScore.toFixed(1)}</span>
                                  {barangay.averageDownloadSpeedMbps && (
                                    <span>↓ {barangay.averageDownloadSpeedMbps.toFixed(1)} Mbps</span>
                                  )}
                                  {barangay.averageUploadSpeedMbps && (
                                    <span>↑ {barangay.averageUploadSpeedMbps.toFixed(1)} Mbps</span>
                                  )}
                                </div>
                              </div>
                              <div className="flex items-center gap-2">
                                {getStatusBadge(barangay.serviceStatus)}
                              </div>
                            </div>
                            
                            {barangay.providerAvailability.hasAnyProvider && (
                              <div className="border-t pt-2">
                                <p className="text-xs font-medium text-muted-foreground mb-1">
                                  Providers ({barangay.providerAvailability.providerCount})
                                </p>
                                <div className="space-y-1">
                                  {Object.entries(barangay.providerAvailability.providers).map(([provider, info]) => (
                                    <div key={provider} className="flex justify-between items-center text-xs">
                                      <span className="font-medium">{provider}</span>
                                      <div className="flex gap-2 text-muted-foreground">
                                        {info.availableGenerations && (
                                          <span className="bg-muted px-1.5 py-0.5 rounded">
                                            {info.availableGenerations}
                                          </span>
                                        )}
                                        {info.averageDownloadSpeedMbps && (
                                          <span>↓ {info.averageDownloadSpeedMbps.toFixed(1)}</span>
                                        )}
                                        {info.averageUploadSpeedMbps && (
                                          <span>↑ {info.averageUploadSpeedMbps.toFixed(1)}</span>
                                        )}
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            )}
                            
                            {barangay.remarks && (
                              <p className="text-xs text-muted-foreground italic border-t pt-2">
                                {barangay.remarks}
                              </p>
                            )}
                          </div>
                        ))}
                      </div>
                      {municipality.barangays && municipality.barangays.length > 10 && (
                        <div className="mt-3 text-center">
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
              <div className="flex items-center justify-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageNumber((prev) => Math.max(1, prev - 1))}
                  disabled={!reportData.hasPreviousPage || isLoading}>
                  Previous
                </Button>
                <span className="text-sm text-muted-foreground">
                  Page {reportData.pageNumber} of {reportData.totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageNumber((prev) => prev + 1)}
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
