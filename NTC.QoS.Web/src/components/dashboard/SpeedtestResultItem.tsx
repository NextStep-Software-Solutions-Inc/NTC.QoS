import { ArrowUpDown } from "lucide-react";

import { type Service } from "../features/service/types/Service";
import ProviderSwitch from "./ProviderSwitch";

interface SpeedtestResultItemProps {
  service: Service;
}
export default function SpeedtestResultItem({ service }: SpeedtestResultItemProps) {
  return (
    <div>
      <ProviderSwitch
        style={{ icon: { backgroundColor: "green" } }}
        type="speedtests"
        icon={<ArrowUpDown size="15px" className="text-white" />}
        name={service.name ?? "Unknown Service"}
        count={0}
        id={service.id}
      />
    </div>
  );
}
