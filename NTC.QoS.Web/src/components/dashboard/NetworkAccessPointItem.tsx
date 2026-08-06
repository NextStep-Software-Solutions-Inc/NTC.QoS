import { Wifi } from "lucide-react";

import { type ServiceProvider } from "../features/service-provider/types/serviceProvider";
import ProviderSwitch from "./ProviderSwitch";

interface NetworkAccessPointItemProps {
  provider: ServiceProvider;
}
const NetworkAccessPointItem = ({ provider }: NetworkAccessPointItemProps) => {
  return (
    <div>
      <ProviderSwitch
        style={{ icon: { backgroundColor: provider.provider.color } }}
        type="NAP"
        icon={<Wifi size="15px" className="text-white" />}
        name={provider.provider.name}
        count={0}
        id={provider.id}
      />
    </div>
  );
};

export default NetworkAccessPointItem;
