"use client";

import React, { useState } from "react";
import { Plus } from "lucide-react";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import AddSpeedtestForm from "../form/AddSpeedtestForm";

const AddSpeedtestDialog = () => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <Button onClick={() => setIsOpen(true)}>
        <Plus className="mr-2 h-4 w-4" />
        Add Speed Test
      </Button>

      <ResponsiveDialog
        className="sm:max-w-[500px]"
        open={isOpen}
        onOpenChange={setIsOpen}
        title="Add Speed Test"
        description="Add a new speed test result for this barangay.">
        <AddSpeedtestForm onCloseDialog={setIsOpen} />
      </ResponsiveDialog>
    </>
  );
};

export default AddSpeedtestDialog;
