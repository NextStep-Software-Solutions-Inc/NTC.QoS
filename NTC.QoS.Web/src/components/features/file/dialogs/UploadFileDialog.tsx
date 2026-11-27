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
      size="wide"
      title={
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-violet-500/15 via-purple-500/15 to-indigo-500/15 text-violet-600 dark:text-violet-400 border border-violet-200/50 dark:border-violet-800/50 shadow-sm">
            {type === "CellularCoverage" ? (
              <Upload className="h-5 w-5 text-violet-600 dark:text-violet-400" />
            ) : (
              <Upload className="h-5 w-5 text-primary" />
            )}
          </div>
          <div>
            <span className="text-lg font-semibold tracking-tight text-gray-900 dark:text-gray-100">
              Upload {displayType} Files
            </span>
          </div>
        </div>
      }
      description={
        <span className="text-xs text-muted-foreground">
          {type === "CellularCoverage" ? (
            <>
              Upload coverage datasets in <span className="font-semibold text-violet-600 dark:text-violet-400">.mbtiles</span> format (or GeoJSON/GeoPackage, up to 1 GB)
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
