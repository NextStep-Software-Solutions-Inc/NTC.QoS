import { Radio } from "lucide-react";

import { type Service } from "../features/service/types/Service";
import ProviderSwitch from "./ProviderSwitch";

interface InternetCoverageItemProps {
  service: Service;
}
const InternetCoverageItem = ({ service }: InternetCoverageItemProps) => {
  return (
    <div>
      <ProviderSwitch
        style={{ icon: { backgroundColor: "green" } }}
        type=""
        icon={<Radio size="15px" className="text-white" />}
        name={service.name ?? "Unknown Service"}
        count={0}
        id={service.id}
      />
    </div>
  );
};

export default InternetCoverageItem;
