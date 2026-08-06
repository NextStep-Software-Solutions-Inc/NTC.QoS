import React, { useState } from "react";
import { Plus } from "lucide-react";

import AntennaDetailsForm from "../features/antenna-pattern/form/AnthenaDetailsForm";
import { type Barangay } from "../features/barangay/types/Barangay";
import AddCellSiteBasicInfoForm from "../features/cell/forms/AddCellSiteBasicInfoForm";
import { type Cell } from "../features/cell/types/Cell";
import { ResponsiveDialog } from "../ResponsiveDialog";
import { Button } from "../ui/button";
import { Tabs, TabsList, TabsTrigger } from "../ui/tabs";

interface AddCellSiteDialogProps {
  barangays?: Barangay;
}

const AddCellSiteDialog = ({ barangays }: AddCellSiteDialogProps) => {
  const [isAddCellSiteDialogOpen, setIsAddCellSiteDialogOpen] = useState<boolean>(false);
  const [tabsContent, setTabsContent] = useState<string>("basic-information");
  const [createdCell, setCreatedCell] = useState<Cell | null>(null);

  const handleOpenChange = (open: boolean) => {
    setIsAddCellSiteDialogOpen(open);

    if (!open) {
      setTabsContent("basic-information");
      setCreatedCell(null);
    }
  };

  return (
    <ResponsiveDialog
      open={isAddCellSiteDialogOpen}
      onOpenChange={handleOpenChange}
      className="sm:max-w-[800px] w-full"
      title="Add Cell Site"
      description="Add a new cell site to the system. Fill in the required information and save."
      trigger={
        <Button className="flex items-center">
          <Plus className="mr-1" />
          Add Cell Site
        </Button>
      }>
      {/* Scrollable content container */}
      <div className="w-full overflow-y-auto flex-1 px-1">
        <Tabs value={tabsContent} onValueChange={setTabsContent} className="mb-4">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="basic-information">Cell Site</TabsTrigger>
            <TabsTrigger
              value="cell-sector"
              disabled={!createdCell}
              aria-disabled={!createdCell}
              title={!createdCell ? "Complete Cell Site step first" : undefined}>
              Cell Sector & Cellular
            </TabsTrigger>
          </TabsList>
        </Tabs>

        {tabsContent === "basic-information" && (
          <AddCellSiteBasicInfoForm
            tabsContent={tabsContent}
            setTabsContent={setTabsContent}
            barangays={barangays}
            enableMultiStepFlow
            onCreated={(cell) => {
              setCreatedCell(cell);
            }}
            onClose={() => {
              setIsAddCellSiteDialogOpen(false);
            }}
          />
        )}
        {tabsContent === "cell-sector" && (
          <AntennaDetailsForm
            tabsContent={tabsContent}
            setTabsContent={setTabsContent}
            isCloseDialog={setIsAddCellSiteDialogOpen}
            cellId={createdCell?.id}
            serviceProviderId={createdCell?.serviceProviderId}
            defaultAddress={createdCell?.address}
          />
        )}
      </div>
    </ResponsiveDialog>
  );
};

export default AddCellSiteDialog;
