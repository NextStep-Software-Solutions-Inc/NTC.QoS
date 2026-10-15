"use client";

import * as React from "react";
import { EllipsisVertical, SquarePen, Trash2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import DeleteProviderDetailsDialog from "../dialogs/DeleteProviderDetailsDialog";

interface DropdownEditProviderProps {
  deleteProvider?: () => void;
  editProvider?: () => void;
  providerId?: string;
  isPending?: boolean;
}

export function DropdownEditProvider({
  deleteProvider,
  editProvider,
  providerId,
  isPending,
}: DropdownEditProviderProps) {
  const [isOpenDialog, setIsOpenDialog] = React.useState(false);
  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">
            <EllipsisVertical />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="w-fit" align="center">
          <DropdownMenuItem
            onClick={() => setIsOpenDialog(true)}
            className="flex items-center gap-x-2 text-red-500 hover:text-red-600 focus:text-red-600 hover:bg-red-50 focus:bg-red-50">
            <Trash2 className="h-4 w-4" />
            Delete
          </DropdownMenuItem>
          <DropdownMenuItem onClick={editProvider} className="flex items-center gap-x-2">
            <SquarePen /> Edit
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <DeleteProviderDetailsDialog
        isPending={isPending}
        open={isOpenDialog}
        onOpenChange={setIsOpenDialog}
        providerId={providerId ?? ""}
        onDelete={deleteProvider}
        ids={[]}
      />
    </>
  );
}
