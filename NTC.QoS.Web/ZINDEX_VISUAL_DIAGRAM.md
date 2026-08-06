# Visual Z-Index Layering Diagram

## BEFORE (z-40 for both)
```
┌─────────────────────────────────────────────────────┐
│                   Admin Map View                    │
│                                                     │
│  ┌────────────────┐                                │
│  │ Filter Panel   │              ┌──────────────┐  │
│  │                │              │ Nav Controls │  │
│  │  (z-40)        │              │   (z-40)     │  │
│  │                │              └──────────────┘  │
│  │                │                                │
│  │                │                                │
│  │                │              ┌──────────────┐  │
│  │                │              │   Layers     │  │
│  │                │              │   (z-40)     │  │
│  └────────────────┘              └──────────────┘  │
│                                                     │
│  ┌────────────┐ ┌─────────────┐                   │
│  │ Map Style  │ │ Analytics   │  ← SAME z-40!     │
│  │  (z-40)    │ │   (z-40)    │     CONFLICT!     │
│  └────────────┘ └─────────────┘                   │
└─────────────────────────────────────────────────────┘
```

## AFTER (Filter Panel z-60, controls z-40)
```
┌─────────────────────────────────────────────────────┐
│                   Admin Map View                    │
│                                                     │
│  ┏━━━━━━━━━━━━━━━━┓ ← TOPMOST!                     │
│  ┃ Filter Panel   ┃              ┌──────────────┐  │
│  ┃                ┃              │ Nav Controls │  │
│  ┃  (z-60)        ┃              │   (z-40)     │  │
│  ┃                ┃              └──────────────┘  │
│  ┃  ┌──────────┐  ┃                                │
│  ┃  │ Covers → │  ┃                                │
│  ┃  │ controls │  ┃              ┌──────────────┐  │
│  ┃  └──────────┘  ┃              │   Layers     │  │
│  ┃                ┃              │   (z-40)     │  │
│  ┗━━━━━━━━━━━━━━━━┛              └──────────────┘  │
│                                                     │
│  ┌────────────┐ ┌─────────────┐                   │
│  │ Map Style  │ │ Analytics   │  ← Behind panel   │
│  │  (z-40)    │ │   (z-40)    │     when expanded │
│  └────────────┘ └─────────────┘                   │
└─────────────────────────────────────────────────────┘
```

## Key Behavior Changes

### Filter Panel Expanded (400px wide)
```
LEFT SIDE OF SCREEN:
┌─────────────────────────┐
│                         │
│  FILTER PANEL (z-60)    │
│  ┏━━━━━━━━━━━━━━━━━━━┓  │
│  ┃ Search            ┃  │
│  ┃ Filters           ┃  │
│  ┃ Layers            ┃  │
│  ┃                   ┃  │
│  ┃ (Covers the       ┃  │
│  ┃  Map Style &      ┃  │
│  ┃  Analytics below) ┃  │
│  ┗━━━━━━━━━━━━━━━━━━━┛  │
│                         │
│  [Hidden: Map Style]    │ ← Not visible when panel expanded
│  [Hidden: Analytics]    │ ← Not visible when panel expanded
└─────────────────────────┘
```

### Filter Panel Collapsed
```
LEFT SIDE OF SCREEN:
┌─────────────────────────┐
│                         │
│  [▶] ← Collapsed button │
│                         │
│                         │
│                         │
│                         │
│                         │
│                         │
│                         │
│  ┌──────────┐           │
│  │Map Style │           │ ← Fully visible
│  └──────────┘           │
│  ┌──────────┐           │
│  │Analytics │           │ ← Fully visible
│  └──────────┘           │
└─────────────────────────┘
```

## Z-Index Stack Visualization

```
     TOP
      ↑
━━━━━━━━━━━━━━━━━━━━━━━━━
   SearchBar (z-[2147483647])
━━━━━━━━━━━━━━━━━━━━━━━━━
   SearchBar Container (z-[10000])
━━━━━━━━━━━━━━━━━━━━━━━━━
   🆕 Filter Panel (z-60)      ← NEW: Topmost UI layer
━━━━━━━━━━━━━━━━━━━━━━━━━
   Broadband Panel (z-50)
   Cell Details (z-50)
   Map Style Dropdown (z-50)
   Indicators (z-50)
━━━━━━━━━━━━━━━━━━━━━━━━━
   Map Style Button (z-40)     ← Behind Filter Panel
   Analytics Button (z-40)     ← Behind Filter Panel
   Navigation (z-40)
   Layer Controls (z-40)
━━━━━━━━━━━━━━━━━━━━━━━━━
   Attribution (z-30)
━━━━━━━━━━━━━━━━━━━━━━━━━
   Map Container (z-0)
━━━━━━━━━━━━━━━━━━━━━━━━━
     BOTTOM
      ↓
```
