/**
 * Signal quality utilities for speedtest forms
 * Automatically determines signal quality based on RSSI (dBm) values
 */

export type SignalQualityLevel = "Excellent" | "Good" | "Average" | "Weak" | "VeryWeak" | "Unknown";

export interface SignalQualityMapping {
  level: SignalQualityLevel;
  range: string;
  minRssi: number;
  maxRssi: number;
}

/**
 * Signal quality mappings based on RSSI (dBm) values
 * Note: RSSI values are negative, so -40 dBm is better than -100 dBm
 */
export const SIGNAL_QUALITY_MAPPINGS: SignalQualityMapping[] = [
  {
    level: "Excellent",
    range: "-40 to 0 dBm",
    minRssi: -40,
    maxRssi: 0,
  },
  {
    level: "Good",
    range: "-70 to -40 dBm",
    minRssi: -70,
    maxRssi: -40,
  },
  {
    level: "Average",
    range: "-80 to -70 dBm",
    minRssi: -80,
    maxRssi: -70,
  },
  {
    level: "Weak",
    range: "-90 to -80 dBm",
    minRssi: -90,
    maxRssi: -80,
  },
  {
    level: "VeryWeak",
    range: "-100 to -90 dBm",
    minRssi: -100,
    maxRssi: -90,
  },
];

/**
 * Determines signal quality level based on RSSI value
 * @param rssi - RSSI value in dBm (can be positive or negative)
 * @returns Signal quality level
 */
export function getSignalQualityFromRSSI(rssi: number | undefined | null): SignalQualityLevel {
  if (rssi === undefined || rssi === null || isNaN(rssi)) {
    return "Unknown";
  }

  // Normalize RSSI to negative value if positive
  const normalizedRssi = rssi > 0 ? -rssi : rssi;

  // Below -100 dBm is considered no coverage
  if (normalizedRssi < -100) {
    return "Unknown"; // No broadband coverage
  }

  // Find the matching quality level
  for (const mapping of SIGNAL_QUALITY_MAPPINGS) {
    if (normalizedRssi >= mapping.minRssi && normalizedRssi <= mapping.maxRssi) {
      return mapping.level;
    }
  }

  // If RSSI is above -40 dBm (better than Excellent range)
  if (normalizedRssi > -40) {
    return "Excellent";
  }

  return "Unknown";
}

/**
 * Gets the RSSI range string for a given signal quality level
 * @param level - Signal quality level
 * @returns RSSI range string (e.g., "-70 to -40 dBm")
 */
export function getRSSIRangeForQuality(level: SignalQualityLevel): string {
  const mapping = SIGNAL_QUALITY_MAPPINGS.find((m) => m.level === level);
  return mapping?.range ?? "Unknown range";
}

/**
 * Gets a display-friendly label combining quality level and RSSI range
 * @param level - Signal quality level
 * @returns Combined label (e.g., "Good (-70 to -40 dBm)")
 */
export function getSignalQualityLabel(level: SignalQualityLevel): string {
  if (level === "Unknown") {
    return "Unknown";
  }
  const range = getRSSIRangeForQuality(level);
  return `${level} (${range})`;
}
