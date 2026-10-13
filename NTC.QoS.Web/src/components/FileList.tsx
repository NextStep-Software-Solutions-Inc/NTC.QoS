"use client";

import React from "react";
import { useParams } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { type File } from "./features/file/types/File";
import { getFilesRequest } from "./features/file/handlers/getFilesRequest";
import FileTable from "./features/file/tables/FileTable";

const useGetFiles = createQueryHookFromTemplate(getFilesRequest);

interface FileListProps {
  type: "Speedtest" | "NAP" | "Cell" | "Cellular" | "CellularCoverage" | "WirelessValidation";
}

function FileList({ type }: FileListProps) {
  const { region } = useParams<{ region: string }>();
  const { data: files, isLoading } = useGetFiles({
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

  return (
    <div className="">
      <FileTable data={files?.data ?? []} actions={["write"]} readonly={false} isLoading={isLoading} type={type} />
    </div>
  );
}

export default FileList;
