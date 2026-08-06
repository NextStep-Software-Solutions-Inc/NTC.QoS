# Barangay Search UI Preview

## Search Dropdown Structure

```
┌─────────────────────────────────────────┐
│  🔍 Search locations, barangays...     │
└─────────────────────────────────────────┘
         ↓ (User types "san")
┌─────────────────────────────────────────┐
│  BARANGAYS                              │
│  ───────────────────────────────────── │
│  📍 Brgy. San Antonio                   │
│     Manila, Metro Manila                │
│                                         │
│  📍 Brgy. San Miguel                    │
│     Pasig, Metro Manila                 │
│                                         │
│  📍 Brgy. San Isidro                    │
│     Quezon City, Metro Manila           │
│                                         │
│  MUNICIPALITIES & REGIONS               │
│  ───────────────────────────────────── │
│  📍 San Juan                            │
│     Metro Manila, Philippines           │
│                                         │
│  📍 San Fernando                        │
│     Pampanga, Central Luzon             │
└─────────────────────────────────────────┘
```

## When Barangay is Selected

```
Map View:
┌───────────────────────────────────────────────┐
│                                               │
│         ╔═══════════════════╗                │
│         ║   Brgy. Boundary  ║  ← Enhanced    │
│         ║                   ║     Highlighting│
│         ║    ┌─────────┐   ║                │
│         ║    │         │   ║  • Thick border│
│         ║    │  Area   │   ║  • Blue fill   │
│         ║    │         │   ║  • Glow effect │
│         ║    └─────────┘   ║                │
│         ║                   ║                │
│         ╚═══════════════════╝                │
│                                               │
│  Map auto-zooms to fit boundary              │
└───────────────────────────────────────────────┘
```

## Visual Styling Details

### Section Headers
- **Style**: Uppercase, gray, small font
- **Purpose**: Clearly separate result types
- **Text**:
  - "BARANGAYS" for backend API results
  - "MUNICIPALITIES & REGIONS" for MapTiler results

### Result Items
- **Barangay Format**: 
  - Primary: "Brgy. {name}" (bold)
  - Secondary: "{municipality}, {province}" (gray, smaller)
  
- **MapTiler Format**:
  - Primary: "{place_name}" (bold)
  - Secondary: "{place_type}" (gray, smaller, capitalized)

### Highlighting
- **Border**: 4px dashed blue line with 90% opacity
- **Fill**: Light blue (rgba(59, 130, 246, 0.25)) with 40% opacity
- **Glow**: 8px blur with 20% opacity for outer emphasis
- **Animation**: Smooth zoom and pan to fit bounds

### Interaction
- **Hover**: Light blue background (10% opacity)
- **Selected**: Same as hover for keyboard navigation
- **Click**: Fetch boundaries and display on map

## Example User Flow

1. **User types "manila"**
   - Shows barangays with "manila" in name (case-insensitive)
   - Shows municipalities/regions with "manila" from MapTiler
   - Both sections visible with clear headers

2. **User selects "Brgy. Malate"** (barangay)
   - Fetches GET /barangays/{id} for boundaries
   - Displays MultiPolygon geometry on map
   - Zooms to fit barangay bounds
   - Applies enhanced highlighting

3. **User selects "Manila"** (city from MapTiler)
   - Uses existing MapTiler geocoding
   - Displays city boundaries
   - Zooms to fit city bounds
   - Same highlighting style

## Benefits of Clear Separation

✅ **Better UX**: Users immediately see what type of result they're looking at
✅ **Faster Selection**: Barangay seekers don't need to scroll past municipalities
✅ **Visual Hierarchy**: Clear organizational structure
✅ **Scalability**: Easy to add more result types in future (e.g., landmarks, addresses)
✅ **Accessibility**: Screen readers can announce section context

## Responsive Design

- Mobile: Full width, stacked sections
- Desktop: Same layout, max-width 28rem (448px)
- Tablet: Adapts to available space
- All sizes: Scrollable if results exceed max-height (20rem/320px)
