"use client";

import React from "react";
import { useRouter } from "next/navigation";
import { Eye, MapPin, MoreHorizontal, SquarePen, Trash2 } from "lucide-react";

import { type Cell } from "@/components/features/cell/types/Cell";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface ActionsProps {
  data: Cell;
  readonly?: boolean;
  regionId?: string;
}

export default function Actions({ data, readonly, regionId = "default" }: ActionsProps) {
  const router = useRouter();

  const handleViewDetails = () => {
    // Navigate to detail view
    router.push(`/${regionId}/broadband/cellsites/${data.id}`);
  };

  const handleViewOnMap = () => {
    // Navigate to map view with this cell highlighted
    if (data.latitude && data.longitude) {
      router.push(`/map?lat=${data.latitude}&lng=${data.longitude}&cellId=${data.id}`);
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" className="h-8 w-8 p-0">
          <span className="sr-only">Open menu</span>
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>Actions</DropdownMenuLabel>
        <DropdownMenuItem onClick={handleViewDetails}>
          <Eye className="mr-2 h-4 w-4" />
          View Details
        </DropdownMenuItem>
        <DropdownMenuItem onClick={handleViewOnMap}>
          <MapPin className="mr-2 h-4 w-4" />
          View on Map
        </DropdownMenuItem>
        {!readonly && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem>
              <SquarePen className="mr-2 h-4 w-4" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem className="text-destructive">
              <Trash2 className="mr-2 h-4 w-4" />
              Delete
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
