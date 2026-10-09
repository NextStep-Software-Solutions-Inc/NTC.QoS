import React from "react";
import Image from "next/image";
import { FileCheck, Trash2, Upload } from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface UploadedFilesContentProps {
  files: File[];
  handleRemoveFile: (index: number) => void;
  handleFileChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
  type?: string;
}

const UploadedFilesContent = ({ files, handleRemoveFile, handleFileChange, type }: UploadedFilesContentProps) => {
  const isCellularCoverage = type === "CellularCoverage";
  const acceptedTypes = isCellularCoverage ? ".mbtiles,.geojson,.gpkg,.xlsx,.csv" : ".xlsx";
  return (
    <div className="grid gap-4 py-4">
      <div
        className={cn(
          "grid min-h-[200px] w-full gap-4 rounded-xl border-2 border-dashed p-6 transition-all",
          files.length > 0
            ? "items-start border-violet-300 dark:border-violet-700 bg-gradient-to-br from-violet-50/50 to-fuchsia-50/50 dark:from-violet-950/20 dark:to-fuchsia-950/20"
            : "items-center border-muted-foreground/25 hover:border-violet-400 dark:hover:border-violet-600 hover:bg-gradient-to-br hover:from-violet-50/30 hover:to-fuchsia-50/30 dark:hover:from-violet-950/10 dark:hover:to-fuchsia-950/10",
        )}>
        {files.length === 0 ? (
          <section className="grid justify-items-center space-y-4">
            <div className="relative">
              <div className="absolute inset-0 bg-gradient-to-r from-violet-500 to-fuchsia-500 rounded-full blur-2xl opacity-20 animate-pulse" />
              <Upload className="h-16 w-16 text-muted-foreground/40 relative" />
            </div>
            <div className="text-center space-y-1">
              <p className="font-semibold text-muted-foreground">No files uploaded yet</p>
              <p className="text-xs text-muted-foreground/60">Click below to choose files</p>
            </div>
          </section>
        ) : (
          <ul className="w-full space-y-2">
            {files.map((file, index) => (
              <li
                key={index}
                className="group flex items-center justify-between rounded-lg border border-violet-200 dark:border-violet-800 bg-gradient-to-r from-violet-50 to-fuchsia-50 dark:from-violet-950/40 dark:to-fuchsia-950/40 px-4 py-3 text-sm shadow-sm hover:shadow-md transition-all">
                <span className="flex items-center gap-3 flex-1 min-w-0">
                  <FileCheck className="h-5 w-5 text-violet-600 dark:text-violet-400 flex-shrink-0" />
                  <span className="truncate font-medium">{file.name}</span>
                  {file.name.toLowerCase().endsWith(".mbtiles") && (
                    <span className="rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-semibold text-primary">
                      MBTILES
                    </span>
                  )}
                  <span className="text-xs text-muted-foreground flex-shrink-0">
                    {file.size >= 1024 * 1024 * 1024
                      ? `${(file.size / (1024 * 1024 * 1024)).toFixed(2)} GB`
                      : `${(file.size / (1024 * 1024)).toFixed(2)} MB`}
                  </span>
                </span>
                <button
                  onClick={() => handleRemoveFile(index)}
                  className="ml-3 p-1.5 rounded-md text-red-500 hover:text-white hover:bg-red-500 transition-all flex-shrink-0"
                  title="Remove"
                  type="button">
                  <Trash2 size={16} />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
      <div className="mx-auto mt-2">
        <section className="grid justify-center space-y-3">
          <input id="file-upload" type="file" accept={acceptedTypes} multiple className="hidden" onChange={handleFileChange} />
          <label htmlFor="file-upload" className="flex justify-center">
            <Button
              type="button"
              className="relative group overflow-hidden bg-gradient-to-r from-violet-600 via-fuchsia-600 to-pink-600 hover:from-violet-700 hover:via-fuchsia-700 hover:to-pink-700 text-white font-semibold shadow-lg hover:shadow-xl transition-all duration-300 transform hover:scale-105 px-8 py-5 rounded-xl"
              onClick={() => document.getElementById("file-upload")?.click()}>
              <span className="relative z-10 flex items-center gap-2">
                <Upload className="h-4 w-4" />
                Choose Files
              </span>
              <div className="absolute inset-0 bg-gradient-to-r from-pink-600 via-fuchsia-600 to-violet-600 opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
            </Button>
          </label>
          <span className="text-center text-xs text-muted-foreground bg-muted/50 px-3 py-1.5 rounded-full">
            {isCellularCoverage
              ? "🗺️ MBTiles (.mbtiles), GeoJSON, or GeoPackage • Max 1 GB per file"
              : "📊 Excel files only (.xlsx) • Max 25 MB per file"}
          </span>
        </section>
      </div>
    </div>
  );
};

export default UploadedFilesContent;
