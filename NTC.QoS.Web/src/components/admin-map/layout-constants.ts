/**
 * Admin Map Layout Constants
 *
 * Centralizes key layout measurements used across admin map components
 * to ensure consistency and simplify future adjustments.
 */

/** Height of the DashboardNavbar in pixels (Tailwind h-16 = 4rem = 64px). */
export const NAVBAR_HEIGHT_PX = 64;

/** Width of the expanded FilterPanel in pixels (corresponds to w-[400px] in FilterPanel.tsx). */
export const FILTER_PANEL_WIDTH_PX = 400;

/** Width of the expanded Broadband Analytics drawer in pixels. */
export const ANALYTICS_PANEL_WIDTH_PX = 400;

/** Drawer handle width in pixels for both expanded and collapsed states. */
export const FILTER_PANEL_HANDLE_WIDTH_PX = 40;

/** Drawer handle height in pixels for both expanded and collapsed states. */
export const FILTER_PANEL_HANDLE_HEIGHT_PX = 96;

/** Drawer handle horizontal offset when attached to the expanded panel edge. */
export const FILTER_PANEL_HANDLE_OFFSET_PX = FILTER_PANEL_HANDLE_WIDTH_PX;

/**
 * Left offset for bottom-left map controls when the FilterPanel is open.
 * Equals the panel width plus a standard 1rem (16px) gap.
 */
export const FILTER_PANEL_OPEN_LEFT_OFFSET_PX = FILTER_PANEL_WIDTH_PX + 16;

/** Right offset for map controls when right drawer is open (drawer width + standard 1rem gap). */
export const RIGHT_DRAWER_OPEN_OFFSET_PX = ANALYTICS_PANEL_WIDTH_PX + 16;

/** Base z-index for the floating right-side map control island. */
export const MAP_CONTROL_STACK_Z_INDEX = 120;

/** Z-index for compact control wrappers that own left-side popovers. */
export const MAP_CONTROL_POPOVER_WRAPPER_Z_INDEX = 130;

/** Z-index for compact control popovers that must render above the control island. */
export const MAP_CONTROL_POPOVER_Z_INDEX = 140;
