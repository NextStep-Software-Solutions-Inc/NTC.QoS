import { type ColumnDef } from "@tanstack/react-table";
import { ChevronsUpDown } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { type FrequencyAllocation } from "../../types/FrequencyAllocation";
import Actions from "./actions";

// ─── Frequency Range Bar Cell ─────────────────────────────────────────────────
function FrequencyRangeCell({
  link,
}: {
  link: { lowerFrequency?: number; centerFrequency?: number; upperFrequency?: number } | null | undefined;
}) {
  if (!link) return <span className="text-muted-foreground">—</span>;

  const lo = link.lowerFrequency;
  const hi = link.upperFrequency;
  const center = link.centerFrequency;
  const fmt = (v: number | undefined) => (v !== undefined && v !== null ? String(v) : "—");

  const hasBounds = lo !== undefined && lo !== null && hi !== undefined && hi !== null;
  const hasCenter = center !== undefined && center !== null;
  const centerPct =
    hasBounds && hasCenter && hi !== lo ? Math.max(0, Math.min(100, ((center! - lo!) / (hi! - lo!)) * 100)) : 50;

  return (
    <TooltipProvider delayDuration={200}>
      <Tooltip>
        <TooltipTrigger asChild>
          <div className="min-w-[130px] max-w-[200px] cursor-default space-y-1 select-none">
            {/* Range label */}
            <p className="text-sm font-semibold tabular-nums text-foreground leading-none">
              {fmt(lo)}–{fmt(hi)}{" "}
              <span className="text-[10px] font-normal text-muted-foreground">MHz</span>
            </p>

            {/* Bar track */}
            <div className="relative h-1.5 w-full rounded-full bg-muted">
              {/* Fill */}
              <div className="absolute inset-0 rounded-full bg-primary/25" />

              {/* Center dot */}
              {hasBounds && hasCenter && (
                <div
                  className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 h-3 w-3 rounded-full bg-primary border-2 border-background shadow-sm"
                  style={{ left: `${centerPct}%` }}
                />
              )}
            </div>

            {/* Min / Max labels */}
            <div className="flex justify-between text-[10px] text-muted-foreground tabular-nums leading-none">
              <span>{fmt(lo)}</span>
              <span>{fmt(hi)}</span>
            </div>
          </div>
        </TooltipTrigger>

        <TooltipContent side="top" className="text-xs leading-relaxed">
          <div>
            <span className="font-semibold text-rose-400">L:</span> {fmt(lo)} MHz
          </div>
          <div>
            <span className="font-semibold text-emerald-400">C:</span> {fmt(center)} MHz
          </div>
          <div>
            <span className="font-semibold text-sky-400">U:</span> {fmt(hi)} MHz
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

const columns: ColumnDef<FrequencyAllocation>[] = [
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
    id: "name",
    accessorFn: (row) => row.name,
    enableHiding: false,
    header: ({ column }) => (
      <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
        Name
        <ChevronsUpDown />
      </Button>
    ),
    cell: ({ row }) => <div className="font-medium">{row.getValue("name") ?? "—"}</div>,
  },
  {
    id: "providerName",
    accessorFn: (row) => row.provider?.name ?? "",
    header: ({ column }) => (
      <Button variant="ghost" onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}>
        Provider
        <ChevronsUpDown />
      </Button>
    ),
    cell: ({ row }) => <div>{row.getValue("providerName") ?? "—"}</div>,
  },
  {
    id: "band",
    header: "Band",
    cell: ({ row }) => {
      const bandName = row.original.upLink?.band?.name ?? row.original.downLink?.band?.name;
      return <div>{bandName ?? "—"}</div>;
    },
  },
  {
    id: "uplink",
    header: "Uplink (MHz)",
    cell: ({ row }) => <FrequencyRangeCell link={row.original.upLink} />,
  },
  {
    id: "downlink",
    header: "Downlink (MHz)",
    cell: ({ row }) => <FrequencyRangeCell link={row.original.downLink} />,
  },
  {
    id: "status",
    header: "Status",
    cell: ({ row }) => {
      const isActive = row.original.isActive;
      return (
        <Badge
          className={
            isActive
              ? "bg-green-100 text-green-700 hover:bg-green-600 hover:text-white transition-colors cursor-default dark:bg-green-200 dark:text-green-800"
              : "bg-gray-100 text-gray-500 hover:bg-gray-500 hover:text-white transition-colors cursor-default dark:bg-gray-200 dark:text-gray-600"
          }>
          {isActive ? "Active" : "Inactive"}
        </Badge>
      );
    },
  },
  {
    id: "actions",
    enableHiding: false,
    cell: ({ row, table }) => {
      const readonly = table.options.meta?.readonly;
      return <Actions data={row.original} readonly={readonly} />;
    },
  },
];

export default columns;
