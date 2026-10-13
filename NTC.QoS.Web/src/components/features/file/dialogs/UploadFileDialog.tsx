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
          <span className="text-xl font-semibold flex items-center gap-2">
            Upload {displayType} Files
            {(type === "CellularCoverage" || type === "Cellular") && (
              <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-300 border border-amber-300 dark:border-amber-700">
                WIP
              </span>
            )}
          </span>
        </div>
      }
      description={
        <span className="text-muted-foreground">
          Upload files in <span className="font-semibold">Excel</span> format (max 25 MB)
        </span>
      }>
      <UploadFileForm type={type} onClose={() => onOpenChange(false)} onSuccess={onUpload} />
    </ResponsiveDialog>
  );
}

export default UploadFileDialog;
