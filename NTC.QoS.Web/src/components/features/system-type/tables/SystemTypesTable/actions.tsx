"use client";

import React from "react";
import { SquarePen } from "lucide-react";

import { type SystemType } from "@/components/features/system-type/types/SystemType";
import { Button } from "@/components/ui/button";
import EditSystemTypeDialog from "../../dialogs/EditSystemTypeDialog";

interface ActionsProps {
  data: SystemType;
  readonly?: boolean;
}
export default function Actions({ data }: ActionsProps) {
  const [editDialogOpen, setEditDialogOpen] = React.useState(false);

  return (
    <>
      <EditSystemTypeDialog open={editDialogOpen} onOpenChange={setEditDialogOpen} systemType={data} />
      <Button variant="ghost" size="sm" onClick={() => setEditDialogOpen(true)}>
        <SquarePen />
      </Button>
    </>
  );
}
