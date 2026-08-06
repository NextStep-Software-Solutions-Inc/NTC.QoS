import { type ColumnDef } from "@tanstack/react-table";
import { ChevronsUpDown } from "lucide-react";

import { type Generation } from "@/components/features/generation/types/Generation";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import Actions from "./actions";

const columns: ColumnDef<Generation>[] = [
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
          Generation Name
          <ChevronsUpDown />
        </Button>
      );
    },
    cell: ({ row }) => <div>{row.getValue("name")}</div>,
  },
  { accessorKey: "description", header: "Description", cell: ({ row }) => <div>{row.getValue("description")}</div> },
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
