"use client";

import { useState } from "react";
import { SquarePen } from "lucide-react";

import { Button } from "@/components/ui/button";
import EditAreaStudyDialog from "../../dialogs/EditAreaStudyDialog";
import { type AreaStudy } from "../../types/AreaStudy";

interface ActionsProps {
  data: AreaStudy;
  readonly?: boolean;
}

export default function Actions({ data, readonly }: ActionsProps) {
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);

  if (readonly) {
    return null;
  }

  return (
    <>
      <Button variant="ghost" size="sm" onClick={() => setIsEditDialogOpen(true)}>
        <SquarePen />
      </Button>

      <EditAreaStudyDialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen} areaStudy={data} />
    </>
  );
}
