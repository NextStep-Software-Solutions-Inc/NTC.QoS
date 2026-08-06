"use client";

import { useState } from "react";
import { Activity, ChevronLeft, ChevronRight, Clock, Download, Search, Upload } from "lucide-react";

import { getSpeedtestsRequest } from "@/components/features/speedtest/handlers/getSpeedtestsRequest";
import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { formatNumber } from "@/lib/format-number";

const useGetSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);

interface ManageSpeedtestsProps {
  regionId: string;
}

export default function ManageSpeedtests({ regionId }: ManageSpeedtestsProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20; // Reasonable page size for large datasets

  const {
    data: speedtestsResponse,
    isLoading,
    error,
  } = useGetSpeedtests({
    pathParams: {},
    queryParams: { pageSize, pageNumber: currentPage, search: searchTerm || undefined, regionIds: [regionId] },
  });

  const speedtests = speedtestsResponse?.data || [];
  const totalCount = speedtests.length;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleSearch = (value: string) => {
    setSearchTerm(value);
    setCurrentPage(1); // Reset to first page when searching
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  if (error) {
    return (
      <div className="container mx-auto py-6">
        <div className="text-center py-12">
          <div className="text-red-500 mb-4">Error loading speedtest data</div>
          <p className="text-muted-foreground">Please try again later</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Speedtest Results</h1>
        <p className="text-muted-foreground">Monitor broadband performance and network quality across the region.</p>
      </div>

      {/* Search and Filters */}
      <div className="mb-6">
        <div className="flex items-center gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
            <Input
              placeholder="Search speedtests..."
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              className="pl-10"
            />
          </div>
          <div className="text-sm text-muted-foreground">{totalCount} total speedtests</div>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardHeader>
                <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                <div className="h-3 bg-gray-200 rounded w-1/2"></div>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="h-3 bg-gray-200 rounded"></div>
                  <div className="h-3 bg-gray-200 rounded w-2/3"></div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Speedtests Grid */}
      {!isLoading && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
            {speedtests.map((speedtest: SpeedTest) => (
              <Card key={speedtest.id} className="hover:shadow-md transition-shadow">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Activity className="h-5 w-5" />
                      Speedtest
                    </CardTitle>
                    <Badge variant="outline">{speedtest.technology?.standard || "Unknown"}</Badge>
                  </div>
                  <CardDescription>
                    {speedtest.address ||
                      `${speedtest.latitude?.toFixed(4) || "N/A"}, ${speedtest.longitude?.toFixed(4) || "N/A"}`}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Download className="h-4 w-4 text-green-600" />
                        <span className="text-sm font-medium">Download</span>
                      </div>
                      <span className="font-bold">{formatNumber(speedtest.downloadSpeed || 0)} Mbps</span>
                    </div>

                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Upload className="h-4 w-4 text-blue-600" />
                        <span className="text-sm font-medium">Upload</span>
                      </div>
                      <span className="font-bold">{formatNumber(speedtest.uploadSpeed || 0)} Mbps</span>
                    </div>

                    {speedtest.ping && (
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Clock className="h-4 w-4 text-orange-600" />
                          <span className="text-sm font-medium">Ping</span>
                        </div>
                        <span className="font-bold">{speedtest.ping} ms</span>
                      </div>
                    )}

                    {speedtest.serviceProvider && (
                      <div className="text-sm pt-2 border-t">
                        <span className="font-medium">Provider:</span>{" "}
                        {speedtest.serviceProvider.provider?.name || "Unknown"}
                      </div>
                    )}

                    {speedtest.dateTime && (
                      <div className="text-xs text-muted-foreground">
                        {new Date(speedtest.dateTime).toLocaleDateString()}{" "}
                        {new Date(speedtest.dateTime).toLocaleTimeString()}
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between">
              <div className="text-sm text-muted-foreground">
                Showing {(currentPage - 1) * pageSize + 1} to {Math.min(currentPage * pageSize, totalCount)} of{" "}
                {totalCount} speedtests
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handlePageChange(currentPage - 1)}
                  disabled={currentPage === 1}>
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <span className="text-sm">
                  Page {currentPage} of {totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handlePageChange(currentPage + 1)}
                  disabled={currentPage === totalPages}>
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}

          {/* Empty State */}
          {speedtests.length === 0 && !isLoading && (
            <div className="text-center py-12">
              <Activity className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
              <h3 className="text-lg font-medium">No speedtest results found</h3>
              <p className="text-muted-foreground">
                {searchTerm
                  ? "No speedtests match your search criteria"
                  : "Speedtest results will appear here once performance tests are conducted"}
              </p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
