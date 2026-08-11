import { type ServiceProvider } from "@/components/features/service-provider/types/serviceProvider";

interface NAPItem {
  ISP: string;
  ISPColor: string;
}

export type GetUniqueServiceProviderArgs = { serviceProvider: ServiceProvider }[];
/**
 * Extracts unique providers from a list of ProviderItem.
 * Returns an array of unique providers by name.
 */

export function getUniqueServiceProviders(data: GetUniqueServiceProviderArgs) {
  if (!data || data.length === 0) return [];

  const serviceProviderMap = new Map<string, ServiceProvider>();

  data.forEach((item) => {
    const { name } = item.serviceProvider.provider;

    if (!serviceProviderMap.has(name)) {
      serviceProviderMap.set(name, item.serviceProvider);
    }
  });

  return Array.from(serviceProviderMap.values());
}

export function getNAPUniqueProviderNames(data?: NAPItem[]): string[] {
  if (!data || data.length === 0) return [];
  const providerSet = new Set<string>();

  data.forEach((item) => {
    providerSet.add(item.ISP);
  });

  return Array.from(providerSet);
}
