"use client";

import { type ColumnDef } from "@tanstack/react-table";
import { ChevronsUpDown, MapPin, RadioTower } from "lucide-react";

import { type Cell } from "@/components/features/cell/types/Cell";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import Actions from "./actions";

const columns: ColumnDef<Cell>[] = [
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
    enableSorting: false,
    enableHiding: false,
  },

  {
    accessorKey: "name",
    enableHiding: false,
    header: ({ column }) => {
      return (
        <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
          <RadioTower className="mr-2 h-4 w-4" />
          Cell Site Name
          <ChevronsUpDown className="ml-2 h-4 w-4" />
        </Button>
      );
    },
    cell: ({ row }) => (
      <div className="flex items-center gap-2 font-medium">
        <RadioTower className="h-4 w-4 text-muted-foreground" />
        {row.getValue("name") || `Cell ${row.original.id}`}
      </div>
    ),
  },

  {
    accessorKey: "status",
    header: ({ column }) => {
      return (
        <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
          Status
          <ChevronsUpDown className="ml-2 h-4 w-4" />
        </Button>
      );
    },
    cell: ({ row }) => {
      const status = row.getValue("status") as string;
      const statusLower = status?.toLowerCase() || "unknown";

      const statusConfig = {
        unknown: {
          variant: "outline" as const,
          className: "bg-amber-100 hover:bg-amber-200 text-amber-800 border-amber-300",
          icon: "",
        },
        pending: {
          variant: "outline" as const,
          className: "bg-orange-100 hover:bg-orange-200 text-orange-800 border-orange-300",
          icon: "",
        },
        licensed: {
          variant: "default" as const,
          className: "bg-green-100 hover:bg-green-200 text-green-800 border-green-300",
          icon: "",
        },
      };

      const config = statusConfig[statusLower as keyof typeof statusConfig] || statusConfig.unknown;

      return (
        <Badge variant={config.variant} className={config.className}>
          {status || "Unknown"}
        </Badge>
      );
    },
  },

  {
    accessorKey: "serviceProvider",
    header: "Service Provider",
    cell: ({ row }) => {
      const provider = row.original.serviceProvider;
      const providerData = provider?.provider;
      const providerName = providerData?.name || "N/A";
      const providerLogo = providerData?.logo;
      const providerColor = providerData?.color;

      return (
        <div className="flex items-center gap-2">
          {providerLogo ? (
            <div
              className="flex h-8 w-8 items-center justify-center rounded-md border bg-white p-1"
              style={{ borderColor: providerColor || "#e5e7eb" }}>
              <img src={providerLogo} alt={providerName} className="h-full w-full object-contain" />
            </div>
          ) : (
            <div
              className="flex h-8 w-8 items-center justify-center rounded-md border text-xs font-bold text-white"
              style={{ backgroundColor: providerColor || "#6b7280", borderColor: providerColor || "#6b7280" }}>
              {providerName && providerName.length >= 2 ? providerName.substring(0, 2).toUpperCase() : "??"}
            </div>
          )}
          <span className="font-medium">{providerName}</span>
        </div>
      );
    },
  },

  {
    accessorKey: "siteId",
    header: "Site ID",
    cell: ({ row }) => <div className="font-mono text-sm">{row.getValue("siteId") || "N/A"}</div>,
  },

  {
    id: "coordinates",
    header: "Coordinates",
    cell: ({ row }) => {
      const lat = row.original.latitude;
      const lng = row.original.longitude;
      const latStr = typeof lat === "number" ? lat.toFixed(4) : "N/A";
      const lngStr = typeof lng === "number" ? lng.toFixed(4) : "N/A";
      return (
        <div className="flex items-center gap-1 text-sm font-mono">
          <MapPin className="h-3 w-3 text-muted-foreground" />
          <span>
            {latStr}, {lngStr}
          </span>
        </div>
      );
    },
  },

  {
    accessorKey: "elevation",
    header: "Elevation (m)",
    cell: ({ row }) => {
      const elevation = row.getValue("elevation") as number;
      return <div>{elevation ? `${elevation}m` : "N/A"}</div>;
    },
  },

  {
    accessorKey: "isVerified",
    header: "Verified",
    cell: ({ row }) => {
      const verified = row.getValue("isVerified") as boolean;
      return (
        <Badge
          variant={verified ? "default" : "outline"}
          className={
            verified
              ? "bg-blue-500 hover:bg-blue-600 text-white border-blue-600 shadow-sm"
              : "border-gray-300 text-gray-600"
          }>
          {verified ? "✓ Verified" : "Unverified"}
        </Badge>
      );
    },
  },

  {
    id: "actions",
    enableHiding: false,
    cell: ({ row, table }) => {
      const readonly = table.options.meta?.readonly;
      const data = row.original;

      return <Actions data={data} readonly={readonly} />;
    },
  },
];

export default columns;
