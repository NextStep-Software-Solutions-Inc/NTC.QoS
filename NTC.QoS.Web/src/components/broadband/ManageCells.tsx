"use client";

import { useState } from "react";
import { ChevronLeft, ChevronRight, MapPin, RadioTower, Search, Signal } from "lucide-react";

import { getCellsRequest } from "@/components/features/cell/handlers/getCellsRequest";
import { type Cell } from "@/components/features/cell/types/Cell";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useGetCells = createQueryHookFromTemplate(getCellsRequest);

interface ManageCellsProps {
  regionId: string;
}

export default function ManageCells({ regionId }: ManageCellsProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20; // Reasonable page size for large datasets

  const {
    data: cellsResponse,
    isLoading,
    error,
  } = useGetCells({
    pathParams: {},
    queryParams: { pageSize, pageNumber: currentPage, search: searchTerm || undefined, regionIds: [regionId] },
  });

  const cells = cellsResponse?.data || [];
  const totalCount = cellsResponse?.totalRows || 0;
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
          <div className="text-red-500 mb-4">Error loading cell data</div>
          <p className="text-muted-foreground">Please try again later</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Cell Sites</h1>
        <p className="text-muted-foreground">Manage and monitor cell sites across the region.</p>
      </div>

      {/* Search and Filters */}
      <div className="mb-6">
        <div className="flex items-center gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
            <Input
              placeholder="Search cells..."
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              className="pl-10"
            />
          </div>
          <div className="text-sm text-muted-foreground">{totalCount} total cells</div>
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

      {/* Cells Grid */}
      {!isLoading && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
            {cells.map((cell: Cell) => (
              <Card key={cell.id} className="hover:shadow-md transition-shadow">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <RadioTower className="h-5 w-5" />
                      {cell.name || `Cell ${cell.id}`}
                    </CardTitle>
                    <Badge variant={cell.status === "active" ? "default" : "secondary"}>
                      {cell.status || "Unknown"}
                    </Badge>
                  </div>
                  <CardDescription>{cell.siteId && `Site ID: ${cell.siteId}`}</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm">
                      <MapPin className="h-4 w-4" />
                      <span>
                        {cell.latitude?.toFixed(4) || "N/A"}, {cell.longitude?.toFixed(4) || "N/A"}
                      </span>
                    </div>
                    {cell.serviceProvider && (
                      <div className="text-sm">
                        <span className="font-medium">Provider:</span>{" "}
                        {cell.serviceProvider.provider?.name || "Unknown"}
                      </div>
                    )}
                    {cell.elevation && (
                      <div className="text-sm">
                        <span className="font-medium">Elevation:</span> {cell.elevation}m
                      </div>
                    )}
                    <div className="flex items-center gap-2 text-sm">
                      <Signal className="h-4 w-4" />
                      <span>Verified: {cell.isVerified ? "Yes" : "No"}</span>
                    </div>
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
                {totalCount} cells
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
          {cells.length === 0 && !isLoading && (
            <div className="text-center py-12">
              <RadioTower className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
              <h3 className="text-lg font-medium">No cell sites found</h3>
              <p className="text-muted-foreground">
                {searchTerm
                  ? "No cells match your search criteria"
                  : "Cell sites will appear here once they are registered"}
              </p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
