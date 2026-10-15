import { type ServiceProvider } from "../../service-provider/types/serviceProvider";

export type NAPMinified = { id: string; ISP: string; ISPColor: string; dist: number; lat: number; lng: number };

// Viewport API Response Type (map rendering only - simplified structure)
export type NAPViewport = {
  isp: string; // lowercase in viewport API
  ispColor: string; // camelCase in viewport API
  id?: string;
  lat: number;
  lng: number;
};

// Admin viewport API item shape (/v2/NAPs/viewport)
// Backend may return either camelCase or PascalCase properties depending on serializer settings.
export type NAPViewportAdmin = {
  id?: string;
  Id?: string;
  lat?: number;
  Lat?: number;
  lng?: number;
  Lng?: number;
  isp?: string;
  ISP?: string;
  ispColor?: string;
  ISPColor?: string;
  address?: string;
  Address?: string;
  serviceProviderId?: string;
  ServiceProviderId?: string;
};

// Regular NAP Type (from standard NAP endpoints) - original format
export type NAP = { ISP: string; ISPColor: string; lat: number; lng: number; dist: number; id: string };

// NAP from paginated API endpoints (has latitude/longitude instead of lat/lng)
export type NAPFromAPI = {
  id: string;
  latitude: number;
  longitude: number;
  address?: string;
  serviceProvider?: ServiceProvider;
};

/**
 * Full NAP Details for frontend display.
 * serviceProvider is optional because:
 * - It may not be loaded in all API responses (e.g., lightweight list endpoints)
 * - ISP and ISPColor can be used as fallbacks when serviceProvider is not available
 */
export type NAPDetails = {
  serviceProvider?: ServiceProvider;
  serviceProviderId?: string;
  id: string;
  ISP: string;
  ISPColor: string;
  lat: number;
  lng: number;
  dist: number;
  address?: string;
};

// API response format for NAP details
export type NAPAPIDetails = {
  id: string;
  latitude: number;
  longitude: number;
  address?: string;
  serviceProvider?: ServiceProvider;
};
