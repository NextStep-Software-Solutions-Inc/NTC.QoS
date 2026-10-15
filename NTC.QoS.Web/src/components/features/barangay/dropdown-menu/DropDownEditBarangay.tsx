"use client";

import * as React from "react";
import { EllipsisVertical, SquarePen } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface DropdownEditBarangayProps {
  barangayId: string;
  onEdit?: (id: string) => void;
}

export function DropdownEditBarangay({ barangayId, onEdit }: DropdownEditBarangayProps) {
  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">
            <EllipsisVertical />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="w-fit" align="center">
          <DropdownMenuItem onClick={() => onEdit?.(barangayId)} className="flex items-center gap-x-2">
            <SquarePen /> Edit
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );
}
