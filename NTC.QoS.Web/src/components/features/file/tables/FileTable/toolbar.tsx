"use client";

import React, { useState } from "react";
import { useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, FileText } from "lucide-react";

import { type File } from "@/components/features/file/types/File";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { useInvalidateQuery } from "@/hooks/invalidate-query";
import { UPLOAD_GUIDES } from "../../constants/uploadGuides";
import UploadFileDialog from "../../dialogs/UploadFileDialog";

type FileType = "Speedtest" | "NAP" | "Cell" | "Cellular" | "WirelessValidation" | "CellularCoverage";

interface ToolbarProps {
  table: Table<File>;
  type?: FileType;
}
export default function Toolbar({ table, type: propType }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];
  const [openUpload, setOpenUpload] = useState(false);
  const invalidateQuery = useInvalidateQuery();
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const type = propType ?? (searchParams.get("type") as FileType | null);

  // Handle file upload success - invalidate all relevant queries
  const handleUploadSuccess = () => {
    // Always invalidate files list
    invalidateQuery("/files");
    
    // Invalidate data and analytics based on file type
    if (type === "Speedtest") {
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "speedtests", refetchType: "active" });
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-speedtests-infinite", refetchType: "all" });
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "speedtest-analytics", refetchType: "active" });
    } else if (type === "NAP") {
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs", refetchType: "active" });
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-NAPs-infinite", refetchType: "all" });
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "nap-analytics", refetchType: "active" });
    } else if (type === "Cell") {
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells-infinite", refetchType: "all" });
    }
  };

  const guide = type ? UPLOAD_GUIDES[type] : null;

  return (
    <div className="flex flex-col sm:flex-row gap-3 w-full">
      <Input
        placeholder="Search File..."
        value={(table.getColumn("name")?.getFilterValue() as string) ?? ""}
        onChange={(event) => table.getColumn("name")?.setFilterValue(event.target.value)}
        className="max-w-sm"
      />
      <div className="flex gap-2 sm:ml-auto items-center">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline">
              Columns <ChevronDown className="ml-2 h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end">
            {table
              .getAllColumns()
              .filter((column) => column.getCanHide())
              .map((column) => {
                return (
                  <DropdownMenuCheckboxItem
                    key={column.id}
                    className="capitalize"
                    checked={column.getIsVisible()}
                    onCheckedChange={(value) => column.toggleVisibility(!!value)}>
                    {column.id}
                  </DropdownMenuCheckboxItem>
                );
              })}
          </DropdownMenuContent>
        </DropdownMenu>

        {guide && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <a
                  href={guide.pdfPath}
                  target="_blank"
                  rel="noopener noreferrer"
                  download={guide.pdfFileName}
                  className="inline-flex items-center gap-1.5 h-10 px-3 py-2 text-xs sm:text-sm font-medium border border-input rounded-md bg-background hover:bg-accent hover:text-accent-foreground transition-colors shadow-xs">
                  <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                  <span className="hidden sm:inline">
                    Upload Guide{type === "CellularCoverage" || type === "Cellular" ? " (WIP)" : ""}
                  </span>
                </a>
              </TooltipTrigger>
              <TooltipContent side="bottom" className="max-w-xs text-xs p-3 bg-slate-900 text-white shadow-xl">
                <p className="font-semibold text-blue-300 mb-1.5">{guide.title} - Key Rules:</p>
                <ul className="list-disc pl-4 space-y-1 text-slate-200 text-[11px]">
                  {guide.rules.map((rule, idx) => (
                    <li key={idx}>{rule}</li>
                  ))}
                </ul>
                <p className="mt-2 text-[10px] text-blue-300 underline font-medium">Click to download SOP manual (PDF)</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        {!readonly && (
          <PermissionCheck action="write" actions={actions}>
            <Button
              onClick={() => setOpenUpload(true)}
              className="relative group overflow-hidden bg-gradient-to-r from-violet-600 via-fuchsia-600 to-pink-600 hover:from-violet-700 hover:via-fuchsia-700 hover:to-pink-700 text-white font-semibold shadow-lg hover:shadow-xl transition-all duration-300 transform hover:scale-105">
              <span className="relative z-10">Upload</span>
              <div className="absolute inset-0 bg-gradient-to-r from-pink-600 via-fuchsia-600 to-violet-600 opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
            </Button>
            {type && (
              <UploadFileDialog
                type={type}
                open={openUpload}
                onOpenChange={setOpenUpload}
                onUpload={handleUploadSuccess}
              />
            )}
          </PermissionCheck>
        )}
      </div>
    </div>
  );
}
