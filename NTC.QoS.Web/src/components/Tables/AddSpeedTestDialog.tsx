"use client";

import React, { useState } from "react";
import { Plus } from "lucide-react";

import { cn } from "@/lib/utils";
import AddSpeedtestForm from "../features/speedtest/form/AddSpeedtestForm";
import { ResponsiveDialog } from "../ResponsiveDialog";
import { Button } from "../ui/button";

const AddSpeedTestDialog = () => {
  const [isAddSpeedTestDialogOpen, setIsAddSpeedTestDialogOpen] = useState<boolean>(false);
  const [tabsContent, setTabsContent] = useState<string>("basic-information");

  return (
    <ResponsiveDialog
      open={isAddSpeedTestDialogOpen}
      onOpenChange={setIsAddSpeedTestDialogOpen}
      className="sm:max-w-[800px] w-full"
      title="Add Speed Test"
      description="Add a new speed test to the system. Fill in the required information and save."
      trigger={
        <Button className="flex items-center">
          <Plus className="mr-1" />
          Add Speed Test
        </Button>
      }>
      <div className="flex items-center justify-between p-4">
        <Button
          variant={tabsContent === "basic-information" ? "default" : "ghost"}
          className={cn("w-full")}
          onClick={() => setTabsContent?.("basic-information")}>
          Basic Information
        </Button>
        <Button
          variant={tabsContent === "antenna-details" ? "default" : "ghost"}
          className={cn("w-full")}
          onClick={() => setTabsContent?.("antenna-details")}>
          Antenna and Sector Details
        </Button>
      </div>

      {/* content  tabs*/}
      <div className="w-full h-fit">{tabsContent === "basic-information" && <AddSpeedtestForm />}</div>
    </ResponsiveDialog>
  );
};

export default AddSpeedTestDialog;
