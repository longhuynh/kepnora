# Kepnora UI Documentation

**Canonical path:** `docs/ui/README.md`  
**Version:** 1.0-draft  
**Date:** 2026-10-29  
**Scope:** Phase 1 mobile MVP, with future phases explicitly separated  
**Audience:** Product design, mobile engineering, backend engineering, QA, AI coding agents

> This is the entry point for Kepnora UI work. It organizes existing design specifications and defines how approved UI decisions become React Native + Expo screens. It is not a substitute for detailed screen specifications, API contracts or security controls.

---

## 1. Product design mission

Kepnora helps people organize physical assets and their maintenance in one workspace. The mobile experience should feel trustworthy, clear and low-effort: users should be able to see what needs attention, find an asset, record maintenance, add an actual odometer reading and retrieve a receipt without learning a complex system.

**Primary design goals**

1. Show the next useful action quickly.
2. Keep data entry short and explain optional fields.
3. Make maintenance status understandable without inventing predictions.
4. Support assets with no associated property.
5. Never show data from another workspace after a workspace switch.
6. Make network and authorization failures recoverable and understandable.
7. Be accessible on both iOS and Android.

## 2. Documentation map

| Path | Responsibility | Status |
|---|---|---|
| `docs/ui/README.md` | UI entry point, ownership and handoff | This document |
| `docs/ui/design-system/tokens.md` | Canonical numeric tokens and semantic roles | Target canonical location; reconcile with existing design-system document |
| `docs/ui/design-system.md` | Existing comprehensive design guidance | Existing companion document; avoid duplicating contradictory tokens |
| `docs/ui/screen-specifications.md` | Phase 1 screen-level interactions and acceptance criteria | Existing companion document |
| `docs/ui/mockups/README.md` | Mockup naming, states, review and handoff | Planned |
| `docs/implementation/mobile-project-structure.md` | React Native architecture and ownership boundaries | Existing |
| `docs/api/api-contracts-master.md` | Proposed HTTP/DTO contract, pending OpenAPI verification | Existing |
| `docs/features/*.md` | Feature-specific behavior and edge cases | Existing or in progress |
| `docs/architecture/tenant-isolation.md` | Tenant security rules | Existing |
| `docs/testing/test-strategy.md` | Test levels and quality gates | Existing |

**Source-of-truth rule:** Approved numeric design tokens belong in `design-system/tokens.md`; screen behavior belongs in `screen-specifications.md`; backend data and authorization belong in API and architecture specifications. If documents conflict, record the discrepancy and resolve it before implementation. Do not silently choose whichever version is convenient.

## 3. MVP versus future UI

| Phase | Design / implementation boundary |
|---|---|
| Phase 1 | Authentication, workspace selection, dashboard, properties, asset types, assets, maintenance, manually entered mileage, documents, notifications, search, settings |
| Phase 2 | Find Services and provider discovery; design separately and do not present as functioning MVP features |
| Phase 3 | Booking, scheduling with providers and payments; do not show successful booking flows in Phase 1 |

A placeholder may be shown only if product approves its copy and it cannot be mistaken for a working capability. Prefer hiding unavailable destinations.

## 4. Primary navigation

Recommended bottom tabs:

- **Home:** dashboard, attention items and recent activity.
- **Assets:** inventory, filtering, add and detail.
- **Maintenance:** upcoming/due/overdue presentation, history and completion.
- **Search:** scoped search across permitted MVP entities.
- **Settings:** profile, workspace, members, preferences and app settings.

Properties, documents, notifications and workspace switching are contextual destinations, not additional bottom tabs. Tab selection must not change the active workspace.

### Proposed Expo Router skeleton

```text
apps/mobile/app/
  _layout.tsx
  (public)/
    welcome.tsx
    sign-in.tsx
  (onboarding)/
    workspace-create.tsx
  (app)/
    _layout.tsx
    (tabs)/
      index.tsx
      assets/index.tsx
      maintenance/index.tsx
      search.tsx
      settings.tsx
    workspaces/switch.tsx
    properties/index.tsx
    properties/new.tsx
    properties/[propertyId].tsx
    assets/new.tsx
    assets/[assetId]/index.tsx
    assets/[assetId]/edit.tsx
    maintenance/[occurrenceId].tsx
    maintenance/[occurrenceId]/complete.tsx
    documents/[documentId].tsx
    notifications/index.tsx
```

These are proposed paths, not verified files. Route wrappers should remain thin; feature screens and hooks live in `src/features`.

## 5. Core user journeys

### 5.1 First-time user

1. Welcome and sign in using approved OIDC authorization code + PKCE flow.
2. Recover session securely and fetch available workspaces.
3. If no membership exists, offer workspace creation.
4. If one workspace exists, enter it; if several, select one.
5. Show Home with clear actions to add an asset or property.
6. Do **not** force property creation: cars, electronics and other assets may be standalone.

The ABP global identity and multi-tenant membership model must pass the M0 proof of concept before treating this flow as implemented.

### 5.2 Add an asset

1. Start from Home or Assets.
2. Select global or workspace-specific asset type and published schema version.
3. Enter common fields and schema-driven custom fields.
4. Optionally select an active property, or choose **No property**.
5. Submit; validate server response and show asset detail.
6. Optionally add maintenance rules or private documents.

Never interpret schema version updates as automatic rewrites of older asset custom fields.

### 5.3 Complete maintenance

1. Open a maintenance occurrence from Home, Maintenance or asset detail.
2. Show asset, rule, due context and persisted lifecycle status.
3. Collect completion date, notes and optional supporting document as supported by contract.
4. Submit once; handle retry, conflict and already-completed states.
5. Refresh occurrence, history and dashboard data.

Persisted occurrence statuses are **Open, Completed, Skipped, Cancelled**. **Upcoming, Due and Overdue** are derived presentation states. Never persist them as lifecycle statuses.

### 5.4 Record mileage

1. Open a vehicle/eligible asset.
2. Choose Add Odometer Reading.
3. Enter actual reading, observed date/time and optional notes.
4. Validate monotonicity or corrections according to the approved odometer contract.
5. Recompute maintenance eligibility from **recorded actual readings only**.

Do not predict mileage or imply the user has driven a certain distance without a reading.

### 5.5 Upload a document

1. Choose a supported document owner (asset or maintenance record as defined by the database).
2. Select photo/file; display supported file types and limits.
3. Request upload intent from authenticated API.
4. Upload directly to private S3 using short-lived presigned URL.
5. Finalize upload through API and refresh document list.
6. Provide explicit retry and cleanup behavior for interrupted uploads.

Never expose raw S3 keys, presigned URLs or tokens in analytics or UI logs.

## 6. Screen inventory

| Area | Screens / states | Primary specification |
|---|---|---|
| Public | Welcome, sign in, auth callback/error | Authentication and onboarding |
| Workspace | Create, select, switch, empty membership | Authentication and settings |
| Home | Overview, attention cards, recent activity, empty | Dashboard and search |
| Properties | List, detail, create/edit, archive confirmation | Property management |
| Asset Types | Select type, dynamic field rendering, errors | Custom asset types |
| Assets | List, filters, create/edit, detail, archived view if approved | Asset management |
| Maintenance | List, occurrence detail, complete, skip/cancel where allowed, history | Maintenance management |
| Mileage | Actual reading form, history and correction state | Maintenance/asset specifications |
| Documents | List, preview, upload, upload failure, deletion confirmation | Documents and warranties |
| Notifications | Inbox, permissions prompt, preferences, deep link | Notifications and reminders |
| Search | Query, results, no results, offline | Dashboard and search |
| Settings | Profile, workspace, members, permissions, sign out | Settings and members |

Every screen requires loading, empty, success, validation error, authorization error, server error and offline behavior where applicable. Screens that display sensitive data must also handle a workspace switch while a request is pending.

## 7. Visual direction

The intended experience is restrained and premium: warm off-white surfaces, readable dark text, deliberate spacing, clear hierarchy, subtle borders and limited shadows. Visual references may evoke the simplicity of high-quality consumer apps, but do not reproduce another product's branded interface.

Prefer informative asset photography when provided by the user. Generic decorative imagery must never displace maintenance information. Color alone must not convey due/overdue or error states.

### Token categories

The canonical token file must define:

- Color roles: canvas, surface, elevated surface, primary/secondary text, border, accent, success, warning, danger, disabled and focus.
- Typography: families, weight mapping, display/title/body/caption scales and line heights.
- Spacing: base unit, component padding, screen gutters and section rhythm.
- Shape: corner radii, borders, shadows and touch targets.
- Motion: durations, reduced-motion fallbacks and skeleton behavior.
- Semantic states: loading, empty, active, archived, completed, due and overdue.

**Do not invent numeric token values here.** Reconcile the existing `docs/ui/design-system.md` and move the approved numbers into `docs/ui/design-system/tokens.md`.

## 8. Component hierarchy

```text
src/components/
  primitives/       # Button, Text, Icon, TextField, Card, Divider
  feedback/         # InlineError, Banner, Toast, EmptyState, Skeleton
  navigation/       # ScreenHeader, Tabs, WorkspaceSwitcher
  forms/            # FormField, Select, DateField, DynamicField
  overlays/         # ConfirmDialog, BottomSheet
src/features/
  assets/components/        # AssetCard, AssetTypePicker
  maintenance/components/   # OccurrenceCard, StatusPill
  properties/components/    # PropertyCard, PropertyPicker
  documents/components/     # UploadProgress, DocumentCard
```

Primitives are generic; domain components belong to features. Route files should not contain API calls or recurrence logic. Avoid a global shared component for a one-off screen until a genuine reuse case exists.

## 9. State ownership and data flow

| State | Owner | Rule |
|---|---|---|
| API entities | TanStack Query | Keys always include workspace ID for tenant data |
| Auth tokens | SecureStore + auth/session provider | Never persist secrets in AsyncStorage |
| Active workspace | Auth/workspace coordinator | Must be checked server-side on every request |
| Temporary UI filters/modals | Local state or Zustand | Never treat as authoritative backend state |
| Form input | React Hook Form + Zod | Server remains final validator |
| Dynamic asset type schemas | Generated API DTOs and versioned schema renderer | Use published immutable schema version |
| Maintenance eligibility | Backend scheduling engine | Mobile displays derived state only |

Use OpenAPI generated TypeScript types from the configured running backend. Example paths in docs are proposals until verified.

## 10. Workspace isolation UX

A workspace switch is a security-sensitive transition, not merely changing a header label.

1. Disable or guard tenant-scoped interactions during transition.
2. Cancel pending queries and mutations where safe.
3. Partition or remove prior workspace query cache and feature-local state.
4. Set the new workspace only after verifying active membership.
5. Reset navigation to a safe route in the new workspace.
6. Reject stale responses or deep links that target the old workspace.
7. Fetch current workspace data with tenant-aware keys.

The server must validate membership and permissions independently. `ICurrentTenant` is scoping, **not authorization**.

## 11. Maintenance display language

| Backend persisted lifecycle | UI interpretation |
|---|---|
| `Open` | Show derived Upcoming, Due or Overdue based on engine rules and current actual readings |
| `Completed` | Completed; include actual completion information |
| `Skipped` | Skipped; show only if allowed by policy |
| `Cancelled` | Cancelled; do not suggest completion is still required |

Due badges should include accessible text, not only color. If mileage is missing, show **Mileage reading needed** or equivalent; do not manufacture a due estimate. If time-zone information is incomplete, display a consistent documented fallback rather than silently changing dates.

## 12. Forms and validation

Use shared field patterns for label, optional marker, help text, input, inline error and character limits. Prefer native keyboard types, accessible focus order and autofill where safe.

- Validate required fields and lengths locally for fast feedback.
- Map API validation errors back to fields by stable field paths.
- Keep user-entered values after network errors.
- Prevent duplicate taps while submitting.
- Explain 409 conflict and allow refresh/retry rather than silent overwrite.
- For schema-driven asset fields, render based on the asset's assigned immutable schema version.
- Distinguish omitted, `null` and empty string according to API contract.

## 13. Error, empty and offline design

| Condition | Required UX |
|---|---|
| First load | Skeleton/progress with accessible status |
| Empty workspace | Invite user to add an asset or property |
| No filtered results | Clear filters action |
| No internet | Offline banner; read-only cached content only when policy allows |
| Unauthorized 401 | Safe session refresh/sign-in recovery |
| Forbidden 403 | Permission message without leaking resource details |
| Not found 404 | Neutral missing/unavailable screen |
| Conflict 409/412 | Refresh and reconcile changes |
| Validation 422 | Field-level errors and summary |
| Rate limit 429 | Retry guidance without aggressive loops |
| Server 5xx | Retry action, preserve draft, correlation ID if exposed |

Never claim a mutation succeeded based only on a tap or a local optimistic state when backend confirmation is required.

## 14. Accessibility requirements

- Support dynamic font scaling and screen-reader labels.
- Ensure touch targets follow platform accessibility guidance.
- Maintain contrast for text, icons and status indicators.
- Provide accessible names for icon-only actions.
- Announce async success, validation failures and important status changes.
- Respect reduced-motion settings.
- Avoid interactions dependent only on swipe gestures.
- Verify forms with keyboard and VoiceOver/TalkBack.
- Keep critical actions reachable on small devices and with large text.

## 15. Platform behavior

Design for iOS and Android without assuming identical native controls. Validate date/time pickers, permission prompts, safe areas, keyboard avoidance, file pickers, camera/photos permissions, push behavior and deep-link restoration on real devices.

Use Expo/EAS configurations compatible with the pinned SDK. Never put API secrets or private credentials in `EXPO_PUBLIC_*` variables. Device notification permissions are user-controlled and may be denied; the app must still function.

## 16. Notifications and deep links

Notifications must describe only authorized, current maintenance context. Tapping a notification must validate session, active workspace membership and resource permission before opening detail. Handle revoked membership, deleted/archived resource, completed maintenance and expired notification gracefully.

Do not promise push delivery. Device registration, preference checks, quiet hours, deduplication and outbox processing are backend concerns specified in the notification engine document.

## 17. Privacy and trust

Addresses, asset details, receipts and documents are private workspace data. Avoid logging their full contents in analytics, crash reports or support screenshots. Do not expose S3 presigned URLs in debug views. Explicitly confirm destructive actions and explain the effect of archive versus delete.

Do not display cross-tenant cached data, even momentarily, during loading transitions.

## 18. UI mockup workflow

Each mockup should be traceable to a named screen and state. The future `docs/ui/mockups/README.md` will define filename conventions, revision rules, image sources and approval workflow.

Recommended state coverage for every key screen:

1. Default populated.
2. Empty or first use.
3. Loading.
4. Validation error.
5. Network/offline.
6. Permission denied.
7. Long text and large font.
8. Dark mode if approved as a release requirement.

Do not mark an image mockup as an implemented or interactive feature.

## 19. Design-to-code handoff

A screen is ready for implementation only when the following are linked:

- Approved mockup or annotated wireframe.
- Screen identifier and navigation entry/exit.
- Required permissions and active-workspace rules.
- API endpoints and generated DTOs.
- Form validation and mutation semantics.
- Loading, empty, error, offline and conflict states.
- Accessibility expectations.
- Acceptance tests.

The feature spec owns domain behavior; the screen spec owns user interaction; the design system owns visual values; the API contract owns payloads. Record deviations as decisions rather than embedding hidden assumptions in code.

## 20. Design review checklist

- [ ] MVP-only navigation is accurate.
- [ ] No property is required for standalone assets.
- [ ] Current workspace is always identifiable.
- [ ] No stale data appears during workspace switches.
- [ ] Maintenance statuses distinguish persisted lifecycle from derived display.
- [ ] Mileage is based on manually entered actual readings only.
- [ ] Document uploads use private S3 intent/finalize workflow.
- [ ] Error, empty, offline and permission states exist.
- [ ] Accessibility and large-text layouts are reviewed.
- [ ] Copy does not promise Phase 2/3 capabilities.
- [ ] Screens reference verified API types or mark proposed contracts.
- [ ] Numeric design tokens match canonical token file.

## 21. Quality gates and tests

UI test coverage should include component behavior, form validation, query invalidation, tenant-switch race conditions, deep links, document upload failure, permission changes and screen-reader smoke checks. Test with realistic long asset names, missing images, absent property assignments, time-zone boundaries and large inventories.

Use Jest/React Native Testing Library (or the compatible pinned test runner) for unit/component tests and choose a single device E2E runner after M0 compatibility validation. Contract tests should compare generated client behavior against the running backend OpenAPI.

## 22. Milestone alignment

| Milestone | UI deliverables |
|---|---|
| M0 | Expo SDK/dependency proof of concept, auth deep-link POC, design token baseline |
| M1 | Sign-in, session recovery, workspace create/select/switch |
| M2 | Properties and asset type selection/dynamic form groundwork |
| M3 | Asset list/create/edit/detail, optional property picker |
| M4 | Maintenance list/detail/complete and actual mileage entry |
| M5 | Document picker, upload progress, preview and error states |
| M6 | Notification permissions, preferences, inbox/deep-link handling |
| M7 | Dashboard, search, workspace settings and member UI |
| M8 | Accessibility, device E2E, performance, offline and beta polish |

Do not treat a screen as complete until API integration and security tests pass.

## 23. Open decisions

1. Which existing design-system values become the canonical token file?
2. Is dark mode required for Phase 1 or later?
3. Which screen states need high-fidelity mockups versus annotated wireframes?
4. Which localization languages are required at launch?
5. What is the exact behavior for archived properties and assets in pickers/search?
6. What is the approved timezone precedence for maintenance display?
7. Which documents may attach to which entities in the canonical database?
8. What is the launch analytics/privacy policy?
9. Which E2E runner is compatible with the pinned Expo SDK?
10. What is the accepted behavior when push permissions are denied?

Resolve these in the owning spec or ADR; avoid undocumented assumptions.

## 24. Definition of Done for the UI documentation set

- [ ] This README links to current design system and screen specs.
- [ ] Canonical `design-system/tokens.md` is reconciled with existing design-system guidance.
- [ ] `mockups/README.md` defines naming, review and approval.
- [ ] Every MVP screen has states, data contract, permissions and acceptance criteria.
- [ ] All navigation paths are checked against the Expo Router implementation.
- [ ] All DTOs are checked against generated OpenAPI.
- [ ] Phase 2/3 screens are clearly deferred.
- [ ] Security, accessibility and cross-tenant test cases are documented.

---

**Guiding principle:** The UI must make real asset and maintenance information easy to act on while preserving workspace boundaries, data integrity and user trust.
