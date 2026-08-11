"use client";

import { useParams, usePathname } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useLocationMap } from "@/providers/LocationMapContext";
import BoundaryDetails from "../BoundaryDetails";
import ToolbarSearchMunicipal from "../features/municipality/toolbar/ToolbarSearchMunicipal";
import ProvidersSpeedTestResult from "../features/speedtest/card/ProvidersSpeedTestResult";
import AddSpeedtestDialog from "../features/speedtest/dialog/AddSpeedtestDialog";
import { getSpeedtestsRequest } from "../features/speedtest/handlers/getSpeedtestsRequest";

const useSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);

export default function BarangaySpeedTest() {
  const { resolveUuid } = useLocationMap();
  const pathname = usePathname();
  const { region, province, municipal, brgy } = useParams<{
    region: string;
    province: string;
    table: string;
    municipal: string;
    brgy: string;
  }>();

  const { data: speedtests } = useSpeedtests({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      provinceIds: [province],
      municipalityIds: [municipal],
      barangayIds: [brgy],
    },
  });

  const externalLink = `${pathname}/${brgy}`;
  const barangayName = resolveUuid(brgy);
  return (
    <section className="w-full">
      <BoundaryDetails
        label={`${barangayName} Speed Tests`}
        addButton={<AddSpeedtestDialog />}
        externalLink={externalLink}>
        <ToolbarSearchMunicipal />
        <div
          style={{
            overflowY: "auto",
            scrollbarWidth: "none", // Firefox
            msOverflowStyle: "none", // IE 10+
          }}
          className="hide-scrollbar">
          <ProvidersSpeedTestResult data={speedtests?.data} />
        </div>
      </BoundaryDetails>
    </section>
  );
}
