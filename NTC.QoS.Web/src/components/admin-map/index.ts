/**
 * Admin Map Module
 *
 * This module provides a redesigned admin map component with:
 * - Google Maps-inspired UI with glassmorphic styling
 * - Modular layer management with opacity controls
 * - Functional filter panel with search
 * - Map style selector (defaulting to Topo v4)
 * - Navigation controls (zoom, compass, locate, 3D)
 *
 * Architecture follows SOLID and DRY principles:
 * - Single Responsibility: Each component handles one concern
 * - Open/Closed: Add new layers via configuration, not code modification
 * - Liskov Substitution: UI components are interchangeable
 * - Interface Segregation: Specific interfaces for each feature
 * - Dependency Inversion: Components depend on abstractions (context)
 *
 * Usage:
 * ```tsx
 * import AdminViewMap from "@/components/admin-map";
 *
 * export default function AdminPage() {
 *   return <AdminViewMap />;
 * }
 * ```
 *
 * To extend with custom layers, see LayerControls.tsx
 */

export { default } from "./AdminViewMap";

// Re-export components for external use
export { FiberInformation } from "./components/FiberInformation";
export { CellSiteDetails } from "./components/CellSiteDetails";
export { AdminBroadbandPanel } from "./components/AdminBroadbandPanel";
export { default as AdminViewMap } from "./AdminViewMap";

// Context exports
export { AdminMapProvider, useAdminMapContext, useLayerState, useFilterState, useAdminMapUI } from "./context";

// UI component exports
export { GlassPanel, MapControlButton, CollapsibleSection, LayerToggle } from "./ui";

// Control component exports
export { MapStyleSelector, LayerControls, NavigationControls, SearchBar, FilterPanel } from "./controls";

// Type exports
export type * from "./types";
