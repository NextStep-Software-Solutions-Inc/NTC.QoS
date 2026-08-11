import { type Table } from "@tanstack/react-table";
import { ChevronDown } from "lucide-react";

import { type Template } from "@/components/features/Template/types/Template";
import PermissionCheck from "@/components/PermissionCheck";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";

interface ToolbarProps {
  table: Table<Template>;
}
export default function Toolbar({ table }: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const actions = table.options.meta?.actions ?? [];

  return (
    <>
      <Input
        placeholder="Search email..."
        value={(table.getColumn("email")?.getFilterValue() as string) ?? ""}
        onChange={(event) => table.getColumn("email")?.setFilterValue(event.target.value)}
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
            <Button variant="default">Create</Button>
          </PermissionCheck>
        </>
      )}
    </>
  );
}
