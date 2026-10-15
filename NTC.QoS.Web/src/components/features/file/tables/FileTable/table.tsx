"use client";

import React from "react";
import {
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnFiltersState,
  type SortingState,
  type VisibilityState,
} from "@tanstack/react-table";
import { FileX } from "lucide-react";

import columns from "@/components/features/file/tables/FileTable/columns";
import Toolbar from "@/components/features/file/tables/FileTable/toolbar";
import { type File } from "@/components/features/file/types/File";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { type MetaActions } from "@/types/global";

interface FileTableProps {
  title?: string;
  description?: string;
  data: File[];
  onRowDoubleClick?: (row: File) => void;
  readonly?: boolean;
  actions: MetaActions;
  isLoading?: boolean;
  type?: "Speedtest" | "NAP" | "Cell" | "Cellular" | "CellularCoverage" | "WirelessValidation";
}

export function FileTable({
  description,
  title,
  onRowDoubleClick,
  readonly = true,
  actions,
  data,
  isLoading = false,
  type,
}: FileTableProps) {
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = React.useState<VisibilityState>({});
  const [rowSelection, setRowSelection] = React.useState({});

  const handleOnRowDoubleClick = (row: (typeof data)[number]) => {
    onRowDoubleClick?.(row);
  };

  const table = useReactTable({
    data,
    columns,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    onColumnVisibilityChange: setColumnVisibility,
    onRowSelectionChange: setRowSelection,
    state: { sorting, columnFilters, columnVisibility, rowSelection },
    meta: { readonly, actions },
  });

  return (
    <div className="w-full">
      {title && <h2 className="text-2xl font-bold mb-2">{title}</h2>}
      {description && <p className="text-muted-foreground mb-4">{description}</p>}
      <div className="mb-4">
        <Toolbar table={table} type={type} />
      </div>
      <div className="rounded-xl border-2 border-violet-200/50 dark:border-violet-800/50 shadow-xl shadow-violet-500/10 overflow-hidden bg-card">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  return (
                    <TableHead key={header.id}>
                      {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              // Loading skeleton
              Array.from({ length: 5 }).map((_, index) => (
                <TableRow key={index}>
                  {columns.map((_, colIndex) => (
                    <TableCell key={colIndex}>
                      <div className="h-6 bg-gradient-to-r from-muted via-muted/50 to-muted rounded animate-pulse" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && "selected"}
                  onDoubleClick={() => handleOnRowDoubleClick(row.original)}
                  className={cn({ "cursor-pointer": Boolean(onRowDoubleClick) })}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-64 text-center">
                  <div className="flex flex-col items-center justify-center gap-4 py-8">
                    <div className="relative">
                      <FileX
                        className="h-20 w-20 text-muted-foreground/30 animate-bounce"
                        style={{ animationDuration: "2s" }}
                      />
                      <div className="absolute inset-0 bg-gradient-to-t from-violet-500/20 to-transparent blur-xl" />
                    </div>
                    <div className="space-y-2">
                      <p className="text-lg font-semibold text-muted-foreground">No files found</p>
                      <p className="text-sm text-muted-foreground/60">Upload your first file to get started</p>
                    </div>
                  </div>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <div className="flex items-center justify-end space-x-2 py-4">
        <div className="flex-1 text-sm text-muted-foreground">
          {table.getFilteredSelectedRowModel().rows.length} of {table.getFilteredRowModel().rows.length} row(s)
          selected.
        </div>
        <div className="space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}>
            Previous
          </Button>
          <Button variant="outline" size="sm" onClick={() => table.nextPage()} disabled={!table.getCanNextPage()}>
            Next
          </Button>
        </div>
      </div>
    </div>
  );
}
