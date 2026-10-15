"use client";

import { useState } from "react";
import { SquarePen } from "lucide-react";

import { Button } from "@/components/ui/button";
import EditLinkDialog from "../../dialogs/EditLinkDialog";
import { type Link } from "../../types/Link";

interface ActionsProps {
  data: Link;
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

      {isEditDialogOpen && <EditLinkDialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen} link={data} />}
    </>
  );
}
