"use client";

import { type ColumnDef } from "@tanstack/react-table";
import { ChevronDown, ChevronRight } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";

// This type is used to define the shape of our data.
export type Report = {
  id: string;
  carrier: string;
  provinceCity: string;
  siteId: string;
  siteAddress: string;
  barangay: string;
  lat: string;
  long: string;
  gsm2g: { frequencies: { f900: boolean; f1800: boolean }; antennaHeight: string; azimuth: string };
  wcdma3g: { antennaHeight: string; azimuth: string };
  fddLte4g: { antennaHeight: string; azimuth: string };
  tddLte4g: { antennaHeight: string; azimuth: string };
  fiveG: { antennaHeight: string; azimuth: string };
};

export const columns: ColumnDef<Report>[] = [
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
  { accessorKey: "carrier", header: "Carrier" },
  { accessorKey: "provinceCity", header: "Province City/Municipality" },
  { accessorKey: "siteId", header: "Site ID" },
  { accessorKey: "siteAddress", header: "Site Address" },
  { accessorKey: "barangay", header: "Barangay" },
  { accessorKey: "lat", header: "LAT" },
  { accessorKey: "long", header: "LONG" },
  {
    id: "gsm2g",
    header: ({ table }) => {
      const isExpanded = table.getIsAllRowsExpanded();
      return (
        <Button
          variant="ghost"
          onClick={() => table.toggleAllRowsExpanded(!isExpanded)}
          className="p-0 hover:bg-transparent">
          {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
          GSM 2G
        </Button>
      );
    },
    columns: [
      {
        accessorKey: "gsm2g.frequencies.f900",
        header: "900",
        cell: ({ row }) => <Checkbox checked={row.original.gsm2g.frequencies.f900} disabled aria-label="900 MHz" />,
      },
      {
        accessorKey: "gsm2g.frequencies.f1800",
        header: "1800",
        cell: ({ row }) => <Checkbox checked={row.original.gsm2g.frequencies.f1800} disabled aria-label="1800 MHz" />,
      },
      { accessorKey: "gsm2g.antennaHeight", header: "Antenna Height (m) - Tower Type" },
      { accessorKey: "gsm2g.azimuth", header: "Azimuth (°)" },
    ],
  },
  {
    id: "3g_wcdma",
    header: "3G_WCDMA",
    columns: [
      { accessorKey: "wcdma3g.antennaHeight", header: "Antenna Height (m) - Tower Type" },
      { accessorKey: "wcdma3g.azimuth", header: "Azimuth (°)" },
    ],
  },
  {
    id: "4g_fdd_lte",
    header: "4G FDD_LTE",
    columns: [
      { accessorKey: "fddLte4g.antennaHeight", header: "Antenna Height (m) - Tower Type" },
      { accessorKey: "fddLte4g.azimuth", header: "Azimuth (°)" },
    ],
  },
  {
    id: "4g_tdd_lte",
    header: "4G TDD_LTE",
    columns: [
      { accessorKey: "tddLte4g.antennaHeight", header: "Antenna Height (m) - Tower Type" },
      { accessorKey: "tddLte4g.azimuth", header: "Azimuth (°)" },
    ],
  },
  {
    id: "5g",
    header: "5G",
    columns: [
      { accessorKey: "fiveG.antennaHeight", header: "Antenna Height (m) - Tower Type" },
      { accessorKey: "fiveG.azimuth", header: "Azimuth (°)" },
    ],
  },
];
