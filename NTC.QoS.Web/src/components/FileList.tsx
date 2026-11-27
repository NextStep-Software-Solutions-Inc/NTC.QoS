"use client";

import React, { useMemo } from "react";
import { useParams } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getTilesetsRequest } from "./features/cellular-coverage/handlers/getTilesetsRequest";
import { type CellularCoverageTileset } from "./features/cellular-coverage/types/CellularCoverageTileset";
import { type File } from "./features/file/types/File";
import { getFilesRequest } from "./features/file/handlers/getFilesRequest";
import FileTable from "./features/file/tables/FileTable";

const useGetFiles = createQueryHookFromTemplate(getFilesRequest);
const useGetTilesets = createQueryHookFromTemplate(getTilesetsRequest);

function formatFileSize(bytes?: number): string {
  if (!bytes || bytes <= 0) return "";
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  return `${Math.max(1, Math.round(bytes / 1024))} KB`;
}

function mapTilesetToFileRow(t: CellularCoverageTileset): File {
  const statusText = t.statusText || String(t.status ?? "");
  const sizeLabel = formatFileSize(t.fileSize);
  const stateLabel =
    statusText === "Ready"
      ? t.isActive
        ? "Active MapTiler Tileset"
        : "Replaced MapTiler Tileset"
      : "MapTiler Tileset";

  return {
    id: t.id,
    createdAt: t.createdAt,
    createdBy: "",
    updatedAt: t.createdAt,
    updatedBy: "",
    isDeleted: false,
    deletedAt: "",
    deletedBy: "",
    name: t.fileName || t.name,
    path: sizeLabel ? `${stateLabel} • ${sizeLabel}` : stateLabel,
    uploadedBy: "",
    type: "MBTiles",
    status: statusText,
    errorMessage: t.errorMessage ?? null,
    errorLogPath: null,
  };
}

interface FileListProps {
  type: "Speedtest" | "NAP" | "Cell" | "Cellular" | "CellularCoverage" | "WirelessValidation";
}

function FileList({ type }: FileListProps) {
  const { region } = useParams<{ region: string }>();
  const isCellularCoverage = type === "CellularCoverage";

  const { data: files, isLoading: isLoadingFiles } = useGetFiles({
    pathParams: {},
    queryParams: { types: type, pageNumber: 1, pageSize: 1000, regionIds: region ? [region] : [] },
    refetchInterval: (query) => {
      const fileList = query.state.data?.data;
      const hasActive = fileList?.some((f: File) => {
        const s = (f.status || "").toLowerCase();
        return s === "pending" || s === "processing" || s === "queued";
      });
      return hasActive ? 3000 : false;
    },
  });

  const { data: tilesets, isLoading: isLoadingTilesets } = useGetTilesets({
    pathParams: {},
    queryParams: { regionId: region || undefined },
    enabled: isCellularCoverage,
    refetchInterval: (query) => {
      const list = query.state.data?.data;
      const hasActive = list?.some((t: CellularCoverageTileset) => {
        const s = (t.statusText || String(t.status ?? "")).toLowerCase();
        return s === "uploading" || s === "processing" || s === "pending" || s === "queued";
      });
      return hasActive ? 3000 : false;
    },
  });

  const mergedData = useMemo(() => {
    const fileRows = files?.data ?? [];
    if (!isCellularCoverage) return fileRows;

    const tilesetRows = (tilesets?.data ?? []).map(mapTilesetToFileRow);
    return [...tilesetRows, ...fileRows].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  }, [files?.data, tilesets?.data, isCellularCoverage]);

  const isLoading = isLoadingFiles || (isCellularCoverage && isLoadingTilesets);

  return (
    <div className="">
      <FileTable data={mergedData} actions={["write"]} readonly={false} isLoading={isLoading} type={type} />
    </div>
  );
}

export default FileList;
