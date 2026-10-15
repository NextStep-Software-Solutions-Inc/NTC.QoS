import React from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, Trash } from "lucide-react";
import { toast } from "sonner";

import { type Generation } from "@/components/features/generation/types/Generation";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import CreateGenerationDialog from "../../dialogs/CreateGenerationDialog";
import DeleteGenerationsDialog from "../../dialogs/DeleteGenerationsDialog";

interface ToolbarProps {
  table: Table<Generation>;
}

function getSelectedRows(table: Table<Generation>) {
  return table.getSelectedRowModel().rows;
}

export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);

  const [openDialog, setOpenDialog] = React.useState(false);

  return (
    <>
      <DeleteGenerationsDialog
        open={openDeleteDialog}
        onOpenChange={setOpenDeleteDialog}
        ids={getSelectedRows(table).map((row) => row.original.id)}
        onSuccess={() => table.resetRowSelection()}
      />

      <CreateGenerationDialog open={openDialog} onOpenChange={setOpenDialog} />

      <Input
        placeholder="Search Generation"
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
            .map((column) => {
              return (
                <DropdownMenuCheckboxItem
                  key={column.id}
                  className="capitalize"
                  checked={column.getIsVisible()}
                  onCheckedChange={(value) => column.toggleVisibility(!!value)}>
                  {column.id}
                </DropdownMenuCheckboxItem>
              );
            })}
        </DropdownMenuContent>
      </DropdownMenu>
      {!readonly && (
        <>
          <PermissionCheck action="write" actions={actions}>
            <Button
              variant="default"
              onClick={() => {
                setOpenDialog(true);
              }}>
              Add Generation
            </Button>
          </PermissionCheck>
          {table.getSelectedRowModel().rows.length > 0 && (
            <PermissionCheck action="delete" actions={actions}>
              <Button
                variant="destructive"
                disabled={getSelectedRows(table).length === 0}
                onClick={() => {
                  const selectedRows = getSelectedRows(table);
                  if (selectedRows.length > 0) {
                    setOpenDeleteDialog(true);
                  } else {
                    toast.error("Please select a generation to delete.");
                  }
                }}>
                <Trash />
                Delete Selected {getSelectedRows(table).length}
              </Button>
            </PermissionCheck>
          )}
        </>
      )}
    </>
  );
}
