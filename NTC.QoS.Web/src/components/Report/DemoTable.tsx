"use client";

import { Fragment, useEffect, useState } from "react";
import { flexRender, getCoreRowModel, getExpandedRowModel, useReactTable, type ColumnDef } from "@tanstack/react-table";
import { ChevronDown, ChevronUp, Info } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";

type Item = {
  id: number;
  name: string;
  email: string;
  location: string;
  flag: string;
  status: "Active" | "Inactive" | "Pending";
  balance: number;
  note?: string;
};

const columns: ColumnDef<Item>[] = [
  {
    id: "expander",
    header: () => null,
    cell: ({ row }) => {
      return row.getCanExpand() ? (
        <Button
          {...{
            className: "size-7 shadow-none text-muted-foreground",
            onClick: row.getToggleExpandedHandler(),
            "aria-expanded": row.getIsExpanded(),
            "aria-label": row.getIsExpanded()
              ? `Collapse details for ${row.original.name}`
              : `Expand details for ${row.original.name}`,
            size: "icon",
            variant: "ghost",
          }}>
          {row.getIsExpanded() ? (
            <ChevronUp className="opacity-60" size={16} strokeWidth={2} aria-hidden="true" />
          ) : (
            <ChevronDown className="opacity-60" size={16} strokeWidth={2} aria-hidden="true" />
          )}
        </Button>
      ) : undefined;
    },
  },
  {
    id: "select",
    header: ({ table }) => (
      <Checkbox
        checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")}
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label="Select all"
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        aria-label="Select row"
      />
    ),
  },
  { header: "Name", accessorKey: "name", cell: ({ row }) => <div className="font-medium">{row.getValue("name")}</div> },
  { header: "Email", accessorKey: "email" },
  {
    header: "Location",
    accessorKey: "location",
    cell: ({ row }) => (
      <div>
        <span className="text-lg leading-none">{row.original.flag}</span> {row.getValue("location")}
      </div>
    ),
  },
  {
    header: "Status",
    accessorKey: "status",
    cell: ({ row }) => (
      <Badge className={cn(row.getValue("status") === "Inactive" && "bg-muted-foreground/60 text-primary-foreground")}>
        {row.getValue("status")}
      </Badge>
    ),
  },
  {
    header: () => <div className="text-right">Balance</div>,
    accessorKey: "balance",
    cell: ({ row }) => {
      const amount = parseFloat(row.getValue("balance"));
      const formatted = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(amount);
      return <div className="text-right">{formatted}</div>;
    },
  },
];

export default function Component() {
  const [data, setData] = useState<Item[]>([]);
  const [expanded, setExpanded] = useState({});
  const [rowSelection, setRowSelection] = useState({});

  useEffect(() => {
    const fetchPosts = async () => {
      try {
        const res = await fetch("https://res.cloudinary.com/dlzlfasou/raw/upload/users-01_fertyx.json");
        if (!res.ok) throw new Error("Failed to fetch data");
        const jsonData: Item[] = (await res.json()) as Item[];
        setData(jsonData.slice(0, 5));
      } catch (error) {
        console.error("Error fetching data:", error);
        setData([]);
      }
    };

    void fetchPosts();
  }, []);

  const table = useReactTable({
    data,
    columns,
    state: { expanded, rowSelection },
    onExpandedChange: setExpanded,
    onRowSelectionChange: setRowSelection,
    getRowCanExpand: (row) => Boolean(row.original.note),
    getCoreRowModel: getCoreRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    enableRowSelection: true,
  });

  return (
    <div className="w-full max-w-[800px] bg-background">
      <div className="mb-4 flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          {table.getFilteredSelectedRowModel().rows.length} of {table.getFilteredRowModel().rows.length} row(s)
          selected.
        </div>
      </div>
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="hover:bg-transparent">
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
            {table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <Fragment key={row.id}>
                  <TableRow data-state={row.getIsSelected() && "selected"}>
                    {row.getVisibleCells().map((cell) => (
                      <TableCell
                        key={cell.id}
                        className="whitespace-nowrap [&:has([aria-expanded])]:w-px [&:has([aria-expanded])]:py-0 [&:has([aria-expanded])]:pr-0">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </TableCell>
                    ))}
                  </TableRow>
                  {row.getIsExpanded() && row.original.note && (
                    <TableRow>
                      <TableCell colSpan={row.getVisibleCells().length}>
                        <div className="flex items-start gap-2 p-2">
                          <Info size={16} strokeWidth={2} className="mt-[2px] shrink-0 opacity-50" aria-hidden="true" />
                          <div className="text-sm text-muted-foreground">{row.original.note}</div>
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                </Fragment>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  No results.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
