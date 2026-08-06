import React from "react";
import { Plus } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import CreateProvinceForm from "../forms/CreateProvinceForm";

interface CreateProvinceDialogProps {
  onCreate?: () => void;
}
export default function CreateProvinceDialog({ onCreate }: CreateProvinceDialogProps) {
  return (
    <ResponsiveDialog
      trigger={
        <Button variant="default">
          <Plus />
          Create Province
        </Button>
      }
      title="Create Province"
      description="Create a new province by filling out the form below.">
      <CreateProvinceForm payload={{ id: "", name: "" }} onClose={() => void {}} onSuccess={onCreate} />
    </ResponsiveDialog>
  );
}
