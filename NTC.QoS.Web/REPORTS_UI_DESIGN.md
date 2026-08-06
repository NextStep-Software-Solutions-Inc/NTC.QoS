# Reports Feature - UI Design Documentation

## Page Layout

### Main Reports Page (`/reports`)

```
┌────────────────────────────────────────────────────────────────────────┐
│ NAVBAR                                                                 │
│ [Region Selector] [Home] [Map] [Broadband] [Network] [Imports] [REPORTS]│
└────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│  Reports                                                               │
│  Generate and export comprehensive reports on broadband coverage       │
│  and connectivity gaps                                                 │
│                                                                        │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ [Wired Broadband] [Wireless Broadband] [Connectivity Gap]       │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│  [SELECTED TAB CONTENT APPEARS HERE]                                   │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

## Tab 1: Wired Broadband Report

```
┌────────────────────────────────────────────────────────────────────────┐
│ 📄 Wired Broadband Report                                             │
│ View and export detailed wired broadband connectivity reports          │
│ with barangay-level data                                               │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│ Filters:                                                               │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│ │ Region ▼     │ │ Province ▼   │ │Municipality ▼│ │ Status ▼     │ │
│ │ All Regions  │ │              │ │              │ │ All Statuses │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘ │
│                                                                        │
│                          [Export PDF] [Export Excel ⬇]                │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ [Summary] [Municipalities]                                       │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ Summary View:                                                          │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│ │ Total Munic. │ │Total Barangays│ │   Served ✓   │ │ Underserved  │ │
│ │    150       │ │    2,500      │ │    1,200     │ │     800      │ │
│ │              │ │               │ │   48.0%      │ │    32.0%     │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘ │
│                                                                        │
│ ┌──────────────┐                                                      │
│ │  Unserved ✗  │                                                      │
│ │     500      │                                                      │
│ │   20.0%      │                                                      │
│ └──────────────┘                                                      │
│                                                                        │
│ Provider Coverage:                                                     │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ PLDT: 450   Globe: 380   Smart: 320   Converge: 280             │ │
│ │ Sky Broadband: 150   Dito: 120                                   │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ Municipalities View:                                                   │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ 📍 Manila                                               [NCR]     │ │
│ │ Total: 897 | Served: 780 | Underserved: 90 | Unserved: 27      │ │
│ │ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐             │ │
│ │ │ Ermita       │ │ Malate       │ │ Intramuros   │             │ │
│ │ │ [Served]     │ │ [Served]     │ │ [Underserved]│ ... (more)  │ │
│ │ └──────────────┘ └──────────────┘ └──────────────┘             │ │
│ │ Showing 10 of 897 barangays                                      │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│                    [◀ Previous]  Page 1 of 15  [Next ▶]              │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

## Tab 2: Wireless Broadband Report

```
┌────────────────────────────────────────────────────────────────────────┐
│ 📡 Wireless Broadband Report                                          │
│ Generate and export wireless broadband coverage reports by region,     │
│ province, or municipality                                              │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│ Filters:                                                               │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│ │ Region ▼     │ │ Province ▼   │ │Municipality ▼│ │ Format ▼     │ │
│ │ All Regions  │ │              │ │              │ │ Excel        │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘ │
│                                                                        │
│                                    [Download Report ⬇]                │
│                                                                        │
│ When loading:                                                          │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ ▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ (Loading...)  │ │
│ │                                                                  │ │
│ │ Please wait, generating report... This may take a few moments.   │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

## Tab 3: Connectivity Gap Report

```
┌────────────────────────────────────────────────────────────────────────┐
│ ⚠️ Connectivity Gap Report                                            │
│ Identify priority areas with connectivity gaps and underserved         │
│ populations                                                            │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│ Filters:                                                               │
│ ┌──────────────────────────┐ ┌──────────────────────────┐           │
│ │ Region ▼                 │ │ Province ▼               │           │
│ │ All Regions              │ │                          │           │
│ └──────────────────────────┘ └──────────────────────────┘           │
│                                                                        │
│                                  [Export PDF] [Export Excel ⬇]        │
│                                                                        │
│ Statistics:                                                            │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│ │ Unserved     │ │ Underserved  │ │  Unserved    │ │ Underserved  │ │
│ │   Areas      │ │   Areas      │ │  Population  │ │  Population  │ │
│ │    42 🔴     │ │    89 🟡     │ │  125,000 🔴  │ │  280,000 🟡  │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘ │
│                                                                        │
│ Priority Areas                                                         │
│ Areas requiring immediate attention based on connectivity status       │
│ and population                                                         │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ 📍 Barangay Pag-asa, Municipality X         [Unserved 🔴]        │ │
│ │ Population: 5,200 | Priority Score: 95 | Last Assessed: Dec 20  │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ 📍 Barangay San Roque, Municipality Y      [Underserved 🟡]      │ │
│ │ Population: 3,800 | Priority Score: 78 | Last Assessed: Dec 19  │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ 📍 Barangay Masaya, Municipality Z          [Unserved 🔴]        │ │
│ │ Population: 4,500 | Priority Score: 85 | Last Assessed: Dec 18  │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│                Generated at: December 21, 2024 4:15 AM               │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

## Color Scheme

### Status Badges
- **Served (Green)**: `bg-green-50 text-green-700 border-green-200`
- **Underserved (Yellow)**: `bg-yellow-50 text-yellow-700 border-yellow-200`
- **Unserved (Red)**: `bg-red-50 text-red-700 border-red-200`

### Cards
- **Default**: White background with gray border
- **Statistics Cards**: Color-coded borders matching status
- **Hover States**: `hover:bg-gray-50` transition

### Buttons
- **Primary**: Blue background (`bg-blue-600`)
- **Secondary/Outline**: White with blue border
- **Disabled**: Grayed out with reduced opacity

### Progress Bar
- **Track**: Light gray background (`bg-secondary`)
- **Indicator**: Blue fill (`bg-primary`) with animated shimmer
- **Loading State**: Indeterminate progress

## Icons Used (Lucide React)

- 📄 `FileText` - Wired Broadband, Export PDF
- 📡 `Signal` - Wireless Broadband
- ⚠️ `AlertTriangle` - Connectivity Gap, Warnings
- ⬇️ `Download` - Export Excel, Download
- ⏱️ `Loader2` - Loading states (spinning animation)
- 📍 `MapPin` - Location indicators
- 🏠 `Home` - Home navigation
- 🗺️ `Map` - Map navigation

## Responsive Design

### Desktop (≥1024px)
- 4-column grid for filters
- Full labels on navigation
- Extended statistics display
- Side-by-side cards

### Tablet (768px - 1023px)
- 2-column grid for filters
- Abbreviated navigation labels
- Stacked statistics cards

### Mobile (<768px)
- Single column layout
- Icon-only navigation where possible
- Simplified card layouts
- Scrollable content areas

## Interaction States

### Loading
```
[Button with Spinner] Generating...
─────────────────────────────────────
Please wait, generating report... 
This may take a few moments.
```

### Empty State
```
    ⚠️
    
No priority areas identified 
for the selected filters
```

### Error State
```
❌ Failed to load report data

[Retry Button]
```

## Accessibility Features

1. **Keyboard Navigation**: All interactive elements focusable
2. **Screen Readers**: Proper ARIA labels and descriptions
3. **Color Contrast**: WCAG AA compliant
4. **Loading Announcements**: Screen reader notifications
5. **Form Labels**: Associated with inputs
6. **Focus Indicators**: Visible focus rings

## Animation & Transitions

- **Button Hover**: 150ms ease-in-out
- **Card Hover**: Background color fade 200ms
- **Tab Switch**: Content fade 300ms
- **Progress Bar**: Shimmer animation (indeterminate)
- **Loader Spin**: Continuous rotation 1s linear

## Typography

- **Page Title**: 3xl, bold, tight tracking
- **Card Titles**: lg, medium
- **Descriptions**: sm, muted foreground
- **Statistics**: 2xl, bold (numbers)
- **Labels**: sm, medium
- **Body Text**: sm, regular

## Spacing System

- **Page Padding**: p-6 (1.5rem)
- **Card Gap**: gap-4 (1rem)
- **Section Spacing**: space-y-4 (1rem vertical)
- **Inline Elements**: gap-2 (0.5rem)
- **Button Padding**: px-3 py-2 or px-4 py-2

This design follows the established NTC QoS design system and maintains consistency with the rest of the application.
