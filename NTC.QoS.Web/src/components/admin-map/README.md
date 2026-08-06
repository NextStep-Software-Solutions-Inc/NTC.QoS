# Admin Map Module

A redesigned admin map component for NTC QoS FE with Google Maps-inspired UI, glassmorphic styling, and refactored architecture following SOLID and DRY principles.

## Features

- **Google Maps-Inspired UI**: Clean, minimalist layout with floating controls
- **Glassmorphic Design**: Modern frosted glass effects throughout
- **Layer Management**: Collapsible panel with toggles and opacity controls
- **Map Style Selector**: Switch between Topo v4 (default), Streets, Satellite, Dark, Outdoor
- **Functional Filters**: Search, provider selection, signal strength, speed test metrics
- **Navigation Controls**: Zoom, compass, geolocation, 3D view, terrain, fullscreen
- **Mobile Responsive**: Adaptive layouts for all screen sizes

## Architecture

```
src/components/admin-map/
├── index.ts                    # Module exports
├── AdminViewMap.tsx            # Main component
├── README.md                   # This documentation
├── context/
│   ├── index.ts
│   └── AdminMapContext.tsx     # Centralized state management
├── controls/
│   ├── index.ts
│   ├── FilterPanel.tsx         # Redesigned sidebar filters
│   ├── LayerControls.tsx       # Layer toggles with opacity
│   ├── MapStyleSelector.tsx    # Map style dropdown
│   ├── NavigationControls.tsx  # Zoom, compass, locate, etc.
│   └── SearchBar.tsx           # Location search
├── types/
│   └── index.ts                # TypeScript definitions
└── ui/
    ├── index.ts
    ├── CollapsibleSection.tsx  # Expandable sections
    ├── GlassPanel.tsx          # Glassmorphic container
    ├── LayerToggle.tsx         # Individual layer toggle
    └── MapControlButton.tsx    # Map control button with tooltip
```

## SOLID Principles Implementation

### Single Responsibility

Each component handles one concern:

- `LayerControls` - Only layer visibility/opacity
- `MapStyleSelector` - Only style switching
- `NavigationControls` - Only map navigation
- `FilterPanel` - Only data filtering

### Open/Closed

Add new layers without modifying existing code:

```tsx
// In LayerControls.tsx, add to LAYER_CONFIGS:
{
  id: "new-layer",
  label: "New Layer",
  icon: NewIcon,
  param: "new-layer",
  color: "text-green-500",
  description: "Description here",
  hasOpacity: true,
  category: "infrastructure",
  subLayers: ["sub-layer-1", "sub-layer-2"],
}
```

### Liskov Substitution

UI components are interchangeable with compatible interfaces:

- All panels use `GlassPanel` for consistent styling
- All buttons use `MapControlButton` for consistent behavior

### Interface Segregation

Specific interfaces for each feature:

- `LayerConfig` for layer definitions
- `AdminMapFilters` for filter state
- `MapStyleConfig` for style options

### Dependency Inversion

Components depend on abstractions (context) not concretions:

```tsx
// Use hooks instead of direct state access
const { layerState, setLayerVisibility } = useLayerState();
const { filters, updateFilters } = useFilterState();
```

## Usage

### Basic Usage

```tsx
import AdminViewMap from "@/components/admin-map";

export default function AdminPage() {
  return <AdminViewMap />;
}
```

### With Custom Initial State

```tsx
import { AdminMapProvider, AdminViewMap } from "@/components/admin-map";

export default function AdminPage() {
  return (
    <AdminMapProvider
      initialFilters={{ providers: ["globe"], networkTypes: ["4G", "5G"] }}
      initialLayerState={{ visibility: { cellular: true } }}
    >
      <AdminViewMap />
    </AdminMapProvider>
  );
}
```

### Using Individual Components

```tsx
import { 
  GlassPanel, 
  MapControlButton, 
  LayerToggle 
} from "@/components/admin-map";

// Use in your custom components
<GlassPanel variant="default" padding="md">
  <MapControlButton tooltip="Custom Action">
    <Icon />
  </MapControlButton>
</GlassPanel>
```

## Adding New Layers

1. Define the layer configuration in `LayerControls.tsx`:

```tsx
const LAYER_CONFIGS: LayerConfig[] = [
  // ... existing layers
  {
    id: "my-new-layer",
    label: "My New Layer",
    icon: MyIcon,
    param: "my-new-layer", // URL parameter
    color: "text-blue-500",
    description: "Optional description",
    hasOpacity: true,
    defaultOpacity: 80,
    category: "infrastructure",
    subLayers: ["my-layer-fill", "my-layer-stroke"],
  },
];
```

2. Add the category if needed:

```tsx
const LAYER_CATEGORIES = [
  // ... existing categories
  { category: "infrastructure", label: "Infrastructure", icon: Building2 },
];
```

3. Create a hook for your layer data (if needed):

```tsx
// In src/hooks/useMapMyLayer.tsx
export const useMapMyLayer = (visibleLayers: Record<string, boolean>) => {
  // Implementation similar to useMapCells, useMapNAPs, etc.
};
```

4. Add the hook to `AdminViewMap.tsx`:

```tsx
import { useMapMyLayer } from "@/hooks/useMapMyLayer";

// In AdminMapContent:
useMapMyLayer(myLayerVisibilities);
```

## Styling

### Glass Panel Variants

- `default` - White/light background
- `dark` - Dark background
- `primary` - Primary color tint
- `transparent` - More transparent

### Map Control Button Sizes

- `sm` - 32x32px
- `md` - 40x40px (default)
- `lg` - 48x48px

### Customizing Colors

Update Tailwind classes in the component props or create new variants in the component files.

## Performance Optimizations

1. **React.memo** - All control components are memoized
2. **useMemo/useCallback** - Expensive computations are memoized
3. **Context splitting** - Separate hooks for layer/filter/UI state
4. **URL persistence** - Layer visibility stored in URL params
5. **Debounced updates** - Map updates are debounced for smooth interaction

## Change Log

### v2.0.0 (December 2024)

- Complete redesign with Google Maps-inspired UI
- Added glassmorphic styling throughout
- Implemented map style selector (Topo v4 default)
- Added layer toggles with opacity controls
- Redesigned filter panel with functional filters
- Added navigation controls (zoom, compass, locate, 3D, terrain)
- Refactored to SOLID/DRY architecture
- Improved performance with memoization
- Added TypeScript types for extensibility
