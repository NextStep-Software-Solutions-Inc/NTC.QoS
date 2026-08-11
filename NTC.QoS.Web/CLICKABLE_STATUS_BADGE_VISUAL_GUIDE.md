# Clickable Status Badge - Visual Flow Guide

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     CELL SITES MANAGEMENT PAGE                              │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │ Cell Card                                                              │ │
│  │                                                                        │ │
│  │  ☐  📡  Site ABC-123                                     ⋮            │ │
│  │                                                                        │ │
│  │      Provider: Globe                                                  │ │
│  │      CID: 12345  •  LAC: 100                                          │ │
│  │                                                                        │ │
│  │      ╭─────────╮  ◄─── CLICKABLE!                                     │ │
│  │      │ Unknown │       Click to update status                         │ │
│  │      ╰─────────╯                                                      │ │
│  │                                                                        │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│                                  │                                          │
│                                  │ USER CLICKS STATUS BADGE                │
│                                  ▼                                          │
└─────────────────────────────────────────────────────────────────────────────┘

                                   ⬇️
                        Navigates with ?editStatus=true
                                   ⬇️

┌─────────────────────────────────────────────────────────────────────────────┐
│                       CELL SITE DETAIL PAGE                                 │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  📡 Cell Site Header                                                 │   │
│  │                                                                       │   │
│  │  ID: ABC-123                                                         │   │
│  │                                                                       │   │
│  │  Site ABC-123    🛡️ Verified  ╭──────────────╮  ❌                 │   │
│  │                                │ ⚠️ Unknown ▾ │  ◄── Status Editor   │   │
│  │                                │────────────  │      with Cancel      │   │
│  │  Provider: Globe               │  Unknown     │                       │   │
│  │                                │  Pending     │                       │   │
│  │                                │  Licensed    │                       │   │
│  │                                ╰──────────────╯                       │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  Cell Details...                                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

                                   ⬇️
                         USER SELECTS NEW STATUS
                                   ⬇️

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                    ✅ Status updated to Pending                             │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  📡 Cell Site Header                                                 │   │
│  │                                                                       │   │
│  │  ID: ABC-123                                                         │   │
│  │                                                                       │   │
│  │  Site ABC-123    🛡️ Verified  ╭────────╮                           │   │
│  │                                │ Pending │  ◄── Updated Badge        │   │
│  │  Provider: Globe               ╰────────╯                            │   │
│  │                                                                       │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [Page reloads after 500ms to show updated data]                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component States

### 1. Card Status Badge (CellsitesTable/table.tsx)

```tsx
╭─────────────────────────────────────────────────────────────╮
│ REGULAR STATE (Not Clickable)                              │
│                                                             │
│  ┌─────────┐                                                │
│  │ Unknown │  ◄── Static badge, no cursor change           │
│  └─────────┘                                                │
│                                                             │
╰─────────────────────────────────────────────────────────────╯

╭─────────────────────────────────────────────────────────────╮
│ CLICKABLE STATE (With onClick handler)                     │
│                                                             │
│  ┌─────────┐                                                │
│  │ Unknown │  ◄── Cursor: pointer                          │
│  └─────────┘      Hover: opacity-80                        │
│      ↑            Click: Navigate to ?editStatus=true      │
│      └── handleStatusClick                                 │
│                                                             │
╰─────────────────────────────────────────────────────────────╯
```

---

### 2. Detail Page Status Badge (CellsiteDetailPage.tsx)

```tsx
╭─────────────────────────────────────────────────────────────╮
│ VIEW MODE (editingStatus = false, isEditing = false)       │
│                                                             │
│  ┌────────────────────────────────────┐                     │
│  │ ⚠️ Unknown                         │  ◄── Static display │
│  └────────────────────────────────────┘                     │
│                                                             │
╰─────────────────────────────────────────────────────────────╯

╭─────────────────────────────────────────────────────────────╮
│ STATUS EDIT MODE (editingStatus = true)                    │
│                                                             │
│  ┌──────────────┐  ┌───┐                                    │
│  │ ⚠️ Unknown ▾ │  │ ❌ │  ◄── Cancel button                │
│  └──────────────┘  └───┘                                    │
│        ↓                                                    │
│   Dropdown opens                                            │
│   ┌──────────────┐                                          │
│   │ Unknown      │                                          │
│   │ Pending      │  ◄── Click to update                    │
│   │ Licensed     │                                          │
│   └──────────────┘                                          │
│                                                             │
╰─────────────────────────────────────────────────────────────╯

╭─────────────────────────────────────────────────────────────╮
│ FULL EDIT MODE (isEditing = true)                          │
│                                                             │
│  ┌────────────────────────────────┐                         │
│  │ ⚠️ Unknown                  ▾ │  ◄── Inline with form   │
│  └────────────────────────────────┘                         │
│       Status updates with Save/Cancel                       │
│                                                             │
╰─────────────────────────────────────────────────────────────╯
```

---

## Event Flow Diagram

```
USER ACTION                 COMPONENT                  API CALL
─────────────────────────────────────────────────────────────────

1. Click Status       →  handleStatusClick()
   Badge on Card          ↓
                          e.stopPropagation()
                          ↓
                          onViewDetails(id, "editStatus=true")
                          ↓
                      ┌──────────────────────┐
                      │ Navigate to Detail   │
                      │ /cells/:id?          │
                      │ editStatus=true      │
                      └──────────────────────┘

                                                         ─────────

2. Page Loads         →  useEffect()
   with ?editStatus=      ↓
   true                   searchParams.get("editStatus")
                          ↓
                          setEditingStatus(true)
                          ↓
                      ┌──────────────────────┐
                      │ Show Status          │
                      │ Dropdown Editor      │
                      └──────────────────────┘

                                                         ─────────

3. Select New         →  handleStatusChange()     →   PATCH
   Status from            ↓                            /api/cells/:id/status
   Dropdown               updateStatusMutation          ↓
                          .mutateAsync()                Backend updates DB
                          ↓                             ↓
                          toast.success()               Returns success
                          ↓                             ↓
                          setEditingStatus(false)    ←  
                          ↓
                          setTimeout(() => {
                            window.location.reload()
                          }, 500)

                                                         ─────────

4. Page Reloads       →  getCellDetailsQuery      →   GET
                          refetches data                /api/cells/:id
                          ↓                             ↓
                      ┌──────────────────────┐         Fetch updated cell
                      │ Display Updated      │      ←  ↓
                      │ Status Badge         │         Return cell data
                      └──────────────────────┘
```

---

## Status Badge Colors

```
┌─────────────┬──────────────┬────────────────────────────────┐
│ Status      │ Icon         │ Badge Style                    │
├─────────────┼──────────────┼────────────────────────────────┤
│ Unknown     │ ⚠️ (Alert)  │ Gray bg / Gray text            │
│             │              │ bg-slate-500/20                │
│             │              │ text-slate-300                 │
├─────────────┼──────────────┼────────────────────────────────┤
│ Pending     │ 🕐 (Clock)   │ Amber bg / Amber text          │
│             │              │ bg-amber-500/20                │
│             │              │ text-amber-300                 │
├─────────────┼──────────────┼────────────────────────────────┤
│ Licensed    │ ✅ (Check)   │ Emerald bg / Emerald text      │
│             │              │ bg-emerald-500/20              │
│             │              │ text-emerald-300               │
└─────────────┴──────────────┴────────────────────────────────┘
```

---

## Header Layout Structure

```
┌─────────────────────────────────────────────────────────────────┐
│ Cell Site Detail Header                                         │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ ID: ABC-123                                  ◄── Top        │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ Site ABC-123  🛡️ Verified  ⚠️ Unknown     ◄── Same line   │ │
│  │     ↑              ↑            ↑                          │ │
│  │  Cell Name    Verified      Status Badge                  │ │
│  │               Badge         (Clickable)                    │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ Provider: Globe                            ◄── Below       │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Interaction Matrix

```
╔═══════════════════════╦═══════════════════════╦═══════════════════════╗
║ Component Location    ║ Status Badge State    ║ User Interaction      ║
╠═══════════════════════╬═══════════════════════╬═══════════════════════╣
║ Cell Card (List View) ║ Clickable             ║ Click → Navigate to   ║
║                       ║ cursor-pointer        ║ detail page with      ║
║                       ║ hover:opacity-80      ║ ?editStatus=true      ║
╠═══════════════════════╬═══════════════════════╬═══════════════════════╣
║ Detail Page           ║ Static (view mode)    ║ No interaction        ║
║ (editingStatus=false, ║                       ║                       ║
║  isEditing=false)     ║                       ║                       ║
╠═══════════════════════╬═══════════════════════╬═══════════════════════╣
║ Detail Page           ║ Dropdown with Cancel  ║ Select → API call →   ║
║ (editingStatus=true)  ║                       ║ Toast → Reload        ║
╠═══════════════════════╬═══════════════════════╬═══════════════════════╣
║ Detail Page           ║ Dropdown inline       ║ Select → Updates form ║
║ (isEditing=true)      ║ Part of full form     ║ Save → API call       ║
╚═══════════════════════╩═══════════════════════╩═══════════════════════╝
```

---

## Code Execution Timeline

```
T=0ms    User clicks status badge on card
           ↓
T=10ms   handleStatusClick() executes
           ├─ e.stopPropagation()
           └─ onViewDetails(id, "editStatus=true")
           ↓
T=50ms   Router navigates to /cells/:id?editStatus=true
           ↓
T=100ms  CellsiteDetailPage renders
           ├─ useEffect detects editStatus=true
           └─ setEditingStatus(true)
           ↓
T=150ms  Status dropdown appears in header
           ↓
         [USER INTERACTS WITH DROPDOWN]
           ↓
T=1000ms User selects "Pending"
           ↓
T=1010ms handleStatusChange() executes
           ├─ updateStatusMutation.mutateAsync()
           └─ API call: PATCH /api/cells/:id/status
           ↓
T=1200ms Backend responds with success
           ├─ toast.success("Status updated to Pending")
           └─ setEditingStatus(false)
           ↓
T=1210ms setTimeout(() => window.location.reload(), 500)
           ↓
T=1710ms Page reloads
           ├─ getCellDetailsQuery refetches
           └─ API call: GET /api/cells/:id
           ↓
T=1900ms Updated cell data renders
           └─ Status badge now shows "Pending"
```

---

## Testing Scenarios

### ✅ Happy Path
```
1. Click "Unknown" badge on cell card
2. Verify URL changes to /cells/:id?editStatus=true
3. Confirm status dropdown appears
4. Select "Pending"
5. See toast: "Status updated to Pending"
6. Page reloads automatically
7. Badge now shows "Pending" ✅
```

### ⚠️ Edge Cases
```
1. Click status badge while API is loading
   → Dropdown should be disabled until loaded

2. Click cancel (X) button
   → editingStatus = false
   → Dropdown disappears
   → URL unchanged

3. Navigate away during API call
   → Mutation should be cancelled
   → No toast shown

4. Click Edit button after clicking status badge
   → Should switch to full edit mode
   → Status dropdown integrated into form
```

### 🐛 Error Scenarios
```
1. API returns 404 (cell not found)
   → toast.error("Failed to update status")
   → editingStatus remains true
   → User can retry

2. API returns 401 (unauthorized)
   → toast.error("Failed to update status")
   → User may need to re-authenticate

3. Network error
   → React Query retries automatically
   → Shows error toast if all retries fail
```

---

**Visual Guide Complete!**  
All flows, states, and interactions documented with ASCII diagrams.
