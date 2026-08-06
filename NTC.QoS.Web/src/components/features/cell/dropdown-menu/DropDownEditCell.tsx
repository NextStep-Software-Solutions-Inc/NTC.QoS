"use client";

import * as React from "react";
import { EllipsisVertical, SquarePen, Trash } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function DropdownEditCell() {
  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">
            <EllipsisVertical />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="w-fit" align="center" color="white">
          <DropdownMenuItem className="flex items-center gap-x-2">
            <SquarePen /> Edit
          </DropdownMenuItem>
          <DropdownMenuItem className="flex items-center gap-x-2 text-red-500  hover:text-red-600">
            <Trash /> Delete
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );
}
