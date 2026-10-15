## Technology Stack
- [Next.js](https://nextjs.org) — React-based framework for building web applications
- [React](https://react.dev) — UI library for building interactive interfaces
- [Tailwind CSS](https://tailwindcss.com) — Utility-first CSS framework
- [ShadCN UI](https://ui.shadcn.com) — Component library for React
- [TanStack Table](https://tanstack.com/table/latest) — Advanced table/grid for React
- [TanStack Query](https://tanstack.com/query/latest) — Data fetching, caching, and deduplication for React
- [Clerk](https://clerk.com) — Authentication and user management
- **C# (.NET 8) Backend Integration** — The application consumes backend APIs written in C# (.NET 8)
# Copilot Instructions for ntc_qos-fe

## Project Purpose & Architecture
- This project is a Next.js, TypeScript, Tailwind, etc. repo for the NTC QoS & Broadband Mapping Website.
- The app provides broadband coverage, service quality, and infrastructure data for both public users and administrators, supporting national and regional admin roles.
- Feature modules are under `src/components/features/`, with shared UI in `src/components/ui/` and utilities in `src/lib/` and `src/utils/`.
- Data flows from API handlers in `src/components/features/*/handlers/` to React Query hooks, then to forms/dialogs and tables. All API calls expect a live backend (C# .NET 8).
- The stack is designed for extensibility and maintainability, following SOLID principles:
  - **Single Responsibility:** Each feature, form, dialog, and handler is in its own file/folder.
  - **Open/Closed:** Add new features by creating new modules, not modifying core logic.
  - **Liskov Substitution:** Shared UI and hooks are reusable and composable.
  - **Interface Segregation:** Types are feature-specific and kept in `types/`.
  - **Dependency Inversion:** API integration is abstracted via handler templates and hooks.

## Key Patterns & Conventions
- **Dialog/Modal Pattern:** All create/edit dialogs are in `src/components/features/*/dialogs/` and use a `ResponsiveDialog` wrapper. Forms are in `forms/` and receive `onCancel`/`onClose` props to control dialog state.
- **Table Pattern:** Tables are in `tables/`, columns are defined in `columns.tsx`, and row actions are in `actions.tsx`. Edit actions open dialogs by passing state from the parent table.
- **API Integration:** API requests are defined in `handlers/` using a `MutationBlueprintFromTemplate` or `QueryBlueprintFromTemplate` pattern. Use `createMutationHookFromTemplate` and `createQueryHookFromTemplate` for React Query integration.
- **Type Safety:** Types for each feature are in `types/`. API payloads and responses are strongly typed and should match backend schemas.
- **UI Consistency:** Use the custom UI components in `src/components/ui/` for all forms, buttons, and inputs. Tailwind utility classes are used for layout and spacing.
- **Dropdowns:** For foreign key relationships (e.g., selecting a Generation in Technology forms), fetch options with a query hook and render a `<select>` with a chevron-down icon for consistency.

## Developer Workflows
- **Install dependencies:** `bun install`
- **Run dev server:** `bun dev`
- **Build:** `bun run build`
- **Lint/Format:** `bun run lint` and `bun run format:check` / `bun run format:write`
- **Test:** (No test scripts found; add if needed)
- **API Mocking:** Not present; all API calls expect a live backend.

## Integration Points
- **Backend:** Expects REST endpoints matching the handlers' payloads and response types. Backend is C# (.NET 8).
- **React Query:** Used for all data fetching/mutations; always invalidate relevant queries after mutations.
- **Lucide Icons:** Used for all iconography (e.g., edit, chevron-down).

## Examples
- See `src/components/features/generation/forms/CreateGenerationForm.tsx` for form conventions.
- See `src/components/features/technology/forms/CreateTechnologyForm.tsx` for dropdown and API integration patterns.
- See `src/components/features/generation/tables/GenerationsTable/table.tsx` for dialog state management and table patterns.

## Best Practices & Special Notes
- Do not render objects directly in JSX; always access a property (e.g., `row.original.generation?.name`).
- All dialog open/close state should be managed in the parent component, not in the form/dialog itself.
- Keep payloads and types in sync with backend schemas; update types in `types/` as backend evolves.
- Strictly follow SOLID and DRY principles when generating code for maintainability and extensibility.
- Reference the README for business context, user roles, and feature descriptions.

---


## UI Generation Requirement
- All generated UI must be modern, professional looking, and adhere to established design patterns and the current system architecture. Always use the project's custom UI components and follow the layout, spacing, and interaction conventions demonstrated in the codebase.

## Change Log Requirement
- For each response or code change, always add a clear change log summarizing what was changed and why. This helps maintain traceability and improves team communication.
