"use client";

import React, { useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { Plus, Antenna, Wind, Zap, Radio, Signal } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { ResponsiveDialog } from '@/components/ResponsiveDialog';
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getCellSectorsRequest } from "../../cell-sector/handlers/getCellSectorsRequest";
import { getCellularsRequest } from "../../cellular/handlers/getCellularsRequest";
import AddTechnologyBandForm from "./AddTechnologyBandForm";
import { type Cell } from "../types/Cell";

const useGetCellSectors = createQueryHookFromTemplate(getCellSectorsRequest);
const useGetCellulars = createQueryHookFromTemplate(getCellularsRequest);

interface CellSectorsTabProps {
  cellId: string;
  cell: Cell;
}

export default function CellSectorsTab({
  cellId,
  cell,
}: CellSectorsTabProps) {
  const router = useRouter();
  const params = useParams();
  const region = params.region as string;
  const serviceProviderId = cell.serviceProviderId || cell.serviceProvider?.id || "";
  const cellAddress = cell.address;
  const [selectedSectorId, setSelectedSectorId] = useState<string | null>(null);
  const [addTechBandDialogOpen, setAddTechBandDialogOpen] = useState(false);
  const [cellularsBySector, setCellularsBySector] = useState<Map<string, any[]>>(new Map());

  // Fetch cell sectors
  const { data: sectorsResponse, isLoading: sectorsLoading, refetch: refetchSectors } = useGetCellSectors({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, cellIds: [cellId] },
  });
  const sectors = sectorsResponse?.data ?? [];

  // Fetch all cellulars for this cell using the proper hook
  const { data: cellularsResponse, isLoading: cellularsLoading, refetch: refetchCellulars } = useGetCellulars({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, cellId },
  });

  // Group cellulars by cellSector.id whenever data changes
  React.useEffect(() => {
    const cellulars = (cellularsResponse?.data || []) as any[];
    const map = new Map<string, any[]>();
    
    // Group cellulars by cellSector.id
    cellulars.forEach((cellular: any) => {
      const sectorId = cellular.cellSector?.id;
      if (sectorId) {
        if (!map.has(sectorId)) {
          map.set(sectorId, []);
        }
        map.get(sectorId)?.push({
          id: cellular.id,
          technologyStandard: cellular.technology?.standard,
          technologyDescription: cellular.technology?.description,
          bandName: cellular.band?.name,
          bandDescription: cellular.band?.description,
        });
      }
    });
    
    setCellularsBySector(map);
  }, [cellularsResponse]);

  const handleAddTechBand = (sectorId: string) => {
    setSelectedSectorId(sectorId);
    setAddTechBandDialogOpen(true);
  };

  const handleTechBandSuccess = () => {
    setAddTechBandDialogOpen(false);
    setSelectedSectorId(null);
    // Refetch cellulars
    void refetchCellulars();
  };

  if (sectorsLoading || cellularsLoading) {
    return (
      <div className="space-y-4">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-48 rounded-lg" />
        ))}
      </div>
    );
  }

  if (sectors.length === 0) {
    return (
      <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
        <CardHeader>
          <CardTitle>Cell Sectors</CardTitle>
          <CardDescription>Antenna sectors for this cell site</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Antenna className="h-12 w-12 text-gray-300 mb-3" />
            <p className="text-sm text-gray-500">No sectors configured for this cell site.</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {sectors.map((sector) => {
        const sectorCellulars = cellularsBySector.get(sector.id) ?? [];
        return (
          <Card
            key={sector.id}
            className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm overflow-hidden"
          >
            <CardHeader className="pb-3">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <Antenna className="h-5 w-5 text-blue-500" />
                    {sector.antennaType} Antenna
                  </CardTitle>
                  <CardDescription>Sector Configuration & Technology Assignment</CardDescription>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => router.push(`/${region}/broadband/cellsites/${cellId}/sectors/${sector.id}`)}
                  className="gap-2"
                >
                  View Details
                </Button>
              </div>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Antenna Details Grid */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="p-3 rounded-lg bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20">
                  <div className="flex items-center gap-2 mb-2">
                    <Antenna className="h-4 w-4 text-blue-500" />
                    <span className="text-xs font-semibold text-gray-600 dark:text-gray-400">Antenna Type</span>
                  </div>
                  <p className="text-sm font-semibold">{sector.antennaType}</p>
                </div>

                <div className="p-3 rounded-lg bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20">
                  <div className="flex items-center gap-2 mb-2">
                    <Zap className="h-4 w-4 text-purple-500" />
                    <span className="text-xs font-semibold text-gray-600 dark:text-gray-400">Height</span>
                  </div>
                  <p className="text-sm font-semibold">{sector.antennaHeight} m</p>
                </div>

                <div className="p-3 rounded-lg bg-gradient-to-br from-orange-50 to-red-50 dark:from-orange-900/20 dark:to-red-900/20">
                  <div className="flex items-center gap-2 mb-2">
                    <Wind className="h-4 w-4 text-orange-500" />
                    <span className="text-xs font-semibold text-gray-600 dark:text-gray-400">Azimuth</span>
                  </div>
                  <p className="text-sm font-semibold">{sector.azimuth}°</p>
                </div>

                <div className="p-3 rounded-lg bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20">
                  <div className="flex items-center gap-2 mb-2">
                    <Radio className="h-4 w-4 text-green-500" />
                    <span className="text-xs font-semibold text-gray-600 dark:text-gray-400">Beam Tilt</span>
                  </div>
                  <p className="text-sm font-semibold">{sector.beamTilt}°</p>
                </div>
              </div>

              {/* Technologies & Bands */}
              <div className="border-t pt-4">
                <div className="flex items-center justify-between mb-4">
                  <h4 className="font-semibold flex items-center gap-2">
                    <Signal className="h-4 w-4" />
                    Assigned Technologies & Bands
                  </h4>
                  <Button
                    size="sm"
                    onClick={() => handleAddTechBand(sector.id)}
                    className="gap-2"
                  >
                    <Plus className="h-4 w-4" />
                    Add Technology
                  </Button>
                </div>

                {sectorCellulars.length === 0 ? (
                  <div className="p-4 text-center bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                    <Signal className="h-8 w-8 text-gray-300 mx-auto mb-2" />
                    <p className="text-sm text-gray-500">No technologies assigned to this sector yet.</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {sectorCellulars.map((cellular) => (
                      <div
                        key={`${cellular.technologyId}-${cellular.bandId}`}
                        className="p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20"
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div>
                            <p className="font-semibold text-sm">{cellular.technologyStandard || 'Technology'}</p>
                            {cellular.technologyDescription && (
                              <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                                {cellular.technologyDescription}
                              </p>
                            )}
                          </div>
                          {cellular.bandName && (
                            <Badge variant="secondary" className="text-xs whitespace-nowrap">
                              {cellular.bandName}
                            </Badge>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        );
      })}

      {/* Add Technology & Band Dialog */}
      {selectedSectorId && (
        <ResponsiveDialog
          open={addTechBandDialogOpen}
          onOpenChange={setAddTechBandDialogOpen}
          title="Add Technology & Band to Sector"
          description="Select a technology and band for this antenna sector"
        >
          <AddTechnologyBandForm
            cellId={cellId}
            cellSectorId={selectedSectorId}
            cellAddress={cellAddress}
            serviceProviderId={serviceProviderId}
            onCancel={() => setAddTechBandDialogOpen(false)}
            onSuccess={handleTechBandSuccess}
          />
        </ResponsiveDialog>
      )}
    </div>
  );
}
