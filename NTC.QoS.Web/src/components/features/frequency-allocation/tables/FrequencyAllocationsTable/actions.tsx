"use client";

import { useState } from "react";
import { Power, SquarePen } from "lucide-react";
import { toast } from "sonner";

import { editFrequencyAllocationRequest } from "../../handlers/editFrequencyAllocationRequest";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import EditFrequencyAllocationDialog from "../../dialogs/EditFrequencyAllocationDialog";
import { type FrequencyAllocation } from "../../types/FrequencyAllocation";

const useEditFrequencyAllocation = createMutationHookFromTemplate(editFrequencyAllocationRequest);

interface ActionsProps {
  data: FrequencyAllocation;
  readonly?: boolean;
}

export default function Actions({ data, readonly }: ActionsProps) {
  const [isEditOpen, setIsEditOpen] = useState(false);
  const editMutation = useEditFrequencyAllocation();

  if (readonly) return null;

  const handleToggleStatus = async () => {
    try {
      await editMutation.mutateAsync({
        pathParams: { id: data.id },
        payload: { isActive: !data.isActive },
      });
      toast.success(`Marked as ${!data.isActive ? "Active" : "Inactive"}`);
    } catch {
      toast.error("Failed to update status.");
    }
  };

  return (
    <TooltipProvider delayDuration={200}>
      <div className="flex items-center gap-1">
        {/* Active / Inactive toggle */}
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              disabled={editMutation.isPending}
              onClick={handleToggleStatus}
              className={
                data.isActive
                  ? "text-green-600 hover:text-white hover:bg-green-600"
                  : "text-gray-400 hover:text-white hover:bg-gray-500"
              }>
              <Power className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top">
            {data.isActive ? "Deactivate" : "Activate"}
          </TooltipContent>
        </Tooltip>

        {/* Edit */}
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="sm" onClick={() => setIsEditOpen(true)}>
              <SquarePen className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top">Edit</TooltipContent>
        </Tooltip>
      </div>

      {isEditOpen && (
        <EditFrequencyAllocationDialog open={isEditOpen} onOpenChange={setIsEditOpen} allocation={data} />
      )}
    </TooltipProvider>
  );
}
