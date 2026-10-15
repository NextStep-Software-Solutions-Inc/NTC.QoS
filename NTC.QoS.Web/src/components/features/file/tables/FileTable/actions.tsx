"use client";

import { useState } from "react";
import { Copy, Download, FileWarning, MoreHorizontal, Pencil, Trash } from "lucide-react";
import { toast } from "sonner";

import { UpdateFileDialog } from "@/components/features/file/dialogs/UpdateFileDialog";
import { type File } from "@/components/features/file/types/File";
import { type PreSignedUrl } from "@/components/features/file/types/PreSignedUrl";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { request } from "@/services/fetch";
import { type APIResponse } from "@/types/reusable/api";
import DeleteFileDialog from "../../dialogs/DeleteFileDialog";

interface ActionsProps {
  data: File;
  readonly?: boolean;
}

export default function Actions({ data, readonly }: ActionsProps) {
  const [openUpdate, setOpenUpdate] = useState(false);
  const [openDelete, setOpenDelete] = useState(false);
  const handleCopyName = (name: string) => {
    void navigator.clipboard.writeText(name);
    toast.info("Copied to clipboard");
  };

  const handleDownload = async (fileId: string) => {
    try {
      toast.info("Generating secure download link...");
      const res = await request.get<APIResponse<PreSignedUrl>>(`/files/${fileId}/presigned-download-url`);
      if (res.succeeded && res.data?.url) {
        window.open(res.data.url, "_blank");
        toast.success("Download started");
      } else {
        toast.error(res.message || "Failed to generate download link");
      }
    } catch {
      toast.error("Error downloading file");
    }
  };

  const handleDownloadErrorReport = async (fileId: string) => {
    if (!data.errorLogPath) {
      toast.info("No error report file is available for this import.");
      return;
    }

    try {
      toast.info("Generating error report download link...");
      const res = await request.get<APIResponse<PreSignedUrl>>(`/files/${fileId}/presigned-error-log-url`);
      if (res.succeeded && res.data?.url) {
        window.open(res.data.url, "_blank");
        toast.success("Error report download started");
      } else {
        toast.error(res.message || "No error report available for this file");
      }
    } catch (err: unknown) {
      const errObj = err as { response?: { data?: { message?: string } }; message?: string };
      const msg = errObj?.response?.data?.message || errObj?.message || "Error downloading error report";
      toast.error(msg);
    }
  };

  return (
    <>
      {!readonly && (
        <>
          <UpdateFileDialog file={data} open={openUpdate} onOpenChange={setOpenUpdate} />
          <DeleteFileDialog id={data.id} open={openDelete} onOpenChange={setOpenDelete} />
        </>
      )}
      <DropdownMenu modal={false}>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" className="h-8 w-8 p-0">
            <span className="sr-only">Open menu</span>
            <MoreHorizontal />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuLabel>Actions</DropdownMenuLabel>
          <DropdownMenuItem onClick={() => handleDownload(data.id)}>
            <Download />
            Download File
          </DropdownMenuItem>
          {data.errorLogPath ? (
            <DropdownMenuItem
              className="text-amber-600 dark:text-amber-400 focus:text-amber-700"
              onClick={() => void handleDownloadErrorReport(data.id)}
            >
              <FileWarning />
              Download Error Report (.csv)
            </DropdownMenuItem>
          ) : (data.status === "Failed" || data.status === "PartiallyProcessed") ? (
            <DropdownMenuItem
              disabled
              className="opacity-50 cursor-not-allowed"
              onClick={() => void handleDownloadErrorReport(data.id)}
            >
              <FileWarning />
              No Error Report File
            </DropdownMenuItem>
          ) : null}
          <DropdownMenuItem onClick={() => handleCopyName(data.id)}>
            <Copy />
            Copy File ID
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem className="text-yellow-900" onSelect={() => setOpenUpdate(true)}>
            <Pencil />
            Edit File
          </DropdownMenuItem>
          <DropdownMenuItem className="text-destructive" onSelect={() => setOpenDelete(true)}>
            <Trash />
            Delete File
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );
}
