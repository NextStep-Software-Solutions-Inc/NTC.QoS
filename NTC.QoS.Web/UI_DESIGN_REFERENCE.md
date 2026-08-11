# Cell Site Management UI - Visual Design Reference

## Page Layout Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                       │
│  ╔═══════════════════════════════════════════════════════════════╗  │
│  ║  [Satellite Icon]     CELL SITE MANAGEMENT                    ║  │
│  ║                                                                ║  │
│  ║  Monitor and manage cell tower infrastructure                 ║  │
│  ║  across all regions                                           ║  │
│  ║                                                                ║  │
│  ║  [📡 1,234 Total Sites]  [🔍 Advanced Filtering]  [⚡ Search] ║  │
│  ╚═══════════════════════════════════════════════════════════════╝  │
│       Gradient: Blue → Indigo → Purple with animated orbs          │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 🔍 Search: [________________________]  [Columns ▼] [Clear]  │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 🔽 Filters:                                                   │   │
│  │   [All Status ▼] [All Providers ▼] [Province ▼]             │   │
│  │   [Municipality ▼] [Barangay ▼]                             │   │
│  └──────────────────────────────────────────────────────────────┘   │
│       Blue-tinted background, rounded corners                       │
│                                                                       │
│  ╔═══════════════════════════════════════════════════════════════╗  │
│  ║ ☑ │ Name        │ Status  │ Provider    │ Coordinates    │ ⚙ ║  │
│  ╠═══╪═════════════╪═════════╪═════════════╪════════════════╪═══╣  │
│  ║ ☐ │ 🗼 Tower A  │ ● Active │ [LOGO] Globe│ 14.5995,121.01│ ⋮ ║  │
│  ║ ☐ │ 🗼 Tower B  │ ● Maint. │ [LOGO] Smart│ 14.5994,121.02│ ⋮ ║  │
│  ║ ☐ │ 🗼 Tower C  │ ● Inact. │ [PLDT] PLDT │ 14.5993,121.03│ ⋮ ║  │
│  ╚═══╧═════════════╧═════════╧═════════════╧════════════════╧═══╝  │
│       Gradient header, hover effects, smooth transitions             │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 3 of 20 selected    Rows/page: [20▼]    Page 1 of 10       │   │
│  │                                                               │   │
│  │ Go to: [__] [Go]  [⏮][◀ Prev] [Next ▶][⏭]                  │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Color Scheme

### Status Badges
```
┌────────────────────────────────────────────────────┐
│ ● Active      → Emerald (#10B981)  [Bright Green] │
│ ● Inactive    → Slate   (#64748B)  [Cool Gray]    │
│ ● Maintenance → Amber   (#F59E0B)  [Warm Yellow]  │
│ ✓ Verified    → Blue    (#3B82F6)  [Vibrant Blue] │
└────────────────────────────────────────────────────┘
```

### Provider Display
```
┌──────────────────────────────────────────────────┐
│ With Logo:                                       │
│ ┌────────┐                                      │
│ │ [LOGO] │ Globe Telecom                        │
│ └────────┘                                      │
│   ↑ Brand color border                          │
│                                                  │
│ Without Logo:                                    │
│ ┌────┐                                          │
│ │ GL │ Globe Telecom                            │
│ └────┘                                          │
│   ↑ Brand color background, white text          │
└──────────────────────────────────────────────────┘
```

### Hero Section
```
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║  ○ Decorative orb (pink-purple gradient, blurred)       ║
║                                                           ║
║    ┌──────────┐                                          ║
║    │          │   CELL SITE MANAGEMENT                   ║
║    │  ✨ 📡  │                                          ║
║    │          │   Monitor and manage infrastructure      ║
║    └──────────┘                                          ║
║    Satellite icon                                        ║
║    in glowing box                                        ║
║                                                           ║
║    ○ Decorative orb (cyan-blue gradient, blurred)       ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
  Background: Blue (#3B82F6) → Indigo (#6366F1) → Purple (#A855F7)
```

### Batch Actions Panel (when items selected)
```
┌────────────────────────────────────────────────────────────┐
│  ┌───┐                                                     │
│  │ 3 │ item(s) selected                                   │
│  └───┘                                                     │
│  Blue circle                                               │
│                                                             │
│  [✓ Update Status ▼]  [🗑 Delete Selected]                │
│   Green accent         Red accent                          │
└────────────────────────────────────────────────────────────┘
  Background: Blue/Indigo gradient, blue border
```

## Component States

### Table Row States
```
Default:        White background
Hover:          Light gray background, smooth transition
Selected:       Blue-tinted background + blue left border
Loading:        Animated gradient skeleton
```

### Filter Inputs
```
Enabled:   White background, 2px border
Disabled:  Gray background, muted text
Focused:   Blue ring, 2px border
```

### Buttons
```
Primary:     Solid color, white text, shadow
Secondary:   Outline, colored text and border
Destructive: Red background, white text
Disabled:    Gray, reduced opacity
```

## Typography

### Headings
```
Page Title:     text-5xl font-bold (Cell Site Management)
Section Title:  text-3xl font-bold
Card Title:     text-lg font-medium
Labels:         text-sm font-medium
Body:           text-sm
```

### Font Weights
```
Bold:      font-bold     (700)
Semibold:  font-semibold (600)
Medium:    font-medium   (500)
Regular:   font-normal   (400)
```

## Spacing & Layout

### Container
```
Max Width:  container (1280px)
Padding:    py-6 (1.5rem vertical)
Gap:        gap-6 (1.5rem between sections)
```

### Components
```
Hero Section:     p-8 rounded-2xl
Filter Section:   p-4 rounded-lg
Table:            border-2 rounded-lg
Pagination:       py-4 px-4
Batch Actions:    p-4 rounded-lg
```

## Responsive Breakpoints

```
Mobile:   < 640px  (sm)
Tablet:   640px+   (md)
Desktop:  1024px+  (lg)
Wide:     1280px+  (xl)
```

## Animations

### Transitions
```
Hover:      transition-all duration-150
Fade:       opacity transition
Slide:      transform transition
Skeleton:   animate-pulse (loading)
```

### Hero Orbs
```
Background orbs with blur-3xl
Subtle opacity (0.2)
Positioned absolutely
```

## Icons

### Used Throughout
```
📡 Satellite  - Hero icon
🗼 RadioTower - Cell sites, empty state
🔍 Search     - Search input
🔽 Filter     - Filter section
✓ Check       - Verified badge
● Dot         - Status indicators
⋮ MoreVert    - Row actions menu
📍 MapPin     - Coordinates
⚙ Settings    - Column visibility
```

## Accessibility Features

```
✓ Semantic HTML
✓ ARIA labels
✓ Keyboard navigation
✓ Focus indicators
✓ Color contrast (WCAG AA)
✓ Screen reader support
```

## Dark Mode Support

```
All components support dark mode with appropriate color variants:
- Background: dark:bg-slate-950
- Text: dark:text-gray-100
- Borders: dark:border-slate-800
- Accents: Maintain vibrant colors
```

---

**Visual Reference Created**: November 24, 2025
**Design System**: Tailwind CSS + shadcn/ui
**Color Palette**: Blue, Indigo, Purple, Emerald, Amber, Slate
