"use client";

import React from "react";
import { SquarePen } from "lucide-react";

import { Button } from "@/components/ui/button";
import EditBandDialog from "../../dialogs/EditBandDialog";
import { type Band } from "../../types/Band";

interface ActionsProps {
  data: Band;
  readonly?: boolean;
}

export function Actions({ data }: ActionsProps) {
  const [editDialogOpen, setEditDialogOpen] = React.useState(false);

  return (
    <>
      <EditBandDialog open={editDialogOpen} onOpenChange={setEditDialogOpen} band={data} />
      <Button variant="ghost" size="sm" onClick={() => setEditDialogOpen(true)}>
        <SquarePen />
      </Button>
    </>
  );
}
