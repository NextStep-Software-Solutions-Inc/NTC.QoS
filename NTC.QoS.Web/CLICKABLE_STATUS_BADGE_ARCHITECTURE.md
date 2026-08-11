# Clickable Status Badge - Complete Architecture Diagram

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                        CLICKABLE STATUS BADGE FEATURE                         ║
║                           Architecture Overview                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌───────────────────────────────────────────────────────────────────────────────┐
│                         LAYER 1: UI COMPONENTS                                │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────┐         ┌─────────────────────────────────┐
│   CellsitesManagementPage       │         │    CellsiteDetailPage           │
│   (List View)                   │         │    (Detail View)                │
│                                 │         │                                 │
│  ┌────────────────────────────┐ │         │  ┌────────────────────────────┐ │
│  │ CellsitesTable             │ │         │  │ Header Section             │ │
│  │                            │ │         │  │                            │ │
│  │  ┌──────────────────────┐  │ │         │  │  ┌──────────────────────┐  │ │
│  │  │ CellCard             │  │ │         │  │  │ Status Badge Area    │  │ │
│  │  │                      │  │ │         │  │  │                      │  │ │
│  │  │  ╭──────────╮        │  │ │  Click  │  │  │  View Mode:          │  │ │
│  │  │  │ Unknown  │ ◄──────┼──┼─┼─────►   │  │  │  ╭──────────╮        │  │ │
│  │  │  ╰──────────╯        │  │ │  Badge  │  │  │  │ Unknown  │        │  │ │
│  │  │  Clickable Badge     │  │ │         │  │  │  ╰──────────╯        │  │ │
│  │  │                      │  │ │         │  │  │                      │  │ │
│  │  └──────────────────────┘  │ │         │  │  │  Edit Mode:          │  │ │
│  │                            │ │         │  │  │  ╭──────────╮  ╭───╮  │  │ │
│  └────────────────────────────┘ │         │  │  │  │Unknown ▾ │  │ X │  │  │ │
│                                 │         │  │  │  ╰──────────╯  ╰───╯  │  │ │
└─────────────────────────────────┘         │  │  │  Dropdown + Cancel   │  │ │
                                            │  │  │                      │  │ │
                                            │  │  └──────────────────────┘  │ │
                                            │  │                            │ │
                                            │  └────────────────────────────┘ │
                                            │                                 │
                                            └─────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│                        LAYER 2: STATE MANAGEMENT                              │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────┐         ┌─────────────────────────────────┐
│   React State (Table)           │         │   React State (Detail)          │
│                                 │         │                                 │
│   • Selected cells              │         │   • isEditing: boolean          │
│   • Filter states               │         │   • editingStatus: boolean      │
│   • Sort preferences            │         │   • editForm: FormData          │
│                                 │         │   • deleteDialogOpen: boolean   │
└─────────────────────────────────┘         └─────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│   URL State (Query Parameters)                                               │
│                                                                               │
│   /cells/:id?editStatus=true  ──► Auto-triggers editingStatus = true         │
│   /cells/:id?edit=true        ──► Auto-triggers isEditing = true             │
│                                                                               │
└───────────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│                       LAYER 3: EVENT HANDLERS                                 │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────┐         ┌─────────────────────────────────┐
│   Table Handlers                │         │   Detail Page Handlers          │
│                                 │         │                                 │
│   handleStatusClick()           │         │   handleStatusChange()          │
│   ├─ e.stopPropagation()        │         │   ├─ updateStatusMutation()     │
│   ├─ Prevent card click         │         │   ├─ toast.success()            │
│   └─ onViewDetails(id, params)  │         │   ├─ setEditingStatus(false)    │
│                                 │         │   └─ window.location.reload()   │
│   handleCardClick()             │         │                                 │
│   ├─ Check if clicking badge    │         │   handleCancel()                │
│   ├─ Return if clickable        │         │   └─ setEditingStatus(false)    │
│   └─ Navigate to detail         │         │                                 │
│                                 │         │   handleEdit()                  │
│   handleViewCell()              │         │   └─ setIsEditing(true)         │
│   ├─ Build URL with params      │         │                                 │
│   └─ router.push(url)           │         │                                 │
│                                 │         │                                 │
└─────────────────────────────────┘         └─────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│                      LAYER 4: DATA FETCHING (React Query)                     │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────┐         ┌─────────────────────────────────┐
│   Query Hooks                   │         │   Mutation Hooks                │
│                                 │         │                                 │
│   useGetAllCells()              │         │   useUpdateCellStatus()         │
│   ├─ Fetches cell list          │         │   ├─ PATCH /cells/:id/status    │
│   ├─ Caching & refetch          │         │   ├─ Optimistic updates         │
│   └─ Loading/error states       │         │   └─ Invalidates queries        │
│                                 │         │                                 │
│   useGetCellDetails()           │         │   useUpdateCell()               │
│   ├─ Fetches single cell        │         │   ├─ PATCH /cells/:id           │
│   ├─ Auto-refetch on mount      │         │   ├─ Full edit update           │
│   └─ Refetch on invalidation    │         │   └─ Invalidates cell details   │
│                                 │         │                                 │
└─────────────────────────────────┘         └─────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│                         LAYER 5: API HANDLERS                                 │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│   editCellStatusRequest.ts                                                 │
│   ├─ Blueprint: MutationBlueprintFromTemplate                              │
│   ├─ Method: PATCH                                                         │
│   ├─ Path: /cells/:id/status                                               │
│   ├─ Payload: string ("Unknown" | "Pending" | "Licensed")                 │
│   └─ Returns: Updated cell object                                          │
│                                                                             │
│   editCellRequest.ts                                                        │
│   ├─ Blueprint: MutationBlueprintFromTemplate                              │
│   ├─ Method: PATCH                                                         │
│   ├─ Path: /cells/:id                                                      │
│   ├─ Payload: EditCellPayload (name, cid, lac, tac, etc.)                 │
│   └─ Returns: Updated cell object                                          │
│                                                                             │
│   getCellDetailsRequest.ts                                                  │
│   ├─ Blueprint: QueryBlueprintFromTemplate                                 │
│   ├─ Method: GET                                                           │
│   ├─ Path: /cells/:id                                                      │
│   └─ Returns: Cell object with full details                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│                          LAYER 6: BACKEND API                                 │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         C# (.NET 8) Backend                                 │
│                                                                             │
│   PATCH /api/cells/:id/status                                              │
│   ├─ Validates status value                                                │
│   ├─ Updates database record                                               │
│   ├─ Returns updated cell                                                  │
│   └─ Logs status change                                                    │
│                                                                             │
│   PATCH /api/cells/:id                                                     │
│   ├─ Validates all fields                                                  │
│   ├─ Updates database record                                               │
│   └─ Returns updated cell                                                  │
│                                                                             │
│   GET /api/cells/:id                                                       │
│   ├─ Fetches cell from database                                            │
│   ├─ Includes related data (provider, etc.)                                │
│   └─ Returns cell object                                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────────┐
│                         LAYER 7: DATABASE                                     │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                            SQL Database                                     │
│                                                                             │
│   Cells Table                                                               │
│   ├─ id: int (PK)                                                           │
│   ├─ name: string                                                           │
│   ├─ status: string ("Unknown" | "Pending" | "Licensed")                   │
│   ├─ cid: int                                                               │
│   ├─ lac: int                                                               │
│   ├─ tac: int                                                               │
│   ├─ siteId: string                                                         │
│   ├─ towerType: string                                                      │
│   ├─ elevation: decimal                                                     │
│   ├─ latitude: decimal                                                      │
│   ├─ longitude: decimal                                                     │
│   ├─ isVerified: boolean                                                    │
│   ├─ serviceProviderId: int (FK)                                            │
│   ├─ createdAt: datetime                                                    │
│   └─ updatedAt: datetime                                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════

                             DATA FLOW DIAGRAM

═══════════════════════════════════════════════════════════════════════════════

┌─────────────────────────────────────────────────────────────────────────────┐
│                        1. USER CLICKS STATUS BADGE                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  handleStatusClick(e)                                                       │
│  ├─ e.stopPropagation()    ──► Prevents card click event                   │
│  └─ onViewDetails(id, "editStatus=true")    ──► Triggers navigation        │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  handleViewCell(id, "editStatus=true")                                      │
│  ├─ Builds URL: /cells/:id?editStatus=true                                 │
│  └─ router.push(url)    ──► Next.js navigation                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  CellsiteDetailPage renders                                                 │
│  ├─ useGetCellDetails(id)    ──► Fetches cell data                         │
│  └─ useEffect()    ──► Detects editStatus=true query param                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  setEditingStatus(true)                                                     │
│  └─ Re-renders with status dropdown visible                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    2. USER SELECTS NEW STATUS                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  handleStatusChange(newStatus)                                              │
│  └─ Calls updateStatusMutation.mutateAsync()                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  useUpdateCellStatus Hook (React Query)                                     │
│  ├─ Calls editCellStatusRequest handler                                    │
│  └─ Sends PATCH request to backend                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  PATCH /api/cells/:id/status                                                │
│  ├─ Backend validates status value                                          │
│  ├─ Updates database record                                                 │
│  └─ Returns updated cell object                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  Response received in frontend                                              │
│  ├─ React Query updates cache                                               │
│  └─ handleStatusChange continues                                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  toast.success("Status updated to [newStatus]")                            │
│  ├─ Shows success notification                                              │
│  └─ setEditingStatus(false)    ──► Closes dropdown                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  setTimeout(() => window.location.reload(), 500)                           │
│  └─ Reloads page after 500ms delay                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      3. PAGE RELOADS & DISPLAYS UPDATE                      │
└─────────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════

                            COMPONENT HIERARCHY

═══════════════════════════════════════════════════════════════════════════════

CellsitesManagementPage
├── CellsitesTable
│   ├── CellCard (map)
│   │   ├── Checkbox
│   │   ├── Header
│   │   │   ├── Cell Name
│   │   │   ├── Status Badge ◄── CLICKABLE
│   │   │   │   └── onClick={handleStatusClick}
│   │   │   └── Actions Dropdown
│   │   ├── Provider Info
│   │   ├── Technical Details
│   │   └── Footer Actions
│   │
│   └── Pagination Controls

CellsiteDetailPage
├── Header Section
│   ├── Back Button
│   ├── Action Buttons
│   │   ├── Edit Button (triggers isEditing)
│   │   └── Delete Button
│   │
│   ├── Cell Icon
│   └── Title and Badges
│       ├── Site ID
│       ├── Cell Name (editable in edit mode)
│       ├── Verified Badge
│       ├── Status Badge ◄── EDITABLE
│       │   ├── View Mode: Static display
│       │   ├── Status Edit: Dropdown + Cancel
│       │   └── Full Edit: Dropdown inline
│       │
│       └── Provider Info
│
├── Details Section
│   ├── Technical Details Card
│   ├── Location Card
│   └── Tower Details Card
│
└── Map Section
    └── Cell Location Map

═══════════════════════════════════════════════════════════════════════════════

                             STATUS BADGE STATES

═══════════════════════════════════════════════════════════════════════════════

┌────────────────────────────────────────────────────────────────────────────┐
│  STATE: View Mode (Default)                                                │
├────────────────────────────────────────────────────────────────────────────┤
│  Conditions:                                                                │
│  • isEditing = false                                                        │
│  • editingStatus = false                                                    │
│                                                                             │
│  Appearance:                                                                │
│  ╭──────────────────────────╮                                              │
│  │ ⚠️  Unknown               │  ◄── Static badge                           │
│  ╰──────────────────────────╯                                              │
│                                                                             │
│  Interaction: None (read-only)                                             │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│  STATE: Status Edit Mode                                                   │
├────────────────────────────────────────────────────────────────────────────┤
│  Conditions:                                                                │
│  • editingStatus = true                                                     │
│  • Triggered by ?editStatus=true query param                               │
│                                                                             │
│  Appearance:                                                                │
│  ╭──────────────╮  ╭───╮                                                   │
│  │ Unknown    ▾ │  │ X │  ◄── Dropdown + Cancel button                    │
│  ╰──────────────╯  ╰───╯                                                   │
│        │                                                                    │
│        ▼ (opens)                                                            │
│  ╭──────────────╮                                                           │
│  │ Unknown      │                                                           │
│  │ Pending      │  ◄── Select to update                                    │
│  │ Licensed     │                                                           │
│  ╰──────────────╯                                                           │
│                                                                             │
│  Interaction:                                                               │
│  • Select option → Calls handleStatusChange()                              │
│  • Click X → setEditingStatus(false)                                       │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│  STATE: Full Edit Mode                                                     │
├────────────────────────────────────────────────────────────────────────────┤
│  Conditions:                                                                │
│  • isEditing = true                                                         │
│  • Triggered by Edit button or ?edit=true query param                      │
│                                                                             │
│  Appearance:                                                                │
│  ╭────────────────────────────────╮                                        │
│  │ Unknown                      ▾ │  ◄── Dropdown inline with form        │
│  ╰────────────────────────────────╯                                        │
│                                                                             │
│  Interaction:                                                               │
│  • Select option → Updates editForm.status                                 │
│  • Click Save → Calls handleSave() (updates all fields)                   │
│  • Click Cancel → setIsEditing(false)                                      │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│  STATE: Clickable on Card                                                  │
├────────────────────────────────────────────────────────────────────────────┤
│  Location: CellsitesTable (card in list view)                              │
│                                                                             │
│  Appearance:                                                                │
│  ╭──────────╮                                                               │
│  │ Unknown  │  ◄── cursor-pointer, hover:opacity-80                        │
│  ╰──────────╯                                                               │
│                                                                             │
│  Interaction:                                                               │
│  • Click → handleStatusClick()                                             │
│  • Navigates to /cells/:id?editStatus=true                                 │
└────────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════
                           ARCHITECTURE COMPLETE
═══════════════════════════════════════════════════════════════════════════════
