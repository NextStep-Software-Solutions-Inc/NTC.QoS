import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import AntennaDetailsForm from "../../antenna-pattern/form/AnthenaDetailsForm";
import { type Barangay } from "../../barangay/types/Barangay";
import AddCellSiteBasicInfoForm from "../forms/AddCellSiteBasicInfoForm";

interface TabsCellSiteProps {
  barangays?: Barangay;
  tabsContent?: string;
  setTabsContent?: (value: string) => void;
}

export function TabsCellSite({ barangays, tabsContent, setTabsContent }: TabsCellSiteProps) {
  return (
    <Tabs defaultValue={tabsContent} onValueChange={setTabsContent} className="w-full h-full">
      <TabsList className="w-full px-3 py-3 ">
        <TabsTrigger value="basic-information" className="w-full">
          Basic Information
        </TabsTrigger>
        <TabsTrigger value="antenna-details" className="w-full">
          Antenna and Sector Details
        </TabsTrigger>
      </TabsList>

      <TabsContent value="basic-information">
        <AddCellSiteBasicInfoForm barangays={barangays} />
      </TabsContent>
      <TabsContent value="antenna-details">
        <AntennaDetailsForm />
      </TabsContent>
    </Tabs>
  );
}
