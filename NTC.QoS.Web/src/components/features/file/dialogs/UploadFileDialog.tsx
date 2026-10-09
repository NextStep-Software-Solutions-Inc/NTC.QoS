import React from "react";
import { Upload } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import UploadFileForm from "../forms/UploadFileForm";

interface UpdateFileDialogProps {
  onUpload?: () => void;
  open: boolean;
  onOpenChange: (state: boolean) => void;
  type: "Speedtest" | "NAP" | "Cell" | "Cellular" | "WirelessValidation" | "CellularCoverage";
}

function UploadFileDialog({ onUpload, open, onOpenChange, type }: UpdateFileDialogProps) {
  // Format type for display with proper spacing
  const displayType = type === "CellularCoverage" 
    ? "Cellular Coverage" 
    : type === "WirelessValidation" 
    ? "Wireless Validation" 
    : type;

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary/10 rounded-lg">
            <Upload className="h-5 w-5 text-primary" />
          </div>
          <span className="text-xl font-semibold">
            Upload {displayType} Files
          </span>
        </div>
      }
      description={
        <span className="text-muted-foreground">
          {type === "CellularCoverage" ? (
            <>
              Upload coverage tiles in <span className="font-semibold text-primary">.mbtiles</span> format (or GeoJSON/GeoPackage, max 1 GB)
            </>
          ) : (
            <>
              Upload files in <span className="font-semibold">Excel</span> format (max 25 MB)
            </>
          )}
        </span>
      }>
      <UploadFileForm type={type} onClose={() => onOpenChange(false)} onSuccess={onUpload} />
    </ResponsiveDialog>
  );
}

export default UploadFileDialog;
