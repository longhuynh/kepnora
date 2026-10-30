# Kepnora UI Mockups — Repository Guide

**Canonical path:** `docs/ui/mockups/README.md`  
**Version:** 1.0-draft  
**Date:** 2026-10-29  
**Scope:** Phase 1 React Native + Expo mobile MVP  
**Audience:** Product designer, mobile/backend engineers, QA, AI coding agents

> This guide defines how mockups are named, reviewed, approved and translated into implementation. It does **not** claim that any visual mockups or Figma files have already been created. Design assets become authoritative only when explicitly approved and linked from this directory.

---

## 1. Purpose

A mockup is a visual communication artifact, not an API contract or database specification. Use it to validate information hierarchy, interaction affordances, layout and state transitions before implementing a screen. Each mockup must be traceable to a user story, a screen specification and a testable implementation outcome.

## 2. Source-of-truth hierarchy

1. Approved product scope and feature specifications define behavior.
2. Architecture and API contracts define security, state and data semantics.
3. `docs/ui/design-system/tokens.md` defines approved tokens after reconciliation.
4. `docs/ui/screen-specifications.md` defines screen behavior and states.
5. Approved mockups define visual layout and interaction details that do not conflict with items 1–4.
6. Mobile code implements the approved combination.

If a mockup conflicts with backend behavior, **do not silently implement the mockup**. Record the conflict, request a decision and update affected specifications.

## 3. Canonical directory structure

```text
docs/ui/mockups/
├── README.md
├── manifest.md                   # Planned; inventory of approved designs
├── flows/                        # Planned; journey maps
│   ├── onboarding/
│   ├── assets/
│   ├── maintenance/
│   └── documents/
├── screens/                      # Planned; per-screen images or design exports
│   ├── auth/
│   ├── workspace/
│   ├── dashboard/
│   ├── properties/
│   ├── assets/
│   ├── maintenance/
│   ├── documents/
│   ├── search/
│   └── settings/
├── components/                   # Planned; reusable component variants
├── prototypes/                   # Planned; interaction recordings or links
└── archive/                      # Planned; superseded designs
```

Only `README.md` is required by this delivery. The other paths are proposed organization, not evidence of existing files.

## 4. File naming convention

Use lowercase kebab-case filenames with explicit screen, state, platform and version:

```text
{feature}--{screen}--{state}--{platform}--v{major}.{minor}.{ext}
```

Examples:

```text
assets--asset-detail--loaded--ios--v1.0.png
assets--asset-detail--empty--android--v1.0.png
maintenance--occurrence-detail--overdue--ios--v1.0.png
workspace--switcher--loading--shared--v1.0.png
```

Supported platform labels: `ios`, `android`, `shared`. A shared export is acceptable only when the visual and behavior are platform-independent. Never use filenames such as `final-final2.png`.

## 5. Mockup lifecycle

| Status | Meaning | May drive implementation? |
|---|---|---|
| `idea` | Exploratory concept | No |
| `draft` | Work in progress | No |
| `in-review` | Submitted for review | No |
| `approved` | Explicitly accepted | Yes, subject to spec consistency |
| `implemented` | Matching implementation merged and tested | Yes |
| `superseded` | Replaced by a later design | No |

A mockup does not become approved merely because it exists in the repository. Approval should be captured in the manifest or review record with date and reviewer.

## 6. Manifest record

Create `manifest.md` when first mockups are added. Each entry should include:

```yaml
id: KEP-UI-ASSET-DETAIL-001
feature: assets
screen: asset-detail
state: loaded
platform: shared
version: 1.0
status: draft
file: screens/assets/assets--asset-detail--loaded--shared--v1.0.png
screen_spec: docs/ui/screen-specifications.md
feature_spec: docs/features/asset-management.md
api_contract: docs/api/api-contracts-master.md
reviewer: null
approved_at: null
implementation_route: null
notes: "Illustrative example; not an existing design."
```

This YAML is an example schema, not a claim that the file exists.

## 7. Minimum mockup package for each screen

Every screen package must identify the primary action, entry route, expected data, permission requirements, navigation exits, empty/loading/error states and relevant screen-reader labels. At least one approved default-state mockup is required before claiming a screen's visual design is final.

For complex flows, attach a sequence or prototype showing how users progress through the screens.

## 8. Required UI state matrix

| State | What the design must communicate |
|---|---|
| Initial loading | Data is being retrieved; no fake data |
| Loaded | Primary content and available actions |
| Empty | Nothing exists yet; clear next action |
| Filtered empty | No matches; clear filters option |
| Validation error | Field-specific actionable message |
| API error | Safe error and retry if appropriate |
| Offline | Stale data indicator or unavailable action |
| Permission denied | No misleading action affordance |
| Not found | Missing or inaccessible resource |
| Saving | Disable duplicate submit, preserve input |
| Save success | Confirmation and destination |
| Conflict | Refresh/reapply changes; no silent overwrite |
| Archived | Archived indicator and restricted actions |

Do not invent persisted states to satisfy a visual concept. In particular, `Upcoming`, `Due` and `Overdue` are **derived display states** for maintenance occurrences; persisted lifecycle values are `Open`, `Completed`, `Skipped`, `Cancelled`.

## 9. Phase 1 screen inventory

The following inventory is a design coverage target, not a statement that the mockups exist.

| Feature | Screens / overlays |
|---|---|
| Authentication | Welcome, sign in, callback/loading, auth failure |
| Workspace | First-workspace create, picker, switcher, invitation/membership state if supported |
| Dashboard | Summary, upcoming maintenance, empty dashboard |
| Properties | List, create/edit, detail, archive confirmation |
| Assets | List, create/edit, detail, optional property picker |
| Asset types | Type selector, dynamic fields, unsupported schema fallback |
| Maintenance | List, occurrence detail, complete/skip, rule editor, history |
| Odometer | Add actual reading, reading history |
| Documents | List, picker, upload progress, view/download, failure |
| Notifications | Preferences, permission explanation, deep-link destination |
| Search | Query, results, empty results |
| Settings | Profile, workspace members/permissions, workspace settings |

Features or flows not approved for Phase 1 must be visually marked as future, not represented as shipped MVP screens.

## 10. Navigation and route ownership

Expo Router route files are thin entry points. Mockup references must use a stable screen ID rather than depending on incidental route-group names such as `(tabs)`.

Navigation targets are confirmed in the mobile project structure and screen specifications. Deep links require authentication, active workspace membership and resource authorization before showing data.

## 11. Onboarding mockup rules

Show a clear route from authentication to workspace selection/creation and then to usable content. A property is optional; onboarding must not require one before the user can add a vehicle, electronic device or other standalone asset.

When a global account belongs to several workspaces, the picker should distinguish the active workspace without implying separate login credentials.

## 12. Workspace switcher rules

Every workspace-aware screen must visibly reflect the active workspace. Mockups should include switching, loading and stale-request scenarios. No workspace A asset, property, address or document may appear after switching to workspace B.

A mockup may show a workspace label, but authorization is enforced by the backend on every request, not by hiding buttons.

## 13. Property screen rules

Property selection on an asset is optional and must include `No property`. Property archive confirmation must explain that linked assets and maintenance history are preserved. Archived properties must be visually distinguishable from active choices.

Do not show automatic geocoding, a map pin or valuation as an MVP guarantee; those capabilities are not part of the canonical property model.

## 14. Asset type and dynamic form rules

Asset type selection is driven by published schema versions. A mockup may illustrate fields such as VIN or fuel type, but must not imply that all workspaces have the same custom fields.

Design for required/optional fields, field validation, select choices, unsupported schema versions and long labels. Never assume a custom-field migration occurs automatically when a new version is published.

## 15. Maintenance status rules

Use distinct presentation for lifecycle state and urgency:

- Lifecycle: `Open`, `Completed`, `Skipped`, `Cancelled`.
- Derived urgency: `Upcoming`, `Due`, `Overdue` when the occurrence remains open.

The server scheduling engine owns recurrence calculations. Mobile UI should render server-provided or contract-defined derived values; it should not independently invent new schedules.

## 16. Mileage rules

Mileage is manually entered in the MVP. Mockups must not show estimated mileage, projected odometer values, GPS-derived distance or predicted service dates as actual readings.

If the latest odometer reading is absent, show an explicit unknown state and a path to add a reading.

## 17. Documents and warranties rules

The UI may show receipts, manuals, photos and warranty documents as supported by the current document model. Upload progress must distinguish selection, upload, finalize and failure.

Do not show warranty expiration alerts as a committed MVP feature unless expiry-date storage and scheduling are approved in the database/API contract.

## 18. Notification rules

Notification mockups distinguish in-app status from Expo push delivery. A successful enqueue is not proof of device delivery. Permission denial should not block maintenance use.

Deep-link designs must handle revoked membership, deleted/archived resources and expired sessions without leaking data.

## 19. Visual design tokens

Use approved semantic color roles, typography, spacing, radii and elevation tokens. `docs/ui/design-system/tokens.md` is the target canonical token location, but reconcile any existing `docs/ui/design-system.md` values before treating them as final.

Avoid hardcoded pixel values in mockup annotations unless they map to a named token or are documented exceptions.

## 20. Responsive layouts

Prepare baseline phone layouts for compact and standard widths. Confirm landscape/tablet requirements separately; do not imply tablet optimization is complete simply because React Native can render on a tablet.

Support safe areas, keyboard avoidance, scrollable forms, text scaling and long translated strings.

## 21. Platform differences

Document intentional differences in iOS and Android navigation, permission prompts, system dialogs, file pickers, date pickers and back gestures. Prefer native platform behavior where it improves predictability.

Use `shared` mockups only when the differences do not affect implementation or acceptance tests.

## 22. Accessibility acceptance

Every interactive element requires an accessible label, adequate touch target and visible focus/pressed/disabled state. Error text should be associated with its field. Color must not be the sole indicator of due/overdue status.

Review dynamic type, screen reader traversal, contrast, reduced motion and keyboard behavior on real devices.

## 23. Copy and content guidelines

Use short, plain English labels. Explain irreversible or destructive actions precisely. Do not claim that archive deletes assets. Avoid technical error messages such as raw SQL constraint names.

Use neutral placeholders and fictional names/addresses in design exports. Never include real user access tokens, private files or production personal data in mockups.

## 24. Error and recovery patterns

Errors should identify what happened, whether the action was saved and what the user can do next. For ambiguous network failures after a create request, the UI should refresh before encouraging a duplicate retry.

A 401 prompts session recovery or sign-in; 403/404 should not disclose another workspace's resource existence; 409/412 indicates an edit conflict per API contract.

## 25. Loading and optimistic behavior

Skeletons and progress indicators should resemble final content without presenting fabricated values. Optimistic UI is allowed only where rollback and concurrency semantics are clear.

Avoid optimistic maintenance completion if it can cause an incorrect reminder state or obscure a failed write.

## 26. Offline design

Offline read-only cached views may be shown when the mobile data policy allows them and the cache is correctly partitioned by workspace. Offline writes are not a default Phase 1 capability.

Mockups must make it clear when displayed data may be stale. Do not imply that a locally changed maintenance occurrence is already saved to the server.

## 27. Empty state language

Differentiate: no data yet, no search results, no permissions, loading failed and resource missing. A workspace with no properties must still offer Add Asset.

Recommended example copy: “No assets yet. Add your first asset to track its maintenance.” This is draft UX copy and may be revised in design review.

## 28. Component variant inventory

Document visual variants for buttons, text fields, select/picker, cards, status chips, list rows, tabs, dialogs, toasts, banners, progress indicators, file rows, date input and dynamic custom-field controls.

For each component, capture default, pressed, disabled, loading, error and accessibility behavior where relevant.

## 29. Review checklist

- [ ] Screen ID and feature specification linked.
- [ ] Primary action and navigation exits are clear.
- [ ] Active workspace is visible where relevant.
- [ ] Property remains optional for assets.
- [ ] Maintenance lifecycle and urgency are not conflated.
- [ ] Mileage is actual/manual, never projected.
- [ ] Permission-denied and stale-session states are addressed.
- [ ] Empty, loading, error and offline states are covered.
- [ ] Dynamic type and screen-reader behavior are annotated.
- [ ] Approved tokens are used.
- [ ] API/backend limitations are not contradicted.
- [ ] Reviewer, date and version recorded.

## 30. Design review participants

Product owner confirms scope and copy. Mobile engineer confirms implementation feasibility. Backend engineer confirms API/authorization semantics. QA confirms testable states. Accessibility review is required for reusable components and critical flows.

A single reviewer may fulfill multiple roles in a small team, but the checklist still applies.

## 31. Approval record template

```markdown
### KEP-UI-MAINTENANCE-DETAIL-001
- Version: 1.0
- Status: in-review
- Reviewer: TBD
- Review date: TBD
- Decision: Pending
- Linked feature spec: docs/features/maintenance-management.md
- Linked screen spec: docs/ui/screen-specifications.md
- Open issues: None recorded yet
```

Do not prefill approvals for unreviewed designs.

## 32. Change management

Minor version increments cover non-breaking layout/copy refinements. Major version increments cover significant flow, interaction or data-model changes. Preserve superseded exports in `archive/` or through version control; update the manifest to identify the current approved version.

Behavior changes must update the feature specification, screen specification, API contract or ADR as applicable—not just the image.

## 33. Handoff to React Native

Each approved screen handoff should include:

1. Screen ID, design link/export and approved version.
2. Expo Router route and feature screen owner.
3. Component inventory and token references.
4. API query/mutation mapping and generated DTO names.
5. Navigation entry/exit rules and deep-link behavior.
6. Validation, errors, permissions and offline states.
7. Accessibility annotations.
8. Acceptance tests and screenshots to compare.

Do not implement business rules directly inside presentation-only components.

## 34. Design-to-code mapping example

```text
Design ID: KEP-UI-ASSET-DETAIL-001
Expo Router: app/(app)/assets/[assetId].tsx (proposed)
Feature screen: src/features/assets/screens/AssetDetailScreen.tsx
Data source: generated OpenAPI client via TanStack Query
Query key: ['workspace', workspaceId, 'asset', assetId]
Permissions: active membership + asset read
UI states: loading, loaded, error, not-found, offline, archived
```

Route names and API details remain proposed until verified against the running implementation.

## 35. Screenshot and visual regression policy

Compare approved mockups against device screenshots at defined viewport sizes and font scales. Record intentional native differences. Automated screenshot tests may supplement, but cannot replace functional and accessibility testing.

Do not fail a release solely for harmless pixel differences caused by platform fonts; evaluate severity and user impact.

## 36. QA scenarios for mockup coverage

| ID | Scenario | Expected design evidence |
|---|---|---|
| UI-M01 | First launch | Welcome/auth flow |
| UI-M02 | No workspace | Create/select workspace |
| UI-M03 | Multiple workspaces | Switcher |
| UI-M04 | Switch during request | No stale tenant data |
| UI-M05 | No properties | Optional property flow |
| UI-M06 | Standalone asset | No property label |
| UI-M07 | Archived property | Archived badge |
| UI-M08 | Dynamic custom fields | Validation and layout |
| UI-M09 | Open maintenance due | Derived Due presentation |
| UI-M10 | Completed maintenance | Completed lifecycle |
| UI-M11 | Unknown mileage | No prediction |
| UI-M12 | Upload in progress | Progress + finalize |
| UI-M13 | Upload failure | Retry guidance |
| UI-M14 | Push permission denied | Non-blocking explanation |
| UI-M15 | Deep link without membership | Safe denial |
| UI-M16 | Expired token | Session recovery |
| UI-M17 | Offline asset detail | Stale indicator |
| UI-M18 | Save conflict | Refresh/reapply |
| UI-M19 | Large text | Accessible layout |
| UI-M20 | Screen reader | Meaningful traversal |

## 37. Milestone alignment

| Milestone | Mockup priorities |
|---|---|
| M0 | Auth/workspace feasibility and navigation skeleton |
| M1 | Sign-in, workspace creation/selection |
| M2 | Properties, asset type selection/dynamic fields |
| M3 | Asset list/detail/create/edit |
| M4 | Maintenance list/detail/complete and odometer |
| M5 | Documents/upload states |
| M6 | Notification preferences/deep links |
| M7 | Dashboard/search/settings |
| M8 | Accessibility, visual regression, beta polish |

## 38. Explicit out-of-scope visuals

Provider discovery (Phase 2), booking, payments (Phase 3), AI-generated maintenance predictions, automated odometer collection, property valuation, map/geolocation automation and a dedicated platform admin portal are not required Phase 1 mockup deliverables.

Exploratory concepts for these capabilities must be clearly marked `future` and separated from approved MVP exports.

## 39. Open design decisions

- Which bottom-tab composition is final for the MVP?
- Which screen becomes the post-login default when a user has no assets?
- How should archived property references appear in asset detail?
- Which default maintenance list filter is preferred?
- Which device viewport sizes define visual regression baselines?
- What are the finalized semantic tokens after reconciling existing design documents?
- Which notification permission education screen is needed before the OS prompt?
- What localization languages beyond English are committed, if any?

Track answers in product/design decisions before claiming full visual approval.

## 40. Definition of done

- [ ] Mockup inventory has stable IDs and versioned filenames.
- [ ] Approved designs are explicitly distinguished from drafts.
- [ ] Every implemented screen links to the relevant specification.
- [ ] Critical screen states have visual coverage.
- [ ] Workspace isolation and permissions are represented safely.
- [ ] Standalone assets and actual mileage behavior are preserved.
- [ ] Mobile handoff includes route, API, components and tests.
- [ ] Accessibility and platform differences are reviewed.
- [ ] Changes are versioned and documented.

---

**Core principle:** Mockups communicate the user experience; they never override tenant authorization, API contracts, persisted lifecycle semantics or Phase 1 scope.
