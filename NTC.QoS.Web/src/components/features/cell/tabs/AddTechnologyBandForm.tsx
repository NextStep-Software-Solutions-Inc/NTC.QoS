"use client";

import React from "react";
import { toast } from "sonner";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { getTechnologiesRequest } from "../../technology/handlers/getTechnologiesRequest";
import { getBandsRequest } from "../../band/handlers/getBandsRequest";
import { createCellularRequest } from "../../cellular/handlers/getCellularsRequest";

const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useGetBands = createQueryHookFromTemplate(getBandsRequest);
const useCreateCellular = createMutationHookFromTemplate(createCellularRequest);

interface AddTechnologyBandFormProps {
  cellId: string;
  cellSectorId: string;
  cellAddress: string;
  serviceProviderId: string;
  onCancel: () => void;
  onSuccess: () => void;
}

export default function AddTechnologyBandForm({
  cellId,
  cellSectorId,
  cellAddress,
  serviceProviderId,
  onCancel,
  onSuccess,
}: AddTechnologyBandFormProps) {
  const [selectedTechnologyId, setSelectedTechnologyId] = React.useState<string>("");
  const [selectedBandId, setSelectedBandId] = React.useState<string>("");
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  // Fetch technologies
  const { data: technologiesResponse, isLoading: technologiesLoading } = useGetTechnologies({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const technologies = technologiesResponse?.data ?? [];

  // Fetch bands
  const { data: bandsResponse, isLoading: bandsLoading } = useGetBands({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const bands = bandsResponse?.data ?? [];

  // Create cellular mutation
  const { mutateAsync: createCellular } = useCreateCellular();

  const handleSubmit = async () => {
    if (!selectedTechnologyId || !selectedBandId) {
      toast.error("Please select both technology and band");
      return;
    }

    setIsSubmitting(true);
    try {
      await createCellular({
        pathParams: {},
        payload: {
          cellSectorId,
          serviceProviderId,
          technologyId: selectedTechnologyId,
          bandId: selectedBandId,
          address: cellAddress,
        },
      });

      toast.success("Technology and band added successfully");
      onSuccess();
    } catch (error: any) {
      // Check if error is "already exists"
      const errorMessage = error?.response?.message || error?.message || "";
      if (errorMessage.toLowerCase().includes("already exists")) {
        toast.info("This technology and band combination is already assigned to this sector");
        onSuccess();
      } else {
        toast.error(errorMessage || "Failed to add technology and band");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 p-4">
      <div>
        <Label htmlFor="technology-select" className="text-sm font-medium">
          Technology <span className="text-red-500">*</span>
        </Label>
        <Select value={selectedTechnologyId} onValueChange={setSelectedTechnologyId} disabled={technologiesLoading}>
          <SelectTrigger id="technology-select" className="mt-2">
            <SelectValue placeholder="Select a technology" />
          </SelectTrigger>
          <SelectContent>
            {technologiesLoading ? (
              <div className="p-2">
                <Skeleton className="h-4 w-32" />
              </div>
            ) : technologies.length === 0 ? (
              <div className="p-2 text-sm text-gray-500">No technologies available</div>
            ) : (
              technologies.map((tech) => (
                <SelectItem key={tech.id} value={tech.id}>
                  {tech.standard}
                </SelectItem>
              ))
            )}
          </SelectContent>
        </Select>
        <p className="text-xs text-gray-500 mt-1">Select the network technology for this sector</p>
      </div>

      <div>
        <Label htmlFor="band-select" className="text-sm font-medium">
          Band <span className="text-red-500">*</span>
        </Label>
        <Select value={selectedBandId} onValueChange={setSelectedBandId} disabled={bandsLoading}>
          <SelectTrigger id="band-select" className="mt-2">
            <SelectValue placeholder="Select a band" />
          </SelectTrigger>
          <SelectContent>
            {bandsLoading ? (
              <div className="p-2">
                <Skeleton className="h-4 w-32" />
              </div>
            ) : bands.length === 0 ? (
              <div className="p-2 text-sm text-gray-500">No bands available</div>
            ) : (
              bands.map((band) => (
                <SelectItem key={band.id} value={band.id}>
                  {band.name}
                </SelectItem>
              ))
            )}
          </SelectContent>
        </Select>
        <p className="text-xs text-gray-500 mt-1">Select the frequency band for this technology</p>
      </div>

      <div className="flex gap-2 justify-end">
        <Button variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} disabled={isSubmitting || !selectedTechnologyId || !selectedBandId}>
          {isSubmitting ? "Adding..." : "Add Technology & Band"}
        </Button>
      </div>
    </div>
  );
}
