import { useSearchParams } from "next/navigation";
import { RadioTower } from "lucide-react";

import { type ServiceProvider } from "../features/service-provider/types/serviceProvider";
import ProviderGeneration from "./ProviderGeneration";
import ProviderSwitch from "./ProviderSwitch";

interface CellSiteItemProps {
  provider: ServiceProvider;
}
const CellSiteItem = ({ provider }: CellSiteItemProps) => {
  const TYPE = "cell";
  const searchParams = useSearchParams();
  const providerId = searchParams.get(`${TYPE}-${provider.provider.name.toLowerCase()}`);

  const expandedProvider = providerId === "true" ? true : false;

  return (
    <div>
      <ProviderSwitch
        style={{ icon: { backgroundColor: provider.provider.color } }}
        icon={<RadioTower size="15px" className="text-white" />}
        name={provider.provider.name}
        count={0}
        id={provider.id}
        type={TYPE}
      />
      {expandedProvider && <ProviderGeneration provider={provider.provider} />}
    </div>
  );
};

export default CellSiteItem;
