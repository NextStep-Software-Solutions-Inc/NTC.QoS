import React from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, Trash } from "lucide-react";
import { toast } from "sonner";

import { type Technology } from "@/components/features/technology/types/Technology";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import CreateTechnologyDialog from "../../dialogs/CreateTechnologyDialog";
import DeleteTechnologiesDialog from "../../dialogs/DeleteTechnologiesDialog";

interface ToolbarProps {
  table: Table<Technology>;
}

// Utility function for getting selected rows
function getSelectedRows(table: Table<Technology>) {
  return table.getSelectedRowModel().rows;
}

export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);

  const [openDialog, setOpenDialog] = React.useState(false);

  return (
    <>
      <DeleteTechnologiesDialog
        open={openDeleteDialog}
        onOpenChange={setOpenDeleteDialog}
        ids={getSelectedRows(table).map((row) => row.original.id)}
        onSuccess={() => table.resetRowSelection()}
      />

      <CreateTechnologyDialog open={openDialog} onOpenChange={setOpenDialog} />

      <Input
        placeholder="Search Technology"
        value={(table.getColumn("standard")?.getFilterValue() as string) ?? ""}
        onChange={(event) => table.getColumn("standard")?.setFilterValue(event.target.value)}
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
              Add Technology
            </Button>
          </PermissionCheck>
          {getSelectedRows(table).length > 0 && (
            <PermissionCheck action="delete" actions={actions}>
              <Button
                variant="destructive"
                onClick={() => {
                  if (getSelectedRows(table).length === 0) {
                    toast.error("No technologies selected for deletion.");
                    return;
                  }
                  setOpenDeleteDialog(true);
                }}>
                <Trash className="mr-2 h-4 w-4" />
                Delete
              </Button>
            </PermissionCheck>
          )}
        </>
      )}
    </>
  );
}
