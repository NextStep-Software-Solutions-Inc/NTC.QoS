import { type ColumnDef } from "@tanstack/react-table";
import { ArrowUpDown } from "lucide-react";

import { type ProvinceColumn } from "@/components/features/province/types/ProvinceColumn";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import Actions from "./actions";

const columns: ColumnDef<ProvinceColumn>[] = [
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
    header: ({ column }) => {
      return (
        <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
          Province
          <ArrowUpDown />
        </Button>
      );
    },
    cell: ({ row }) => <div className="capitalize">{row.getValue("name")}</div>,
  },
  {
    accessorKey: "municipalityCount",
    header: "# of municipalities",
    cell: ({ row }) => <div className="capitalize">{row.getValue("municipalityCount") ?? "-"}</div>,
  },
  {
    accessorKey: "barangayCount",
    header: "# of barangays",
    cell: ({ row }) => <div className="capitalize">{row.getValue("barangayCount") ?? "-"}</div>,
  },
  {
    accessorKey: "ispCount",
    header: "# of ISPs",
    cell: ({ row }) => <div className="capitalize">{row.getValue("ispCount") ?? "-"}</div>,
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
