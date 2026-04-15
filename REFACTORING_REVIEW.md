# Refactoring Review

## Current state

Repository was reviewed and partially refactored with focus on removing high-impact duplication, stabilizing build verification, and reducing frontend query/cache sprawl.

### Verification
- `dotnet build backend/SoftimProject.sln`: passed
- `dotnet test backend/SoftimProject.sln --no-build`: passed for `SoftimProject.Infrastructure.Tests` (4 tests)
- `npm run lint`: passes with 2 warnings and 0 errors

## Implemented refactors

### Backend
- Extracted shared project detail DTOs and EF projections into [ProjectDetailModels.cs](C:/Users/hnizd/source/repos/softim-cz/softimproject/backend/src/SoftimProject.Application/Features/Projects/ProjectDetailModels.cs).
- Extracted shared ticket detail DTOs and EF projections into [TicketDetailModels.cs](C:/Users/hnizd/source/repos/softim-cz/softimproject/backend/src/SoftimProject.Application/Features/Tickets/TicketDetailModels.cs).
- Simplified `GetProjectById` and `GetProjectByCode` handlers to use shared projection instead of duplicated `Include` + in-memory mapping.
- Simplified `GetTicketById` and `GetTicketByNumber` handlers to use shared projection instead of duplicated projection logic.
- Updated Web API controllers to consume the shared DTO namespaces cleanly.

### Frontend
- Added centralized query key factory and param normalization in [query-keys.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/query-keys.ts).
- Refactored core query modules to use shared query keys and consistent invalidation:
  - [projects.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/projects.ts)
  - [tickets.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/tickets.ts)
  - [kanban.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/kanban.ts)
  - [worklogs.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/worklogs.ts)
  - [comments.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/comments.ts)
  - [attachments.ts](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/queries/attachments.ts)
- Replaced `react-hook-form` `watch()` usage with `useWatch()` in [projects/page.tsx](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/app/(app)/projects/page.tsx), removing one real hook anti-pattern.
- Removed low-value lint noise from several files by deleting unused imports, props, and dead store parameters.

## Remaining anti-patterns and design debt

### 1. Monolithic pages
- [settings/page.tsx](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/app/(app)/projects/[code]/settings/page.tsx)
  - Very large multi-responsibility page.
  - Contains `react-hooks/set-state-in-effect` disable at file level.
  - Mixes project settings, GitHub integration, custom fields, members, and board config in one component tree.
- [admin/lookups/page.tsx](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/app/(app)/admin/lookups/page.tsx)
  - 1000+ line page with repeated inline CRUD table patterns.
  - High duplication in add/edit/cancel/save row state management.
- [tasks/page.tsx](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/app/(app)/projects/[code]/tasks/page.tsx)
  - Large stateful page mixing filtering, persistence, table config, export behavior, and sidebar interaction.

### 2. Repeated form/table state logic
- Lookup/admin UI repeats the same local state machine across multiple tables.
- Recommendation:
  - Extract `useInlineCrudState<T>()` hook.
  - Extract reusable editable row actions component.
  - Move field definitions into config objects instead of hand-writing each table body.

### 3. Residual frontend performance/UX issues
- [settings/page.tsx](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/app/(app)/projects/[code]/settings/page.tsx) still uses raw `<img>` for avatars.
- [tasks/page.tsx](C:/Users/hnizd/source/repos/softim-cz/softimproject/frontend/src/app/(app)/projects/[code]/tasks/page.tsx) still triggers the TanStack Table React Compiler incompatibility warning.
- The settings experience is functionally dense but visually heavy; sections should be split into smaller route-level tabs or lazy panels.
- Admin lookups lacks stronger hierarchy and bulk-edit affordances; current UI makes high-volume maintenance slow.

### 4. Sparse automated test coverage
- Domain/Application/WebApi test projects still do not discover runnable tests.
- Current automated coverage is concentrated in infrastructure tests only.
- Recommendation:
  - Add handler-level tests for core project/ticket/worklog flows.
  - Add Web API authorization/integration tests.

### 5. Backend query consistency
- Some application queries already use `AsNoTracking` and projection well, but the approach is still inconsistent across the repository.
- Recommendation:
  - Prefer DTO projection over `Include` for read paths.
  - Reserve `Include` for true aggregate mutation workflows.

## UX/UI improvement proposals

### High priority
- Split project settings into route segments or tab panels with isolated data loading:
  - General
  - Members
  - Custom fields
  - GitHub
  - Board
- Replace repetitive admin tables with a common editable-grid component.
- Add optimistic UI or inline status feedback for frequent admin actions.
- Add sticky action bars for long forms in settings/admin pages.

### Medium priority
- Add empty-state guidance and inline help where configuration has prerequisites.
- Unify avatar rendering and fallback behavior in one shared component.
- Review typography and spacing density on admin/configuration screens; current layouts prioritize raw data density over scanability.

### Low priority
- Review image handling and remote image strategy before converting avatar `<img>` to `next/image`.
- Revisit TanStack Table integration if React Compiler adoption becomes a stronger constraint.

## Recommended next implementation phases

1. Extract `ProjectSettings` into subcomponents or nested routes.
2. Introduce shared admin lookup CRUD primitives and reduce page duplication.
3. Add discoverable tests to Domain/Application/WebApi projects.
4. Continue replacing read-side `Include` usage with projections across backend query handlers.
5. Add a lightweight frontend architecture note describing query key policy, page composition rules, and preferred state patterns.
