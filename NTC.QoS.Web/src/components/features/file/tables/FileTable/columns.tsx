import { type ColumnDef } from "@tanstack/react-table";
import { AlertCircle, ArrowUpDown } from "lucide-react";

import { type File } from "@/components/features/file/types/File";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import Actions from "./actions";

const columns: ColumnDef<File>[] = [
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
          Name
          <ArrowUpDown />
        </Button>
      );
    },
    cell: ({ row }) => (
      <div className="lowercase">
        <div className="font-medium">{row.getValue("name")}</div>
        <div className="text-muted-foreground">{row.original.path}</div>
      </div>
    ),
  },
  {
    accessorKey: "type",
    header: () => <div className="text-right">Type</div>,
    cell: ({ row }) => {
      return <div className="text-right font-medium">{row.getValue("type")}</div>;
    },
  },
  {
    accessorKey: "status",
    header: "Status",
    cell: ({ row }) => {
      const status = (row.original.status || (row.getValue("status") as string)) ?? "";
      const errorMessage = row.original.errorMessage;
      const isFailedOrPartial = status === "Failed" || status === "PartiallyProcessed";

      if (isFailedOrPartial && errorMessage) {
        return (
          <Tooltip>
            <TooltipTrigger asChild>
              <div className="inline-flex items-center gap-1.5 cursor-help">
                <span
                  className={
                    status === "Failed"
                      ? "px-2 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300"
                      : "px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300"
                  }
                >
                  {status === "PartiallyProcessed" ? "Partial" : status}
                </span>
                <AlertCircle className="h-4 w-4 text-red-500 shrink-0" />
              </div>
            </TooltipTrigger>
            <TooltipContent className="max-w-xs md:max-w-sm p-3 text-xs leading-relaxed bg-popover text-popover-foreground border shadow-md">
              <p className="font-semibold mb-1 text-destructive">Import / Validation Details:</p>
              <p className="whitespace-pre-wrap">{errorMessage}</p>
              {row.original.errorLogPath && (
                <p className="mt-2 text-muted-foreground text-[11px] italic">
                  Tip: Open the Actions menu (...) to download the error report (.csv).
                </p>
              )}
            </TooltipContent>
          </Tooltip>
        );
      }

      return (
        <span
          className={
            status === "Success"
              ? "px-2 py-0.5 rounded-full text-xs font-semibold bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300"
              : status === "Processing"
              ? "px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300 animate-pulse"
              : status === "Failed"
              ? "px-2 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300"
              : status === "PartiallyProcessed"
              ? "px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300"
              : "capitalize"
          }
        >
          {status === "PartiallyProcessed" ? "Partial" : status || "-"}
        </span>
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
