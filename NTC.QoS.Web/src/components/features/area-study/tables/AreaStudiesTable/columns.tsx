import { type ColumnDef } from "@tanstack/react-table";
import { ChevronsUpDown } from "lucide-react";

import { type AreaStudy } from "@/components/features/area-study/types/AreaStudy";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import Actions from "./actions";

const columns: ColumnDef<AreaStudy>[] = [
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
    size: 280,
    minSize: 220,
    header: ({ column }) => {
      return (
        <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
          Name
          <ChevronsUpDown />
        </Button>
      );
    },
    cell: ({ row }) => <div>{row.getValue("name")}</div>,
  },
  {
    accessorKey: "description",
    size: 520,
    minSize: 320,
    header: "Description",
    cell: ({ row }) => {
      const description = row.getValue("description");
      return <div className="min-w-[320px] whitespace-normal break-words">{(description ?? "—") as string}</div>;
    },
  },
  {
    id: "signalLevelConfig",
    size: 220,
    minSize: 180,
    header: "Signal Level",
    cell: ({ row }) => {
      const hasSignalLevel = Boolean(row.original.signalLevelId);
      return hasSignalLevel ? (
        <Badge className="bg-green-100 text-green-700 hover:bg-green-100 dark:bg-green-200 dark:text-green-800">
          Configured
        </Badge>
      ) : (
        <Badge variant="secondary">Not Configured</Badge>
      );
    },
  },
  {
    id: "actions",
    enableHiding: false,
    size: 120,
    minSize: 120,
    cell: ({ row, table }) => {
      const readonly = table.options.meta?.readonly;
      const data = row.original;

      return <Actions data={data} readonly={readonly} />;
    },
  },
];

export default columns;
