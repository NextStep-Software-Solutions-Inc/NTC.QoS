import { type ColumnDef } from "@tanstack/react-table";
import { ChevronsUpDown } from "lucide-react";

// import { type ServiceProviderColumn } from "@/components/features/service-provider/types/ServiceProviderColumn";
import { type Provider } from "@/components/features/provider/types/Provider";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import Actions from "./actions";

const columns: ColumnDef<Provider>[] = [
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
          Provider Name
          <ChevronsUpDown />
        </Button>
      );
    },
    cell: ({ row }) => <div>{row.getValue("name")}</div>,
  },
  {
    accessorKey: "licenseNumber",
    header: "License",
    cell: ({ row }) => <div>{row.getValue("licenseNumber") ?? "-"}</div>,
  },
  {
    accessorKey: "color",
    enableHiding: false,
    header: "Provider Color",
    cell: ({ row }) => (
      <div className="flex items-center gap-2">
        <span className="min-w-5 aspect-square rounded" style={{ backgroundColor: row.getValue("color") }}></span>
        <div>{row.getValue("color")}</div>
      </div>
    ),
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
