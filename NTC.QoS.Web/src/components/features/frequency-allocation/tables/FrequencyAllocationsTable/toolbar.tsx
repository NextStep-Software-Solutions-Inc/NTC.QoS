import React from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, Trash } from "lucide-react";

import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import CreateFrequencyAllocationDialog from "../../dialogs/CreateFrequencyAllocationDialog";
import DeleteFrequencyAllocationsDialog from "../../dialogs/DeleteFrequencyAllocationsDialog";
import { type FrequencyAllocation } from "../../types/FrequencyAllocation";

interface ToolbarProps {
  table: Table<FrequencyAllocation>;
}

export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  const [openCreateDialog, setOpenCreateDialog] = React.useState(false);
  const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);

  return (
    <>
      {openCreateDialog && (
        <CreateFrequencyAllocationDialog open={openCreateDialog} onOpenChange={setOpenCreateDialog} />
      )}
      {openDeleteDialog && (
        <DeleteFrequencyAllocationsDialog
          open={openDeleteDialog}
          onOpenChange={setOpenDeleteDialog}
          ids={table.getSelectedRowModel().rows.map((row) => row.original.id)}
          onSuccess={() => table.resetRowSelection()}
        />
      )}

      <Input
        placeholder="Search frequency allocation..."
        value={(table.getColumn("name")?.getFilterValue() as string) ?? ""}
        onChange={(event) => table.getColumn("name")?.setFilterValue(event.target.value)}
        className="max-w-sm"
      />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" className="ml-auto">
            Columns <ChevronDown />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {table
            .getAllColumns()
            .filter((column) => column.getCanHide())
            .map((column) => (
              <DropdownMenuCheckboxItem
                key={column.id}
                className="capitalize"
                checked={column.getIsVisible()}
                onCheckedChange={(value) => column.toggleVisibility(!!value)}>
                {column.id}
              </DropdownMenuCheckboxItem>
            ))}
        </DropdownMenuContent>
      </DropdownMenu>

      {!readonly && (
        <>
          <PermissionCheck action="write" actions={actions}>
            <Button variant="default" onClick={() => setOpenCreateDialog(true)}>
              Add Frequency Allocation
            </Button>
          </PermissionCheck>
          {table.getSelectedRowModel().rows.length > 0 && (
            <PermissionCheck action="delete" actions={actions}>
              <Button variant="destructive" onClick={() => setOpenDeleteDialog(true)}>
                <Trash />
                Delete {table.getSelectedRowModel().rows.length}
              </Button>
            </PermissionCheck>
          )}
        </>
      )}
    </>
  );
}
