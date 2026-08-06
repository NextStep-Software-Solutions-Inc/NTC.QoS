# Barangay Coverage UI Design Documentation

## Overview
The Barangay Coverage page provides a rich, colorful, and animated interface for managing broadband coverage data across all barangays in a region.

## Visual Design

### Color Palette (Pastel Theme)
- **Primary**: Purple (#9333EA to #EC4899) - Gradients for main elements
- **Success**: Green (#10B981 to #059669) - Covered items and save buttons
- **Info**: Blue (#3B82F6 to #2563EB) - Wireless coverage indicators
- **Warning**: Amber (#F59E0B to #F97316) - Unsaved changes
- **Neutral**: Gray - Uncovered items
- **Background**: Gradient from purple-50/pink-50/blue-50 - Soft pastel backdrop

### Layout Structure

```
┌─────────────────────────────────────────────────────────────────┐
│ [Card with purple-pink-blue gradient background]                │
│                                                                   │
│  Barangay Coverage Management                                    │
│  Manage broadband coverage data for barangays in your region... │
│                                                                   │
│ ┌───────────────────────────────────────────────────────────┐  │
│ │ [Scrollable List Area]                                      │  │
│ │                                                             │  │
│ │ ┌─────────────────────────────────────────────────────┐   │  │
│ │ │ ☐ San Jose                     [012345]             │   │  │
│ │ │                                                       │   │  │
│ │ │   Wired FTTH Coverage (%)    Wireless (%)   Combined │   │  │
│ │ │   [75.5    %] ████████        [85.0    %]   100.0%  │   │  │
│ │ │   ━━━━━━━━━━━━━━━             ━━━━━━━━━━━━         │   │  │
│ │ │   (purple gradient bar)       (blue gradient bar)   │   │  │
│ │ │                                                       │   │  │
│ │ │   Last updated: 12/28/2025  By: admin@example.com   │   │  │
│ │ └─────────────────────────────────────────────────────┘   │  │
│ │                                                             │  │
│ │ ┌─────────────────────────────────────────────────────┐   │  │
│ │ │ ☑ Santa Cruz           [012346] [Wired 100%] [...]  │ [Save] │
│ │ │                                                       │   │  │
│ │ │   Wired FTTH Coverage (%)    Wireless (%)   Combined │   │  │
│ │ │   [90.0    %] ████████████    [95.0    %]   100.0%  │   │  │
│ │ │   ━━━━━━━━━━━━━━━━━━          ━━━━━━━━━━━━━━       │   │  │
│ │ │   (purple gradient bar)       (blue gradient bar)   │   │  │
│ │ │                                                       │   │  │
│ │ │   [Unsaved] badge (amber color)                      │   │  │
│ │ └─────────────────────────────────────────────────────┘   │  │
│ │                                                             │  │
│ │ ┌─────────────────────────────────────────────────────┐   │  │
│ │ │ ☑ Bagumbayan           [012347] [Wireless 100%]      │   │  │
│ │ │   (Green border - covered)                           │   │  │
│ │ │   ...                                                │   │  │
│ │ └─────────────────────────────────────────────────────┘   │  │
│ │                                                             │  │
│ └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Interactive Elements

### 1. Barangay Item Card
Each barangay is displayed in an animated card with:
- **Border Colors**:
  - Gray (border-gray-200): Uncovered, no changes
  - Green (border-green-300): Covered (wired or wireless at 100%)
  - Amber (border-amber-300): Unsaved changes (dirty state)
- **Background Gradients**:
  - White: Default state
  - Green-to-emerald gradient: Covered
  - Amber-to-orange gradient: Unsaved changes
- **Animations**: 
  - Slide-in from bottom with stagger effect (0.02s delay per item)
  - Scale/fade transitions on state changes

### 2. Checkbox
- **Location**: Left side of each card
- **Visual**: 5x5 size, green when checked
- **Behavior**: 
  - Checking sets both wired and wireless to 100%
  - Unchecking sets both to 0%
  - Marks item as dirty (unsaved)

### 3. Barangay Information
- **Name**: Bold, large text (text-lg font-semibold)
- **Code**: Badge with blue background (bg-blue-100)
- **Status Badges**:
  - "Unsaved" - Amber (bg-amber-100)
  - "Wired 100%" - Green (bg-green-100)
  - "Wireless 100%" - Blue (bg-blue-100)

### 4. Coverage Input Fields
Three columns in responsive grid:

#### Wired FTTH Coverage
- Number input (0-100, step 0.1)
- Purple border focus (border-purple-200)
- Progress bar below with purple gradient
- Animated width transition

#### Wireless Coverage
- Number input (0-100, step 0.1)
- Blue border focus (border-blue-200)
- Progress bar below with blue gradient
- Animated width transition

#### Combined Coverage
- Read-only display
- Shows min(wired + wireless, 100)
- Large, bold number in gray box
- Green gradient progress bar
- Automatically calculated

### 5. Progress Bars
- **Height**: 2px (h-2)
- **Base**: Gray background (bg-gray-200)
- **Fill**: Animated gradient
  - Wired: Purple (from-purple-400 to-purple-600)
  - Wireless: Blue (from-blue-400 to-blue-600)
  - Combined: Green (from-green-400 to-emerald-600)
- **Animation**: Smooth width transition (0.5s duration)

### 6. Save Button
- **Visibility**: Only appears when item has unsaved changes
- **Animation**: Scale-in from 0 with fade
- **Style**: Green gradient (from-green-500 to-emerald-600)
- **States**:
  - Normal: Shadow-md, hover increases shadow
  - Saving: Shows spinner, disabled
  - Success: Toast notification

### 7. Metadata Display
- **Text**: Small gray text (text-xs text-gray-500)
- **Content**: Last updated date and username
- **Location**: Bottom of card

## Animations

### Entry Animation
```javascript
initial={{ opacity: 0, y: 20 }}
animate={{ opacity: 1, y: 0 }}
transition={{ delay: index * 0.02, duration: 0.3 }}
```
- Each item fades in and slides up
- Staggered by 0.02s per item for cascade effect

### Progress Bar Animation
```javascript
initial={{ width: 0 }}
animate={{ width: `${percentage}%` }}
transition={{ duration: 0.5 }}
```
- Bars grow from 0 to target percentage
- Smooth 0.5s animation

### Save Button Animation
```javascript
initial={{ scale: 0, opacity: 0 }}
animate={{ scale: 1, opacity: 1 }}
exit={{ scale: 0, opacity: 0 }}
```
- Scales in when changes detected
- Scales out when saved

## User Interactions

### Workflow 1: Mark as Covered
1. User checks checkbox
2. Card border turns amber (unsaved)
3. Both percentages set to 100%
4. Progress bars animate to full
5. Save button appears with scale animation
6. User clicks save
7. Button shows spinner
8. Toast notification on success
9. Border turns green (covered)
10. Save button disappears

### Workflow 2: Edit Percentages
1. User types in percentage field
2. Value clamped to 0-100
3. Progress bar animates to new width
4. Combined percentage updates automatically
5. Card marked as dirty (amber border)
6. "Unsaved" badge appears
7. Save button scales in
8. User clicks save
9. API call executes
10. Coverage data refetched
11. UI updates with clean state

### Workflow 3: View Coverage
1. Page loads with loading spinner
2. Barangays fetch from region
3. Coverage data fetch for region
4. Items animate in with stagger
5. Covered items show green borders
6. Uncovered items show gray borders
7. All have interactive controls ready

## Responsive Design
- **Mobile**: Single column, full width
- **Tablet**: Single column, constrained width
- **Desktop**: Single column with comfortable spacing
- **Input grid**: Stacks on mobile, 3 columns on desktop

## Accessibility
- Semantic HTML with labels
- Keyboard navigation support
- ARIA labels on interactive elements
- High contrast color combinations
- Clear visual feedback

## Performance
- Virtualization ready (AnimatePresence supports it)
- Optimistic updates for instant feedback
- Debounced input if needed
- Efficient re-renders with React.memo potential

## Toast Notifications
- Success: Green with checkmark
- Error: Red with error icon
- Messages:
  - "Updated coverage for [barangay name]"
  - "Created coverage for [barangay name]"
  - "Failed to save coverage for [barangay name]"
