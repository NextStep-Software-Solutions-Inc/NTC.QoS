import { type ColumnDef } from "@tanstack/react-table";
import { ChevronsUpDown } from "lucide-react";

import { type Link } from "@/components/features/link/types/Link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import Actions from "./actions";

const columns: ColumnDef<Link>[] = [
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
    id: "type",
    accessorFn: (row) => row.type,
    enableHiding: false,
    header: ({ column }) => {
      return (
        <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
          Type
          <ChevronsUpDown />
        </Button>
      );
    },
    cell: ({ row }) => <div>{row.getValue("type")}</div>,
  },
  {
    id: "providerName",
    accessorFn: (row) => row.serviceProvider?.provider?.name ?? "",
    enableHiding: false,
    header: ({ column }) => (
      <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
        Provider Name
        <ChevronsUpDown />
      </Button>
    ),
    cell: ({ row }) => <div>{row.getValue("providerName") ?? "—"}</div>,
  },
  {
    accessorKey: "band",
    header: "Band",
    cell: ({ row }) => {
      const band = row.original?.band?.name;
      return (
        <div className="max-w-[300px] truncate" title={band}>
          {band ?? "—"}
        </div>
      );
    },
  },
  {
    accessorKey: "frequency",
    header: "Frequency (MHz)",
    cell: ({ row }) => {
      const lowerFrequency = row.original.lowerFrequency;
      const centerFrequency = row.original.centerFrequency;
      const upperFrequency = row.original.upperFrequency;
      const formatValue = (val: number | string | undefined) =>
        val !== undefined && val !== null && val !== "" ? val : "—";

      const badgeBase = "variant-secondary px-3 py-1 rounded-full shadow-sm font-semibold ring-1 ring-white";
      return (
        <div className="flex gap-2 items-center text-left whitespace-nowrap">
          <Badge className={`${badgeBase} bg-red-100 text-red-700 dark:bg-red-200 dark:text-red-800`}>
            L: {formatValue(lowerFrequency)}
          </Badge>
          <Badge className={`${badgeBase} bg-green-100 text-green-700 dark:bg-green-200 dark:text-green-800`}>
            C: {formatValue(centerFrequency)}
          </Badge>
          <Badge className={`${badgeBase} bg-blue-100 text-blue-700 dark:bg-blue-200 dark:text-blue-800`}>
            U: {formatValue(upperFrequency)}
          </Badge>
        </div>
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
