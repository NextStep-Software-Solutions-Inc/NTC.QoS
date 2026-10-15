"use client";

import React from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { updateSectorRequest } from "../../sector/handlers/updateSectorRequest";
import { type Sector } from "../../sector/types/Sector";

const useUpdateSector = createMutationHookFromTemplate(updateSectorRequest);

interface EditAntennaDetailsFormProps {
  sectorId: string;
  sector: Sector;
  onCancel: () => void;
  onSuccess: () => void;
}

export default function EditAntennaDetailsForm({
  sectorId,
  sector,
  onCancel,
  onSuccess,
}: EditAntennaDetailsFormProps) {
  const [formData, setFormData] = React.useState<Partial<Sector>>({
    antennaType: sector.antennaType || "",
    number: sector.number || 0,
    radioEquipment: sector.radioEquipment || "",
    isRxEqualTx: sector.isRxEqualTx || false,
    txPower: sector.txPower || 0,
    txAntennaModel: sector.txAntennaModel || "",
    txCableType: sector.txCableType || "",
    txCableLength: sector.txCableLength || 0,
    txCableLoss: sector.txCableLoss || 0,
    txAdditionalLoss: sector.txAdditionalLoss || 0,
    txAntennaHeight: sector.txAntennaHeight || 0,
    txAntennaGain: sector.txAntennaGain || 0,
    txAzimuth: sector.txAzimuth || 0,
    txBeamTilt: sector.txBeamTilt || 0,
    txEIRP: sector.txEIRP || 0,
    rxAntennaModel: sector.rxAntennaModel || "",
    rxCableType: sector.rxCableType || "",
    rxCableLength: sector.rxCableLength || 0,
    rxCableLoss: sector.rxCableLoss || 0,
    rxAdditionalLoss: sector.rxAdditionalLoss || 0,
    rxAntennaHeight: sector.rxAntennaHeight || 0,
    rxAntennaGain: sector.rxAntennaGain || 0,
    rxAzimuth: sector.rxAzimuth || 0,
    rxBeamTilt: sector.rxBeamTilt || 0,
    rxEIRP: sector.rxEIRP || 0,
  });
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  const { mutateAsync: updateSector } = useUpdateSector();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "number" ? parseFloat(value) || 0 : value,
    }));
  };

  const handleCheckboxChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: checked,
    }));
  };

  const handleSelectChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      await updateSector({
        pathParams: { id: sector.id },
        payload: formData,
      });

      toast.success("Antenna details updated successfully");
      onSuccess();
    } catch (error: any) {
      console.error(error);
      toast.error(error?.response?.data?.message || "Failed to update antenna details");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 p-4 flex-1 min-h-0 overflow-hidden">
      <Tabs defaultValue="basic" className="flex flex-col flex-1 min-h-0 overflow-hidden">
        <TabsList className="grid w-full grid-cols-3 shrink-0">
          <TabsTrigger value="basic">Basic</TabsTrigger>
          <TabsTrigger value="tx">TX Setup</TabsTrigger>
          <TabsTrigger value="rx">RX Setup</TabsTrigger>
        </TabsList>

        {/* Basic Tab */}
        <TabsContent value="basic" className="overflow-y-auto flex-1 mt-4 pr-1">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="antennaType" className="text-sm font-medium">
                Antenna Type
              </Label>
              <select
                id="antennaType"
                name="antennaType"
                value={formData.antennaType || ""}
                onChange={handleSelectChange}
                disabled={isSubmitting}
                className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
              >
                <option value="">Select antenna type</option>
                <option value="Unknown">Unknown</option>
                <option value="Omnidirectional">Omnidirectional</option>
                <option value="Sector">Sector</option>
                <option value="Yagi">Yagi</option>
                <option value="ParabolicDish">Parabolic Dish</option>
                <option value="SmallCell">Small Cell</option>
              </select>
            </div>

            <div>
              <Label htmlFor="number" className="text-sm font-medium">
                Number
              </Label>
              <Input
                id="number"
                name="number"
                type="number"
                value={formData.number || 0}
                onChange={handleInputChange}
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="radioEquipment" className="text-sm font-medium">
                Radio Equipment <span className="text-red-500">*</span>
              </Label>
              <Input
                id="radioEquipment"
                name="radioEquipment"
                value={formData.radioEquipment || ""}
                onChange={handleInputChange}
                placeholder="Equipment model/type"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div className="flex items-center gap-2 pt-6">
              <input
                id="isRxEqualTx"
                name="isRxEqualTx"
                type="checkbox"
                checked={formData.isRxEqualTx || false}
                onChange={handleCheckboxChange}
                disabled={isSubmitting}
                className="rounded border-gray-300"
              />
              <Label htmlFor="isRxEqualTx" className="text-sm font-medium cursor-pointer mb-0">
                RX = TX
              </Label>
            </div>
          </div>
        </TabsContent>

        {/* TX Setup Tab */}
        <TabsContent value="tx" className="overflow-y-auto flex-1 mt-4 pr-1">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="txPower" className="text-sm font-medium">
                TX Power <span className="text-red-500">*</span>
              </Label>
              <Input
                id="txPower"
                name="txPower"
                type="number"
                value={formData.txPower || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txAntennaModel" className="text-sm font-medium">
                TX Antenna Model <span className="text-red-500">*</span>
              </Label>
              <Input
                id="txAntennaModel"
                name="txAntennaModel"
                value={formData.txAntennaModel || ""}
                onChange={handleInputChange}
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txCableType" className="text-sm font-medium">
                TX Cable Type
              </Label>
              <Input
                id="txCableType"
                name="txCableType"
                value={formData.txCableType || ""}
                onChange={handleInputChange}
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txCableLength" className="text-sm font-medium">
                TX Cable Length (m)
              </Label>
              <Input
                id="txCableLength"
                name="txCableLength"
                type="number"
                value={formData.txCableLength || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txCableLoss" className="text-sm font-medium">
                TX Cable Loss (dB)
              </Label>
              <Input
                id="txCableLoss"
                name="txCableLoss"
                type="number"
                value={formData.txCableLoss || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txAdditionalLoss" className="text-sm font-medium">
                TX Additional Loss (dB)
              </Label>
              <Input
                id="txAdditionalLoss"
                name="txAdditionalLoss"
                type="number"
                value={formData.txAdditionalLoss || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txAntennaHeight" className="text-sm font-medium">
                TX Antenna Height (m)
              </Label>
              <Input
                id="txAntennaHeight"
                name="txAntennaHeight"
                type="number"
                value={formData.txAntennaHeight || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txAntennaGain" className="text-sm font-medium">
                TX Antenna Gain (dBi)
              </Label>
              <Input
                id="txAntennaGain"
                name="txAntennaGain"
                type="number"
                value={formData.txAntennaGain || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txAzimuth" className="text-sm font-medium">
                TX Azimuth (°)
              </Label>
              <Input
                id="txAzimuth"
                name="txAzimuth"
                type="number"
                value={formData.txAzimuth || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txBeamTilt" className="text-sm font-medium">
                TX Beam Tilt (°)
              </Label>
              <Input
                id="txBeamTilt"
                name="txBeamTilt"
                type="number"
                value={formData.txBeamTilt || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="txEIRP" className="text-sm font-medium">
                TX EIRP (dBm)
              </Label>
              <Input
                id="txEIRP"
                name="txEIRP"
                type="number"
                value={formData.txEIRP || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>
          </div>
        </TabsContent>

        {/* RX Setup Tab */}
        <TabsContent value="rx" className="overflow-y-auto flex-1 mt-4 pr-1">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="rxAntennaModel" className="text-sm font-medium">
                RX Antenna Model <span className="text-red-500">*</span>
              </Label>
              <Input
                id="rxAntennaModel"
                name="rxAntennaModel"
                value={formData.rxAntennaModel || ""}
                onChange={handleInputChange}
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxCableType" className="text-sm font-medium">
                RX Cable Type
              </Label>
              <Input
                id="rxCableType"
                name="rxCableType"
                value={formData.rxCableType || ""}
                onChange={handleInputChange}
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxCableLength" className="text-sm font-medium">
                RX Cable Length (m)
              </Label>
              <Input
                id="rxCableLength"
                name="rxCableLength"
                type="number"
                value={formData.rxCableLength || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxCableLoss" className="text-sm font-medium">
                RX Cable Loss (dB)
              </Label>
              <Input
                id="rxCableLoss"
                name="rxCableLoss"
                type="number"
                value={formData.rxCableLoss || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxAdditionalLoss" className="text-sm font-medium">
                RX Additional Loss (dB)
              </Label>
              <Input
                id="rxAdditionalLoss"
                name="rxAdditionalLoss"
                type="number"
                value={formData.rxAdditionalLoss || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxAntennaHeight" className="text-sm font-medium">
                RX Antenna Height (m) <span className="text-red-500">*</span>
              </Label>
              <Input
                id="rxAntennaHeight"
                name="rxAntennaHeight"
                type="number"
                value={formData.rxAntennaHeight || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxAntennaGain" className="text-sm font-medium">
                RX Antenna Gain (dBi) <span className="text-red-500">*</span>
              </Label>
              <Input
                id="rxAntennaGain"
                name="rxAntennaGain"
                type="number"
                value={formData.rxAntennaGain || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxAzimuth" className="text-sm font-medium">
                RX Azimuth (°) <span className="text-red-500">*</span>
              </Label>
              <Input
                id="rxAzimuth"
                name="rxAzimuth"
                type="number"
                value={formData.rxAzimuth || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxBeamTilt" className="text-sm font-medium">
                RX Beam Tilt (°) <span className="text-red-500">*</span>
              </Label>
              <Input
                id="rxBeamTilt"
                name="rxBeamTilt"
                type="number"
                value={formData.rxBeamTilt || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="rxEIRP" className="text-sm font-medium">
                RX EIRP (dBm)
              </Label>
              <Input
                id="rxEIRP"
                name="rxEIRP"
                type="number"
                value={formData.rxEIRP || 0}
                onChange={handleInputChange}
                step="0.1"
                disabled={isSubmitting}
                className="mt-1"
              />
            </div>
          </div>
        </TabsContent>
      </Tabs>

      <div className="flex gap-3 pt-2 border-t shrink-0">
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
