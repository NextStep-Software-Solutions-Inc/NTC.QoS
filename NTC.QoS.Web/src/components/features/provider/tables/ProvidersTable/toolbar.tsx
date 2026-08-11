import React from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, Trash } from "lucide-react";

import CreatedProviderDialog from "@/components/features/provider/dialogs/CreatedProviderDialog";
import CreateProviderDialog from "@/components/features/provider/dialogs/CreateProviderDialog";
import DeleteProvidersDialog from "@/components/features/provider/dialogs/DeleteProvidersDialog";
import { type Provider } from "@/components/features/provider/types/Provider";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import AddServiceProviderDialog from "../../../service-provider/dialogs/AddServiceProviderDialog";

interface ToolbarProps {
  table: Table<Provider>;
}
export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);

  const [openProviderDialog, setOpenProviderDialog] = React.useState(false);
  const [openProviderCreatedDialog, setOpenProviderCreatedDialog] = React.useState(false);
  const [openCreateServiceProviderDialog, setOpenCreateServiceProviderDialog] = React.useState(false);
  const [createdProvider, setCreatedProvider] = React.useState<Provider | null>(null);

  const handleOnProviderCreated = (provider: Provider) => {
    setOpenProviderDialog(false);
    setOpenProviderCreatedDialog(true);
    setCreatedProvider(provider);
  };

  const handleOnCreateServiceProvider = () => {
    setOpenProviderCreatedDialog(false);
    setOpenCreateServiceProviderDialog(true);
  };

  return (
    <>
      <DeleteProvidersDialog
        open={openDeleteDialog}
        onOpenChange={setOpenDeleteDialog}
        ids={table.getSelectedRowModel().rows.map((row) => row.original.id)}
        onSuccess={() => table.resetRowSelection()}
      />

      <CreateProviderDialog
        open={openProviderDialog}
        onOpenChange={setOpenProviderDialog}
        onCreated={handleOnProviderCreated}
      />

      {createdProvider && (
        <CreatedProviderDialog
          provider={createdProvider}
          open={openProviderCreatedDialog}
          onOpenChange={setOpenProviderCreatedDialog}
          onPrev={() => setOpenProviderDialog(true)}
          onNext={() => setOpenCreateServiceProviderDialog(true)}
          onSuccessDialogClose={() => setOpenProviderCreatedDialog(false)}
        />
      )}

      {createdProvider && (
        <AddServiceProviderDialog
          provider={createdProvider}
          onClose={() => setOpenCreateServiceProviderDialog(false)}
          open={openCreateServiceProviderDialog}
          onOpenChange={handleOnCreateServiceProvider}
          onSuccessDialogClose={() => setOpenCreateServiceProviderDialog(false)}
        />
      )}

      <Input
        placeholder="Search provider..."
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
            <Button
              variant="default"
              onClick={() => {
                setOpenProviderDialog(true);
              }}>
              Add Provider
            </Button>
          </PermissionCheck>
        </>
      )}
    </>
  );
}
