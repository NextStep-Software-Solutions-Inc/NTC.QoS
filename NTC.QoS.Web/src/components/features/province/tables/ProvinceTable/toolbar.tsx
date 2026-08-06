import React from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, Trash } from "lucide-react";

import { type ProvinceColumn } from "@/components/features/province/types/ProvinceColumn";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import CreateProvinceDialog from "../../dialogs/CreateProvinceDialog";
import DeleteProvinceDialog from "../../dialogs/DeleteProvinceDialog";

interface ToolbarProps {
  table: Table<ProvinceColumn>;
}
export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);

  return (
    <>
      <DeleteProvinceDialog
        ids={table.getSelectedRowModel().rows.map((r) => r.original.id)}
        open={openDeleteDialog}
        onOpenChange={() => setOpenDeleteDialog(false)}
      />
      <Input
        placeholder="Search name..."
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
          {table.getSelectedRowModel().rows.length > 0 && (
            <PermissionCheck action="delete" actions={actions}>
              <Button
                variant="destructive"
                disabled={table.getSelectedRowModel().rows.length === 0}
                onClick={() => {
                  const selectedRows = table.getSelectedRowModel().rows;
                  if (selectedRows.length > 0) {
                    setOpenDeleteDialog(true);
                  } else {
                    alert("Please select a province to delete.");
                  }
                }}>
                <Trash />
                Delete Selected {table.getSelectedRowModel().rows.length}
              </Button>
            </PermissionCheck>
          )}

          <PermissionCheck action="write" actions={actions}>
            <CreateProvinceDialog />
          </PermissionCheck>
        </>
      )}
    </>
  );
}
