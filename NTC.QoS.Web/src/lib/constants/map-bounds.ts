/**
 * Geographic bounds for the Philippines
 * Used to constrain viewport-based data queries to avoid fetching data from empty ocean areas
 *
 * These bounds provide a reasonable buffer around the Philippines archipelago
 */
export const PHILIPPINES_BOUNDS = {
  minLat: 4.5, // Southernmost point (near Tawi-Tawi)
  maxLat: 21.0, // Northernmost point (near Batanes)
  minLng: 116.0, // Westernmost point (near Palawan)
  maxLng: 127.0, // Easternmost point (near Eastern Mindanao)
} as const;

/**
 * Center point of the Philippines for map initialization
 */
export const PHILIPPINES_CENTER = { lat: 12.796927, lng: 122.405211 } as const;
