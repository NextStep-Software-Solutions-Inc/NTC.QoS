import { useParams } from "next/navigation";
import { Building2, ChevronLeft, Globe, MapPin } from "lucide-react";

import { Button } from "@/components/ui/button";
import getHumanReadableString from "@/lib/getHumanReadableString";
import { DropdownEditCell } from "../features/cell/dropdown-menu/DropDownEditCell";
import { type Cell } from "../features/cell/types/Cell";
import TabsUnderProviderName from "./TabsUnderProviderName";

interface BrgyCellSiteDetailsProps {
  cells?: Cell[];
  setIsView?: (isView: boolean) => void;
}
const BrgyCellSiteDetails = ({ cells, setIsView }: BrgyCellSiteDetailsProps) => {
  const { region, province, municipal, brgy } = useParams<{
    region: string;
    province: string;
    municipal: string;
    brgy: string;
  }>();

  return (
    <div>
      {cells?.map((cell) => (
        <div key={cell.id} className="mt-4 w-full h-full border justify-center rounded-lg p-[1.5rem]">
          <div className="w-fit flex items-center gap-2 my-3">
            <Button onClick={() => setIsView?.(false)} variant={"outline"} className="w-fit p-3 rounded-md">
              <ChevronLeft />
            </Button>
            <h1 className="text-[2rem] font-bold">{getHumanReadableString(brgy)}</h1>
          </div>
          <div className="w-full flex items-center justify-between">
            <div className="w-fit flex items-center gap-x-2 p-3">
              <p
                style={{ backgroundColor: cell.serviceProvider?.provider.color }}
                className="text-[13px] text-white font-semibold py-2 px-5 rounded-full">
                {cell.serviceProvider?.provider.name}
              </p>
              <div className=" bg-gray-100 font-semibold py-2 px-5 rounded-full flex items-center gap-x-2">
                <Globe size={15} className="mr" />
                <p className="text-[13px]">{getHumanReadableString(region)}</p>
              </div>
            </div>
            <DropdownEditCell />
          </div>

          <div className="w-full mt-1 border p-3 rounded-md">
            <section className="w-full flex items-center p-3">
              <div className="w-full flex items-center gap-x-2">
                <div className="p-3 bg-gray-200 rounded-full">
                  <Building2 />
                </div>
                <section>
                  <p>Province - City</p>
                  <p>
                    {getHumanReadableString(province)} - {getHumanReadableString(municipal)}
                  </p>
                </section>
              </div>
              <div className="w-full flex items-center gap-x-2">
                <div className="p-3 bg-gray-200 rounded-full">
                  <MapPin />
                </div>
                <section>
                  <p>Zone</p>
                  <p>Zone - 14</p>
                </section>
              </div>
            </section>
            <section className="w-full flex items-center p-3">
              <div className="w-full flex items-center gap-x-2">
                <div className="p-3 bg-gray-200 rounded-full">
                  <MapPin />
                </div>
                <section>
                  <p>Barangay</p>
                  <p>
                    {getHumanReadableString(brgy)} - {getHumanReadableString(municipal)}
                  </p>
                </section>
              </div>
              <div className="w-full flex items-center gap-x-2">
                <div className="p-3 bg-gray-200 rounded-full">
                  <MapPin />
                </div>
                <section>
                  <p>Coordinates</p>
                  <p>145.56344, 8.43221</p>
                </section>
              </div>
            </section>
            <Button variant="outline" className="w-full font-semibold py-3 px-5 rounded-md my-5">
              See Location
            </Button>
          </div>
          <TabsUnderProviderName />
        </div>
      ))}
    </div>
  );
};

export default BrgyCellSiteDetails;
