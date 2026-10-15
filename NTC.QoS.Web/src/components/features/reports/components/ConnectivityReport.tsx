"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Activity, Download, FileText, Loader2 } from "lucide-react";

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

export function ConnectivityReport() {
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
      isWirelessCovered: true,
    }
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
      pageNumber,
      pageSize,
    },
    enabled: false,
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
      const response = await fetch(`${backendUrl}/reports/connectivity/download?${params.toString()}`, {
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
        reportType: 'Connectivity',
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
    } finally {
      setDownloadingFormat(null);
    }
  };

  const handleExportPdf = () => handleDownload(ReportFormat.Pdf);
  const handleExportExcel = () => handleDownload(ReportFormat.Excel);

  const getStatusBadge = (status: ConnectivityStatusEnum) => {
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

  const getScoreLevel = (score: number) => {
    if (score >= 80) return { label: "Excellent", className: "bg-green-100 text-green-700" };
    if (score >= 60) return { label: "Good", className: "bg-blue-100 text-blue-700" };
    if (score >= 40) return { label: "Moderate", className: "bg-yellow-100 text-yellow-700" };
    return { label: "Fair", className: "bg-orange-100 text-orange-700" };
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Activity className="h-5 w-5 text-blue-600" />
          <CardTitle>Connectivity Assessment Report</CardTitle>
        </div>
        <CardDescription>
          Comprehensive connectivity assessment with quality scores and infrastructure analysis
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div className="space-y-2">
            <Label htmlFor="connectivity-region">Region</Label>
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
                <SelectTrigger id="connectivity-region" disabled={!!regionParam && !!currentRegion}>
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
            <Label htmlFor="connectivity-province">Province</Label>
            <Select
              value={provinceId}
              onValueChange={(value) => {
                setProvinceId(value);
                setMunicipalityId("all");
                setPageNumber(1);
              }}
              disabled={regionId === "all"}>
              <SelectTrigger id="connectivity-province">
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
            <Label htmlFor="connectivity-municipality">Municipality</Label>
            <Select
              value={municipalityId}
              onValueChange={(value) => {
                setMunicipalityId(value);
                setPageNumber(1);
              }}
              disabled={provinceId === "all"}>
              <SelectTrigger id="connectivity-municipality">
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
                <Activity className="mr-2 h-4 w-4" />
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
                : "Loading connectivity assessment data... This may take a few moments."}
            </p>
          </div>
        )}

        {!isLoading && reportData && (
          <Tabs defaultValue="summary" className="w-full">
            <TabsList>
              <TabsTrigger value="summary">Summary</TabsTrigger>
              <TabsTrigger value="areas">Areas</TabsTrigger>
            </TabsList>

            <TabsContent value="summary" className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <Card className="border-green-200 bg-green-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-green-700">Served Barangays</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-green-900">{reportData.regionSummary.servedBarangays}</div>
                  </CardContent>
                </Card>

                <Card className="border-blue-200 bg-blue-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-blue-700">Served Population</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-blue-900">
                      {reportData.servedPopulation.toLocaleString()}
                    </div>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm font-medium">Average Connectivity Scores</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">Fiber Coverage</span>
                      <Badge className={getScoreLevel(reportData.scoreSummary.averageFiberCoverageScore).className}>
                        {reportData.scoreSummary.averageFiberCoverageScore.toFixed(1)}
                      </Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">Internet Access</span>
                      <Badge className={getScoreLevel(reportData.scoreSummary.averageInternetAccessScore).className}>
                        {reportData.scoreSummary.averageInternetAccessScore.toFixed(1)}
                      </Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">Technology Type</span>
                      <Badge className={getScoreLevel(reportData.scoreSummary.averageTechnologyTypeScore).className}>
                        {reportData.scoreSummary.averageTechnologyTypeScore.toFixed(1)}
                      </Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">Quality of Service</span>
                      <Badge className={getScoreLevel(reportData.scoreSummary.averageQualityOfServiceScore).className}>
                        {reportData.scoreSummary.averageQualityOfServiceScore.toFixed(1)}
                      </Badge>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">Coverage Availability</span>
                      <Badge className={getScoreLevel(reportData.scoreSummary.averageCoverageAvailabilityScore).className}>
                        {reportData.scoreSummary.averageCoverageAvailabilityScore.toFixed(1)}
                      </Badge>
                    </div>
                    <div className="flex justify-between items-center pt-2 border-t">
                      <span className="text-sm font-bold">Total Score</span>
                      <Badge className={`${getScoreLevel(reportData.scoreSummary.averageTotalScore).className} font-bold`}>
                        {reportData.scoreSummary.averageTotalScore.toFixed(1)}
                      </Badge>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <Card className="border-green-200 bg-green-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-green-700">Excellent</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-green-900">{reportData.scoreSummary.excellentCount}</div>
                    <p className="text-xs text-muted-foreground">Score ≥ 80</p>
                  </CardContent>
                </Card>

                <Card className="border-blue-200 bg-blue-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-blue-700">Good</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-blue-900">{reportData.scoreSummary.goodCount}</div>
                    <p className="text-xs text-muted-foreground">Score 60-79</p>
                  </CardContent>
                </Card>

                <Card className="border-yellow-200 bg-yellow-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-yellow-700">Moderate</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-yellow-900">{reportData.scoreSummary.moderateCount}</div>
                    <p className="text-xs text-muted-foreground">Score 40-59</p>
                  </CardContent>
                </Card>

                <Card className="border-orange-200 bg-orange-50">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-orange-700">Fair</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-orange-900">{reportData.scoreSummary.fairCount}</div>
                    <p className="text-xs text-muted-foreground">Score &lt; 40</p>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm font-medium">Infrastructure Summary</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                    <div className="text-center p-3 border rounded">
                      <div className="text-2xl font-bold">{reportData.scoreSummary.totalCellTowers}</div>
                      <p className="text-xs text-muted-foreground mt-1">Cell Towers</p>
                    </div>
                    <div className="text-center p-3 border rounded">
                      <div className="text-2xl font-bold">{reportData.scoreSummary.totalFiberNAPs}</div>
                      <p className="text-xs text-muted-foreground mt-1">Fiber NAPs</p>
                    </div>
                    <div className="text-center p-3 border rounded">
                      <div className="text-2xl font-bold">{reportData.scoreSummary.totalWifiHotspots}</div>
                      <p className="text-xs text-muted-foreground mt-1">WiFi Hotspots</p>
                    </div>
                    <div className="text-center p-3 border rounded">
                      <div className="text-2xl font-bold">{reportData.scoreSummary.totalServiceProviders}</div>
                      <p className="text-xs text-muted-foreground mt-1">Service Providers</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="areas" className="space-y-4">
              <div className="space-y-3">
                {reportData.municipalities && reportData.municipalities.length > 0 ? (
                  reportData.municipalities.flatMap((municipality) => municipality.barangays || []).map((area: AreaDto) => {
                    const scoreLevel = getScoreLevel(area.totalScore);
                    // Format complete location name
                    const locationParts = [area.locationName];
                    if (area.municipalityName) locationParts.push(area.municipalityName);
                    if (area.provinceName) locationParts.push(area.provinceName);
                    const fullLocationName = locationParts.filter(Boolean).join(", ");
                    
                    return (
                      <Card key={area.locationId}>
                        <CardContent className="pt-6">
                          <div className="space-y-3">
                            <div className="flex items-start justify-between">
                              <div className="space-y-1">
                                <h4 className="font-medium text-lg">{fullLocationName}</h4>
                                <div className="flex gap-2 items-center">
                                  {getStatusBadge(area.connectivityStatus)}
                                  <Badge className={scoreLevel.className}>
                                    {scoreLevel.label}: {area.totalScore.toFixed(1)}
                                  </Badge>
                                </div>
                              </div>
                              <div className="text-right text-sm text-muted-foreground">
                                <div>Population: {area.population?.toLocaleString() || "N/A"}</div>
                                {area.dominantServiceProvider && (
                                  <div className="font-medium text-foreground mt-1">
                                    Provider: {area.dominantServiceProvider}
                                  </div>
                                )}
                              </div>
                            </div>

                            <div className="grid grid-cols-2 md:grid-cols-5 gap-2 text-sm">
                              <div className="p-2 bg-muted rounded">
                                <div className="text-xs text-muted-foreground">Fiber Coverage</div>
                                <div className="font-semibold">{area.fiberCoverageScore.toFixed(1)}</div>
                              </div>
                              <div className="p-2 bg-muted rounded">
                                <div className="text-xs text-muted-foreground">Internet Access</div>
                                <div className="font-semibold">{area.internetAccessScore.toFixed(1)}</div>
                              </div>
                              <div className="p-2 bg-muted rounded">
                                <div className="text-xs text-muted-foreground">Technology</div>
                                <div className="font-semibold">{area.technologyTypeScore.toFixed(1)}</div>
                              </div>
                              <div className="p-2 bg-muted rounded">
                                <div className="text-xs text-muted-foreground">QoS</div>
                                <div className="font-semibold">{area.qualityOfServiceScore.toFixed(1)}</div>
                              </div>
                              <div className="p-2 bg-muted rounded">
                                <div className="text-xs text-muted-foreground">Coverage</div>
                                <div className="font-semibold">{area.coverageAvailabilityScore.toFixed(1)}</div>
                              </div>
                            </div>

                            <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
                              <div className="flex justify-between p-2 border rounded">
                                <span className="text-muted-foreground">Cell Towers:</span>
                                <span className="font-medium">{area.cellTowerCount}</span>
                              </div>
                              <div className="flex justify-between p-2 border rounded">
                                <span className="text-muted-foreground">Fiber NAPs:</span>
                                <span className="font-medium">{area.fiberNAPCount}</span>
                              </div>
                              <div className="flex justify-between p-2 border rounded">
                                <span className="text-muted-foreground">WiFi Hotspots:</span>
                                <span className="font-medium">{area.wifiHotspotCount}</span>
                              </div>
                              <div className="flex justify-between p-2 border rounded">
                                <span className="text-muted-foreground">Providers:</span>
                                <span className="font-medium">{area.serviceProviderCount}</span>
                              </div>
                            </div>

                            {(area.averageDownloadSpeedMbps || area.averageUploadSpeedMbps) && (
                              <div className="flex gap-4 text-sm">
                                <div>
                                  <span className="text-muted-foreground">Avg Download: </span>
                                  <span className="font-medium">{area.averageDownloadSpeedMbps?.toFixed(2)} Mbps</span>
                                </div>
                                <div>
                                  <span className="text-muted-foreground">Avg Upload: </span>
                                  <span className="font-medium">{area.averageUploadSpeedMbps?.toFixed(2)} Mbps</span>
                                </div>
                                <div>
                                  <span className="text-muted-foreground">Speed Tests: </span>
                                  <span className="font-medium">{area.speedTestCount}</span>
                                </div>
                              </div>
                            )}

                            <div className="text-xs text-muted-foreground">
                              Last Assessed: {new Date(area.lastAssessed).toLocaleDateString()}
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    );
                  })
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Activity className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>No areas found for the selected filters</p>
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
