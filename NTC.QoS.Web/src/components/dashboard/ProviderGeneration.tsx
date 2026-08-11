import { useCallback, useMemo } from "react";
import { useSearchParams } from "next/navigation";

import { getBandsRequest } from "@/components/features/band/handlers/getBandsRequest";
import { type Band } from "@/components/features/band/types/Band";
import { getGenerationsRequest } from "@/components/features/generation/handlers/getGenerationsRequest";
import type { Generation } from "@/components/features/generation/types/Generation";
import { getTechnologiesRequest } from "@/components/features/technology/handlers/getTechnologiesRequest";
import type { Technology } from "@/components/features/technology/types/Technology";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import Each from "../Each";
import { type Provider } from "../features/provider/types/Provider";

const useGenerations = createQueryHookFromTemplate(getGenerationsRequest);
const useTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useBands = createQueryHookFromTemplate(getBandsRequest);

interface ProviderGenerationProps {
  provider: Provider;
}

export default function ProviderGeneration({ provider }: ProviderGenerationProps) {
  const { data: generations } = useGenerations({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <div className="space-y-4">
      <Each of={generations?.data ?? []} keyExtractor={(item) => item.id}>
        {(generation) => <GenerationTechnologyList generation={generation} provider={provider} />}
      </Each>
    </div>
  );
}

interface GenerationTechnologyListProps {
  generation: Generation;
  provider: Provider;
}

function GenerationTechnologyList({ generation, provider }: GenerationTechnologyListProps) {
  const { data: technologies } = useTechnologies({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <Each of={technologies?.data ?? []} keyExtractor={(item) => item.id}>
      {(technology) => <GenerationTechnology generation={generation} technology={technology} provider={provider} />}
    </Each>
  );
}

interface GenerationTechnologyProps {
  generation: Generation;
  technology: Technology;
  provider: Provider;
}

function GenerationTechnology({ generation, technology, provider }: GenerationTechnologyProps) {
  return (
    <div className={cn("grid grid-cols-3 items-center gap-4")} style={{ color: provider.color }}>
      <div className="text-right text-sm font-bold">
        <div>
          <div>{generation.name.toUpperCase()} </div>
          <div className="text-muted-foreground">{technology.standard.toUpperCase()}</div>
        </div>
      </div>
      <div className="col-span-2 space-y-2">
        <TechnologyBandList technology={technology} provider={provider} />
      </div>
    </div>
  );
}

interface TechnologyBandListProps {
  technology: Technology;
  provider: Provider;
}

function TechnologyBandList({ provider }: TechnologyBandListProps) {
  const { data: bands } = useBands({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  return (
    <Each of={bands?.data ?? []} keyExtractor={(item) => item.id}>
      {(band) => <TechnologyBand band={band} provider={provider} />}
    </Each>
  );
}

interface TechnologyBandProps {
  band: Band;
  provider: Provider;
}

function TechnologyBand({ band, provider }: TechnologyBandProps) {
  const searchParams = useSearchParams();

  const bandIdSearchParams = searchParams.get(band.id) ?? "false";

  const isToggled = useMemo(() => (bandIdSearchParams === "true" ? true : false), [bandIdSearchParams]);

  const handleToggle = useCallback(() => {
    const params = new URLSearchParams(window.location.search);
    if (isToggled) {
      params.set(band.id, "false");
    } else {
      params.set(band.id, "true");
    }
    window.history.replaceState({}, "", `${window.location.pathname}?${params}`);
  }, [band.id, isToggled]);

  const providerColor = provider.color;

  return (
    <div
      className={cn(
        "mb-1 mr-2 flex cursor-pointer items-center rounded-full px-2 py-1 text-[#737373]",
        "max-w-max border border-gray-200",
        "transition-opacity hover:opacity-80",
        { "border-[#3B82F6]": isToggled },
      )}
      onClick={handleToggle}>
      <span className="flex flex-row items-center gap-2 text-sm">
        <span className={cn("aspect-square h-4 rounded-full")} style={{ backgroundColor: providerColor }}></span>
        <span className="max-w-fit truncate text-nowrap">{band.name}</span>
      </span>
    </div>
  );
}
