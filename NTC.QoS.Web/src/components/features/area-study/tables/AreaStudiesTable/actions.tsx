"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { SlidersHorizontal, SquarePen } from "lucide-react";

import { Button } from "@/components/ui/button";
import EditAreaStudyDialog from "../../dialogs/EditAreaStudyDialog";
import { type AreaStudy } from "../../types/AreaStudy";

interface ActionsProps {
  data: AreaStudy;
  readonly?: boolean;
}

export default function Actions({ data, readonly }: ActionsProps) {
  const router = useRouter();
  const params = useParams();
  const region = String(params.region ?? "");
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);

  if (readonly) {
    return null;
  }

  return (
    <>
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => router.push(`/${region}/settings/area-studies/${data.id}/signal-level`)}
          title="Signal configuration">
          <SlidersHorizontal className="h-4 w-4" />
        </Button>

        <Button variant="ghost" size="sm" onClick={() => setIsEditDialogOpen(true)} title="Edit area study">
          <SquarePen className="h-4 w-4" />
        </Button>
      </div>

      <EditAreaStudyDialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen} areaStudy={data} />
    </>
  );
}
