"use client";

import React from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editCellSectorRequest } from "../../cell-sector/handlers/editCellSectorRequest";

const useEditCellSector = createMutationHookFromTemplate(editCellSectorRequest);

interface EditSectorFormProps {
  sectorId: string;
  antennaType: string;
  antennaHeight: number;
  azimuth: number;
  beamTilt: number;
  onCancel: () => void;
  onSuccess: () => void;
}

export default function EditSectorForm({
  sectorId,
  antennaType,
  antennaHeight,
  azimuth,
  beamTilt,
  onCancel,
  onSuccess,
}: EditSectorFormProps) {
  const [formData, setFormData] = React.useState({
    antennaType,
    antennaHeight,
    azimuth,
    beamTilt,
  });
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  const { mutateAsync: editSector } = useEditCellSector();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === "antennaType" ? value : parseFloat(value) || 0,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    setIsSubmitting(true);
    try {
      await editSector({
        pathParams: { id: sectorId },
        payload: {
          antennaType: formData.antennaType,
          antennaHeight: formData.antennaHeight,
          azimuth: formData.azimuth,
          beamTilt: formData.beamTilt,
        },
      });

      toast.success("Sector details updated successfully");
      onSuccess();
    } catch (error: any) {
      console.error(error);
      toast.error(error?.response?.data?.message || "Failed to update sector details");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 p-4">
      <div>
        <Label htmlFor="antennaType" className="text-sm font-medium">
          Antenna Type <span className="text-red-500">*</span>
        </Label>
        <Input
          id="antennaType"
          name="antennaType"
          value={formData.antennaType}
          onChange={handleInputChange}
          placeholder="Enter antenna type"
          disabled={isSubmitting}
          className="mt-1"
        />
      </div>

      <div>
        <Label htmlFor="antennaHeight" className="text-sm font-medium">
          Height (m) <span className="text-red-500">*</span>
        </Label>
        <Input
          id="antennaHeight"
          name="antennaHeight"
          type="number"
          value={formData.antennaHeight}
          onChange={handleInputChange}
          placeholder="Enter height in meters"
          disabled={isSubmitting}
          className="mt-1"
        />
      </div>

      <div>
        <Label htmlFor="azimuth" className="text-sm font-medium">
          Azimuth (°) <span className="text-red-500">*</span>
        </Label>
        <Input
          id="azimuth"
          name="azimuth"
          type="number"
          value={formData.azimuth}
          onChange={handleInputChange}
          placeholder="Enter azimuth in degrees"
          disabled={isSubmitting}
          className="mt-1"
        />
      </div>

      <div>
        <Label htmlFor="beamTilt" className="text-sm font-medium">
          Beam Tilt (°) <span className="text-red-500">*</span>
        </Label>
        <Input
          id="beamTilt"
          name="beamTilt"
          type="number"
          value={formData.beamTilt}
          onChange={handleInputChange}
          placeholder="Enter beam tilt in degrees"
          disabled={isSubmitting}
          className="mt-1"
        />
      </div>

      <div className="flex gap-3 pt-4">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={isSubmitting}
          className="flex-1"
        >
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting} className="flex-1">
          {isSubmitting ? "Saving..." : "Save Changes"}
        </Button>
      </div>
    </form>
  );
}
