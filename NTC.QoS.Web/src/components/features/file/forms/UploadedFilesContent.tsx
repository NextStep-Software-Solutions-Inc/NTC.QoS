import React, { useRef, useState } from "react";
import { CheckCircle2, FileCheck, FileSpreadsheet, Layers, Plus, Trash2, Upload } from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface UploadedFilesContentProps {
  files: File[];
  handleRemoveFile: (index: number) => void;
  handleFileChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onFilesAdded?: (files: File[]) => void;
  type?: string;
}

const getFileTypeBadge = (fileName: string) => {
  const ext = fileName.toLowerCase().split(".").pop();
  switch (ext) {
    case "mbtiles":
      return {
        label: "MBTILES",
        color: "bg-violet-100 text-violet-700 border-violet-200 dark:bg-violet-900/50 dark:text-violet-300 dark:border-violet-800",
      };
    case "geojson":
      return {
        label: "GEOJSON",
        color: "bg-indigo-100 text-indigo-700 border-indigo-200 dark:bg-indigo-900/50 dark:text-indigo-300 dark:border-indigo-800",
      };
    case "gpkg":
      return {
        label: "GPKG",
        color: "bg-cyan-100 text-cyan-700 border-cyan-200 dark:bg-cyan-900/50 dark:text-cyan-300 dark:border-cyan-800",
      };
    case "xlsx":
    case "xls":
      return {
        label: "EXCEL",
        color: "bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/50 dark:text-emerald-300 dark:border-emerald-800",
      };
    case "csv":
      return {
        label: "CSV",
        color: "bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/50 dark:text-amber-300 dark:border-amber-800",
      };
    default:
      return {
        label: ext?.toUpperCase() ?? "FILE",
        color: "bg-gray-100 text-gray-700 border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-700",
      };
  }
};

const formatFileSize = (bytes: number): string => {
  if (bytes >= 1024 * 1024 * 1024) {
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
};

const UploadedFilesContent = ({
  files,
  handleRemoveFile,
  handleFileChange,
  onFilesAdded,
  type,
}: UploadedFilesContentProps) => {
  const isCellularCoverage = type === "CellularCoverage";
  const acceptedTypes = isCellularCoverage ? ".mbtiles,.geojson,.gpkg,.xlsx,.csv" : ".xlsx";
  const [isDragging, setIsDragging] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      if (onFilesAdded) {
        onFilesAdded(Array.from(e.dataTransfer.files));
      } else {
        handleFileChange({ target: { files: e.dataTransfer.files } } as unknown as React.ChangeEvent<HTMLInputElement>);
      }
    }
  };

  const openFilePicker = () => {
    inputRef.current?.click();
  };

  return (
    <div className="py-2 space-y-3">
      <input
        ref={inputRef}
        id="file-upload"
        type="file"
        accept={acceptedTypes}
        multiple
        className="hidden"
        onChange={handleFileChange}
      />

      {files.length === 0 ? (
        <div
          role="button"
          tabIndex={0}
          onClick={openFilePicker}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              openFilePicker();
            }
          }}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          className={cn(
            "group relative flex flex-col items-center justify-center p-7 rounded-xl border-2 border-dashed transition-all duration-200 cursor-pointer text-center",
            isDragging
              ? "border-violet-500 bg-violet-50/70 dark:bg-violet-950/40 ring-4 ring-violet-500/20 scale-[1.01]"
              : "border-gray-200 dark:border-zinc-800 hover:border-violet-400 dark:hover:border-violet-600 bg-gray-50/40 dark:bg-zinc-900/30 hover:bg-violet-50/20 dark:hover:bg-violet-950/10"
          )}>
          <div className="relative mb-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-violet-500/10 via-purple-500/10 to-indigo-500/10 dark:from-violet-400/20 dark:to-indigo-400/20 text-violet-600 dark:text-violet-400 border border-violet-200/50 dark:border-violet-800/40 shadow-sm transition-transform duration-200 group-hover:scale-110">
              <Upload className="h-6 w-6" />
            </div>
          </div>

          <div className="space-y-1">
            <p className="text-sm font-semibold text-gray-900 dark:text-gray-100">
              Drag & drop files here, or <span className="text-violet-600 dark:text-violet-400 underline underline-offset-2">browse</span>
            </p>
            <p className="text-xs text-muted-foreground">
              Select files from your local storage to upload
            </p>
          </div>

          <div className="mt-4 flex items-center justify-center gap-1.5 flex-wrap">
            {isCellularCoverage ? (
              <>
                <span className="inline-flex items-center gap-1 rounded-md bg-violet-100/70 dark:bg-violet-900/40 px-2 py-0.5 text-[11px] font-medium text-violet-700 dark:text-violet-300 border border-violet-200/60 dark:border-violet-800/50">
                  <Layers className="h-3 w-3" /> .mbtiles
                </span>
                <span className="inline-flex items-center gap-1 rounded-md bg-indigo-100/70 dark:bg-indigo-900/40 px-2 py-0.5 text-[11px] font-medium text-indigo-700 dark:text-indigo-300 border border-indigo-200/60 dark:border-indigo-800/50">
                  .geojson
                </span>
                <span className="inline-flex items-center gap-1 rounded-md bg-cyan-100/70 dark:bg-cyan-900/40 px-2 py-0.5 text-[11px] font-medium text-cyan-700 dark:text-cyan-300 border border-cyan-200/60 dark:border-cyan-800/50">
                  .gpkg
                </span>
                <span className="text-[11px] text-muted-foreground font-medium pl-1">
                  • Max 1 GB per file
                </span>
              </>
            ) : (
              <>
                <span className="inline-flex items-center gap-1 rounded-md bg-emerald-100/70 dark:bg-emerald-900/40 px-2 py-0.5 text-[11px] font-medium text-emerald-700 dark:text-emerald-300 border border-emerald-200/60 dark:border-emerald-800/50">
                  <FileSpreadsheet className="h-3 w-3" /> Excel (.xlsx)
                </span>
                <span className="text-[11px] text-muted-foreground font-medium pl-1">
                  • Max 25 MB per file
                </span>
              </>
            )}
          </div>
        </div>
      ) : (
        <div className="space-y-3">
          <div className="flex items-center justify-between px-1">
            <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Selected File{files.length > 1 ? "s" : ""} ({files.length})
            </span>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={openFilePicker}
              className="h-7 text-xs font-medium text-violet-600 dark:text-violet-400 hover:text-violet-700 hover:bg-violet-50 dark:hover:bg-violet-950/40 gap-1 px-2">
              <Plus className="h-3.5 w-3.5" />
              Add more
            </Button>
          </div>

          <div
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={cn(
              "max-h-[220px] overflow-y-auto space-y-2 rounded-xl p-1 transition-all",
              isDragging && "ring-2 ring-violet-500 bg-violet-50/30 rounded-xl"
            )}>
            {files.map((file, index) => {
              const badge = getFileTypeBadge(file.name);
              return (
                <div
                  key={`${file.name}-${index}`}
                  className="group flex items-center justify-between gap-3 rounded-lg border border-gray-200 dark:border-zinc-800 bg-white dark:bg-zinc-900/60 px-3.5 py-2.5 shadow-sm hover:border-violet-300 dark:hover:border-violet-700 transition-all">
                  <div className="flex items-center gap-3 min-w-0 flex-1">
                    <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gray-100 dark:bg-zinc-800 text-gray-600 dark:text-gray-300 shrink-0">
                      {file.name.toLowerCase().endsWith(".mbtiles") ? (
                        <Layers className="h-4 w-4 text-violet-600 dark:text-violet-400" />
                      ) : (
                        <FileCheck className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="truncate text-sm font-medium text-gray-900 dark:text-gray-100" title={file.name}>
                          {file.name}
                        </span>
                        <span className={cn("rounded px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wider border shrink-0", badge.color)}>
                          {badge.label}
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
                        <span>{formatFileSize(file.size)}</span>
                        <span>•</span>
                        <span className="text-emerald-600 dark:text-emerald-400 font-medium flex items-center gap-1">
                          <CheckCircle2 className="h-3 w-3 inline" /> Ready
                        </span>
                      </div>
                    </div>
                  </div>

                  <button
                    onClick={() => handleRemoveFile(index)}
                    className="p-1.5 rounded-md text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors shrink-0"
                    title="Remove file"
                    type="button">
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};

export default UploadedFilesContent;
