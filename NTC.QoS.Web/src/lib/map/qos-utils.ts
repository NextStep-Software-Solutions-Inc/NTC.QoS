// QoS feature properties interface
export interface QoSFeatureProperties {
  zipcode?: string;
  Zipcode?: string;
  barangay?: string;
  Barangay?: string;
  municipality?: string;
  Municipality?: string;
  province?: string;
  Province?: string;
  region?: string;
  Region?: string;
  providers?: string;
  Providers?: string;
  Provider?: string;
  provider?: string;
}

export interface NormalizedQoSProperties {
  zipcode: string;
  barangay: string;
  municipality: string;
  province: string;
  region: string;
  providers: string[];
}

/**
 * Helper to extract a property value from different case variations
 */
function getPropertyValue<K extends string>(props: Record<string, unknown>, key: K): string {
  const lowerKey = key.toLowerCase();
  const upperKey = key.charAt(0).toUpperCase() + key.slice(1);

  return (props[lowerKey] ?? props[upperKey] ?? props[key] ?? "") as string;
}

/**
 * Normalize QoS feature properties (handles case sensitivity)
 * Properties may come as lowercase, uppercase, or capitalized from the vector tiles
 */
export function normalizeQoSProperties(props: QoSFeatureProperties): NormalizedQoSProperties {
  // Get provider string from various possible property names
  const rawProviders = props.providers ?? props.Providers ?? props.Provider ?? props.provider ?? "";

  // Parse providers - could be comma or slash separated
  const providersList = rawProviders
    .split(/[,\/]/)
    .map((p) => p.trim())
    .filter((p) => p.length > 0);

  return {
    zipcode: getPropertyValue(props as Record<string, unknown>, "zipcode"),
    barangay: getPropertyValue(props as Record<string, unknown>, "barangay"),
    municipality: getPropertyValue(props as Record<string, unknown>, "municipality"),
    province: getPropertyValue(props as Record<string, unknown>, "province"),
    region: getPropertyValue(props as Record<string, unknown>, "region"),
    providers: providersList,
  };
}
