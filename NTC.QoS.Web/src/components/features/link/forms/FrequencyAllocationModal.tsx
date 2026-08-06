"use client";

import { useState } from "react";
import { ArrowUpDown, Radio } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import CenterFrequencyBandwidthForm from "./CenterFrequencyBandwidthForm";
import UpperLowerFrequencyForm from "./UpperLowerFrequencyForm";

interface FrequencyAllocationModalProps {
  isOpen: boolean;
  onClose: () => void;
}

type InputType = "center-bandwidth" | "upper-lower" | null;

export default function FrequencyAllocationModal({ isOpen, onClose }: FrequencyAllocationModalProps) {
  const [selectedInputType, setSelectedInputType] = useState<InputType>(null);

  const handleClose = () => {
    setSelectedInputType(null);
    onClose();
  };

  const handleBack = () => {
    setSelectedInputType(null);
  };

  const handleFormSubmit = () => {
    // Handle form submission
    handleClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-md">
        {!selectedInputType ? (
          // Step 1: Input Type Selection
          <>
            <DialogHeader>
              <DialogTitle>Add New Frequency Allocation</DialogTitle>
              <p className="text-sm text-gray-500">Please select an Input Type</p>
            </DialogHeader>

            <div className="space-y-3 py-4">
              <button
                onClick={() => setSelectedInputType("center-bandwidth")}
                className="w-full flex items-center gap-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 text-left">
                <Radio className="w-5 h-5 text-gray-600" />
                <span className="font-medium">Center Frequency + Bandwidth</span>
              </button>

              <button
                onClick={() => setSelectedInputType("upper-lower")}
                className="w-full flex items-center gap-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 text-left">
                <ArrowUpDown className="w-5 h-5 text-gray-600" />
                <span className="font-medium">Upper Frequency + Lower Frequency</span>
              </button>
            </div>

            <div className="flex justify-end pt-4">
              <Button variant="outline" onClick={handleClose} className="w-full bg-transparent">
                Cancel
              </Button>
            </div>
          </>
        ) : (
          // Step 2: Form based on selection
          <>
            {selectedInputType === "center-bandwidth" && (
              <CenterFrequencyBandwidthForm onCreated={handleFormSubmit} onClose={handleBack} />
            )}
            {selectedInputType === "upper-lower" && (
              <UpperLowerFrequencyForm onCreated={handleFormSubmit} onClose={handleBack} />
            )}
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
