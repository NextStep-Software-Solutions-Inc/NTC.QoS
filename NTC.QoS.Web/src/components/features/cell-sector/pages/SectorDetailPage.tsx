"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Trash2, Edit } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Antenna, Signal, Plus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { getSectorsRequest } from "../../sector/handlers/getSectorsRequest";
import { getCellularsRequest } from "../../cellular/handlers/getCellularsRequest";
import { getCellDetailsRequest } from "../../cell/handlers/getCellDetailsRequest";
import { deleteSectorRequest } from "../../sector/handlers/deleteSectorRequest";
import EditAntennaDetailsForm from "../../sector/forms/EditAntennaDetailsForm";
import AddTechnologyBandForm from "../../cell/tabs/AddTechnologyBandForm";

const useGetSectors = createQueryHookFromTemplate(getSectorsRequest);
const useGetCellulars = createQueryHookFromTemplate(getCellularsRequest);
const useGetCellDetails = createQueryHookFromTemplate(getCellDetailsRequest);
const useDeleteSector = createMutationHookFromTemplate(deleteSectorRequest);

interface SectorDetailPageProps {
  region: string;
  cellId: string;
  sectorId: string;
}

export default function SectorDetailPage({
  region,
  cellId,
  sectorId,
}: SectorDetailPageProps) {
  const router = useRouter();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [addTechBandDialogOpen, setAddTechBandDialogOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  // Fetch cell details to get address and serviceProviderId
  const { data: cellResponse, isLoading: cellLoading, error: cellError } = useGetCellDetails({
    pathParams: { id: cellId },
    queryParams: {},
  });
  const cell = cellResponse?.data;
  


  // Fetch sector details using batch endpoint with cellSectorId filter
  const { data: sectorsResponse, isLoading: sectorLoading, refetch: refetchSector } = useGetSectors({
    pathParams: {},
    queryParams: { pageSize: 1, pageNumber: 1, cellSectorId: sectorId },
  });
  const sector = (sectorsResponse?.data && Array.isArray(sectorsResponse.data)) 
    ? sectorsResponse.data[0] 
    : undefined;

  // Fetch cellulars for this sector
  const { data: cellularsResponse, isLoading: cellularsLoading, refetch: refetchCellulars } = useGetCellulars({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, cellSectorId: sectorId },
  });
  const cellulars = cellularsResponse?.data ?? [];

  // Delete sector mutation
  const { mutateAsync: deleteSector } = useDeleteSector();

  const handleDeleteSector = async () => {
    if (!sector) return;
    
    setIsDeleting(true);
    try {
      await deleteSector({
        pathParams: { id: sector.id },
      });

      toast.success("Sector deleted successfully");
      router.push(`/${region}/broadband/cellsites/${cellId}`);
    } catch (error: any) {
      console.error(error);
      toast.error(error?.response?.data?.message || "Failed to delete sector");
      setIsDeleting(false);
    }
  };

  const handleEditSuccess = () => {
    setEditDialogOpen(false);
    void refetchSector();
  };

  const handleAddTechSuccess = () => {
    setAddTechBandDialogOpen(false);
    void refetchCellulars();
  };

  if (sectorLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-48 rounded-lg" />
        <Skeleton className="h-96 rounded-lg" />
      </div>
    );
  }

  if (!sector) {
    return (
      <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
        <CardContent className="py-12">
          <div className="flex flex-col items-center justify-center text-center">
            <Antenna className="h-12 w-12 text-gray-300 mb-3" />
            <p className="text-sm text-gray-500">Sector not found.</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const cellularsArray = Array.isArray(cellulars) ? cellulars : (cellulars as any)?.data ?? [];
  
  // Try to get serviceProviderId from cell, fallback to first cellular if available
  const serviceProviderId = cell?.serviceProviderId || cell?.serviceProvider?.id || (cellularsArray[0] as any)?.serviceProviderId || "";
  
  const transformedCellulars = cellularsArray.map((cellular: any) => ({
    id: cellular.id,
    technologyStandard: cellular.technology?.standard,
    technologyDescription: cellular.technology?.description,
    bandName: cellular.band?.name,
    bandDescription: cellular.band?.description,
  }));

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => router.push(`/${region}/broadband/cellsites/${cellId}`)}
        >
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Cell Site
        </Button>
      </div>

      {/* Antenna Details Card */}
      <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm overflow-hidden">
        <CardHeader className="pb-3">
          <div className="flex items-start justify-between">
            <div>
              <CardTitle className="flex items-center gap-2 text-2xl">
                <Antenna className="h-6 w-6 text-blue-500" />
                {sector.antennaType} Antenna
              </CardTitle>
              <CardDescription className="mt-2">Antenna Configuration Details</CardDescription>
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setEditDialogOpen(true)}
                className="gap-2"
              >
                <Edit className="h-4 w-4" />
                Edit Details
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={() => setDeleteConfirmOpen(true)}
                className="gap-2"
              >
                <Trash2 className="h-4 w-4" />
                Delete
              </Button>
            </div>
          </div>
        </CardHeader>

        <CardContent className="space-y-6">
          {/* Basic Antenna Info */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="p-4 rounded-lg bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20">
              <div className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">Antenna Type</div>
              <p className="text-lg font-bold">{sector.antennaType || "—"}</p>
            </div>

            <div className="p-4 rounded-lg bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20">
              <div className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">Radio Equipment</div>
              <p className="text-lg font-bold">{sector.radioEquipment}</p>
            </div>

            <div className="p-4 rounded-lg bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20">
              <div className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">RX = TX</div>
              <p className="text-lg font-bold">{sector.isRxEqualTx ? "Yes" : "No"}</p>
            </div>
          </div>

          {/* TX Configuration Summary */}
          <div className="border-t pt-4">
            <h4 className="font-semibold mb-3">TX Configuration</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">TX Power</div>
                <p className="font-semibold text-sm">{sector.txPower} W</p>
              </div>
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">TX Antenna Model</div>
                <p className="font-semibold text-sm">{sector.txAntennaModel}</p>
              </div>
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">TX Gain</div>
                <p className="font-semibold text-sm">{sector.txAntennaGain} dBi</p>
              </div>
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">TX EIRP</div>
                <p className="font-semibold text-sm">{sector.txEIRP} dBm</p>
              </div>
            </div>
          </div>

          {/* RX Configuration Summary */}
          <div className="border-t pt-4">
            <h4 className="font-semibold mb-3">RX Configuration</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">RX Antenna Model</div>
                <p className="font-semibold text-sm">{sector.rxAntennaModel}</p>
              </div>
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">RX Height</div>
                <p className="font-semibold text-sm">{sector.rxAntennaHeight} m</p>
              </div>
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">RX Gain</div>
                <p className="font-semibold text-sm">{sector.rxAntennaGain} dBi</p>
              </div>
              <div className="p-3 rounded bg-gray-50 dark:bg-gray-900/50">
                <div className="text-xs text-gray-600 dark:text-gray-400">RX EIRP</div>
                <p className="font-semibold text-sm">{sector.rxEIRP} dBm</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Technologies & Bands Card */}
      <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm overflow-hidden">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Signal className="h-5 w-5" />
                Assigned Technologies & Bands
              </CardTitle>
              <CardDescription>Technology and band assignments for this sector</CardDescription>
            </div>
            <Button
              size="sm"
              onClick={() => setAddTechBandDialogOpen(true)}
              disabled={cellLoading}
              className="gap-2"
            >
              <Plus className="h-4 w-4" />
              Add Technology
            </Button>
          </div>
        </CardHeader>

        <CardContent>
          {cellularsLoading ? (
            <div className="space-y-3">
              {[1, 2].map((i) => (
                <Skeleton key={i} className="h-20 rounded-lg" />
              ))}
            </div>
          ) : transformedCellulars.length === 0 ? (
            <div className="p-8 text-center bg-gray-50 dark:bg-gray-900/50 rounded-lg">
              <Signal className="h-12 w-12 text-gray-300 mx-auto mb-2" />
              <p className="text-sm text-gray-500">No technologies assigned to this sector yet.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {transformedCellulars.map((cellular: any) => (
                <div
                  key={`${cellular.technologyStandard}-${cellular.bandName}`}
                  className="p-4 rounded-lg border border-gray-200 dark:border-gray-700 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20 hover:shadow-md transition-shadow"
                >
                  <div className="flex items-start justify-between gap-3">
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
        </CardContent>
      </Card>

      {/* Edit Antenna Details Dialog */}
      {sector && (
        <ResponsiveDialog
          open={editDialogOpen}
          onOpenChange={setEditDialogOpen}
          title="Edit Antenna Details"
          description="Update the detailed antenna and radio equipment configuration"
        >
          <EditAntennaDetailsForm
            sectorId={sectorId}
            sector={sector}
            onCancel={() => setEditDialogOpen(false)}
            onSuccess={handleEditSuccess}
          />
        </ResponsiveDialog>
      )}

      {/* Add Technology Dialog */}
      {cell && (
        <>
          <ResponsiveDialog
            open={addTechBandDialogOpen}
            onOpenChange={setAddTechBandDialogOpen}
            title="Add Technology & Band to Sector"
            description="Select a technology and band for this antenna sector"
          >
            <AddTechnologyBandForm
              cellId={cellId}
              cellSectorId={sectorId}
              cellAddress={cell.address || ""}
              serviceProviderId={serviceProviderId}
              onCancel={() => setAddTechBandDialogOpen(false)}
              onSuccess={handleAddTechSuccess}
            />
          </ResponsiveDialog>
        </>
      )}

      {/* Delete Confirmation Dialog */}
      <ResponsiveDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title="Delete Sector"
        description="Are you sure you want to delete this antenna sector? This action cannot be undone."
      >
        <div className="space-y-4 p-4">
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Deleting this sector will remove all associated technology and band assignments.
          </p>
          <div className="flex gap-3">
            <Button
              variant="outline"
              onClick={() => setDeleteConfirmOpen(false)}
              disabled={isDeleting}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteSector}
              disabled={isDeleting}
              className="flex-1"
            >
              {isDeleting ? "Deleting..." : "Delete Sector"}
            </Button>
          </div>
        </div>
      </ResponsiveDialog>
    </div>
  );
}
