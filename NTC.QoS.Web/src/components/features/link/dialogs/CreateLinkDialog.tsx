"use client";

import { useState } from "react";
import { BarChart3, Radio } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import CenterFrequencyBandwidthForm from "../forms/CenterFrequencyBandwidthForm";
import UpperLowerFrequencyForm from "../forms/UpperLowerFrequencyForm";

export interface Link {
  id: string;
  provider: string;
  band: string;
  centerFrequency?: number;
  bandwidth?: number;
  upperFrequency?: number;
  lowerFrequency?: number;
  rxThreshold: number;
}

interface CreateLinkDialogProps {
  onCreated?: (link: Link) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
interface ResponsiveDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title?: string;
  description?: string;
  children: React.ReactNode;
}

function ResponsiveDialog({ open, onOpenChange, title, description, children }: ResponsiveDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        {(title ?? description) && (
          <DialogHeader>
            {title && <DialogTitle>{title}</DialogTitle>}
            {description && <DialogDescription>{description}</DialogDescription>}
          </DialogHeader>
        )}
        {children}
      </DialogContent>
    </Dialog>
  );
}

export default function CreateLinkDialog({ onCreated, open, onOpenChange }: CreateLinkDialogProps) {
  const [selectedType, setSelectedType] = useState<string | null>(null);

  const handleLinkCreated = (link: Link) => {
    onOpenChange(false);
    setSelectedType(null);
    onCreated?.(link);
  };

  const handleCancel = () => {
    onOpenChange(false);
    setSelectedType(null);
  };

  const handleBack = () => {
    setSelectedType(null);
  };

  const renderContent = () => {
    if (!selectedType) {
      return (
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground mb-6">Please select an input type</p>

          <div className="space-y-3">
            <button
              onClick={() => setSelectedType("center-bandwidth")}
              className="w-full flex items-center gap-3 p-4 border rounded-lg hover:bg-muted/50 transition-colors text-left">
              <Radio className="h-5 w-5 text-muted-foreground" />
              <span className="font-medium">Center Frequency + Bandwidth</span>
            </button>

            <button
              onClick={() => setSelectedType("upper-lower")}
              className="w-full flex items-center gap-3 p-4 border rounded-lg hover:bg-muted/50 transition-colors text-left">
              <BarChart3 className="h-5 w-5 text-muted-foreground" />
              <span className="font-medium">Upper Frequency + Lower Frequency</span>
            </button>
          </div>

          <div className="flex justify-end pt-4">
            <Button variant="outline" onClick={handleCancel}>
              Cancel
            </Button>
          </div>
        </div>
      );
    }

    if (selectedType === "center-bandwidth") {
      return <CenterFrequencyBandwidthForm onCreated={() => handleLinkCreated({} as Link)} onClose={handleBack} />;
    }

    if (selectedType === "upper-lower") {
      return <UpperLowerFrequencyForm onCreated={() => handleLinkCreated({} as Link)} onClose={handleBack} />;
    }

    return null;
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={!selectedType ? "Add New Frequency Allocation" : undefined}
      description="">
      {renderContent()}
    </ResponsiveDialog>
  );
}
