import React from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, Trash } from "lucide-react";
import { toast } from "sonner";

import { type AreaStudy } from "@/components/features/area-study/types/AreaStudy";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import CreateAreaStudyDialog from "../../dialogs/CreateAreaStudyDialog";
import DeleteAreaStudiesDialog from "../../dialogs/DeleteAreaStudiesDialog";

interface ToolbarProps {
  table: Table<AreaStudy>;
}
export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);
  const [openDialog, setOpenDialog] = React.useState(false);

  const handleOnClick = () => {
    const selectedRows = table.getSelectedRowModel().rows;
    if (selectedRows.length > 0) {
      setOpenDeleteDialog(true);
    } else {
      toast.error("Please select an area study to delete.");
    }
  };

  return (
    <>
      {openDeleteDialog && (
        <DeleteAreaStudiesDialog
          open={openDeleteDialog}
          onOpenChange={setOpenDeleteDialog}
          ids={table.getSelectedRowModel().rows.map((row) => row.original.id)}
          onSuccess={() => table.resetRowSelection()}
        />
      )}

      {openDialog && <CreateAreaStudyDialog open={openDialog} onOpenChange={setOpenDialog} />}

      <Input
        placeholder="Search area study..."
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
              Add Area Study
            </Button>
          </PermissionCheck>
          {table.getSelectedRowModel().rows.length > 0 && (
            <PermissionCheck action="delete" actions={actions}>
              <Button variant="destructive" onClick={handleOnClick}>
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
