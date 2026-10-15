"use client";

import React from "react";
import { SquarePen } from "lucide-react";

import { type Technology } from "@/components/features/technology/types/Technology";
import { Button } from "@/components/ui/button";
import EditTechnologyDialog from "../../dialogs/EditTechnologyDialog";

interface ActionsProps {
  data: Technology;
  readonly?: boolean;
}
export default function Actions({ data }: ActionsProps) {
  const [editDialogOpen, setEditDialogOpen] = React.useState(false);

  return (
    <>
      <EditTechnologyDialog open={editDialogOpen} onOpenChange={setEditDialogOpen} technology={data} />
      <Button variant="ghost" size="sm" onClick={() => setEditDialogOpen(true)}>
        <SquarePen />
      </Button>
    </>
  );
}
