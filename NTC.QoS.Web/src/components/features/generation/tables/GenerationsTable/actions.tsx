"use client";

import React from "react";
import { SquarePen } from "lucide-react";

import { type Generation } from "@/components/features/generation/types/Generation";
import { Button } from "@/components/ui/button";
import EditGenerationDialog from "../../dialogs/EditGenerationDialog";

interface ActionsProps {
  data: Generation;
  readonly?: boolean;
}
export default function Actions({ data }: ActionsProps) {
  const [editDialogOpen, setEditDialogOpen] = React.useState(false);

  return (
    <>
      <EditGenerationDialog open={editDialogOpen} onOpenChange={setEditDialogOpen} generation={data} />
      <Button variant="ghost" size="sm" onClick={() => setEditDialogOpen(true)}>
        <SquarePen />
      </Button>
    </>
  );
}
