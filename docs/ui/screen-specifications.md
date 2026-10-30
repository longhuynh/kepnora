# Kepnora — Phase 1 Mobile Screen Specifications

**Path:** `docs/ui/screen-specifications.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Status:** Implementation-oriented proposal; validate route names, permissions, DTOs, ABP compatibility and final visual tokens against the running application.  
**Platform:** React Native + Expo + TypeScript (iOS and Android)  
**References:** `design-system.md`, `../architecture/mobile-app-architecture.md`, `../architecture/api-design.md`, `../architecture/authentication-authorization.md`, `../architecture/file-storage.md`, `../architecture/background-jobs-and-outbox.md`, `../mvp-build-order.md`, `../database-overview.md`.

---

**Document contract.** This specification covers **Phase 1 only**. It is not proof that APIs or screens already exist. Any route or DTO shown here is a proposed integration contract until verified in generated OpenAPI. Phase 2 provider discovery and Phase 3 booking/payment are explicitly excluded. The product supports assets without properties (`Asset.PropertyId = null`). One workspace maps to one ABP tenant; a global user can join multiple workspaces only after the M0 identity proof of concept succeeds.

## 1. Purpose and intended audience

Give product designers, React Native developers, .NET API developers, QA engineers and AI coding agents a common, testable specification for every Phase 1 screen. Each screen definition states the purpose, navigation, UI structure, data ownership, interactions, validation, loading/empty/error behavior, tenant security and acceptance criteria. The premium direction is inspired by the restraint of Apple Home and Airbnb, **not** a reproduction of their branded interfaces.


## 2. Product scope and phase gates

| Phase | In scope | Navigation behavior |
|---|---|---|
| 1 — MVP | Auth, workspace, properties, assets, types, maintenance, mileage, documents, dashboard, search, notifications, settings | Implement and ship |
| 2 | Provider discovery, provider profiles, quote/contact | Not exposed as working Phase 1 destinations |
| 3 | Appointments, booking, payment | Not exposed as working Phase 1 destinations |

Avoid designing Phase 1 completion screens that promise a provider has been booked. Dashboard and maintenance calls to action must lead to supported Phase 1 functionality.


## 3. Navigation model

Suggested Expo Router information architecture (filenames are proposed and must align with the app architecture document):

```text
app/
  (public)/welcome.tsx
  (public)/sign-in.tsx
  (onboarding)/workspace-create.tsx
  (app)/_layout.tsx
  (app)/(tabs)/index.tsx                 # Home
  (app)/(tabs)/assets/index.tsx          # Assets
  (app)/(tabs)/maintenance/index.tsx     # Maintenance
  (app)/(tabs)/search.tsx                # Search
  (app)/(tabs)/settings.tsx              # Settings
  (app)/workspaces/switch.tsx
  (app)/properties/index.tsx
  (app)/properties/new.tsx
  (app)/properties/[propertyId].tsx
  (app)/assets/new.tsx
  (app)/assets/[assetId]/index.tsx
  (app)/assets/[assetId]/edit.tsx
  (app)/assets/[assetId]/documents.tsx
  (app)/assets/[assetId]/maintenance-rules/new.tsx
  (app)/maintenance/[occurrenceId].tsx
  (app)/maintenance/[occurrenceId]/complete.tsx
  (app)/documents/[documentId].tsx
  (app)/notifications/index.tsx
```

Use a small, stable bottom-tab set: **Home, Assets, Maintenance, Search, Settings**. Properties, documents and notifications are contextual destinations rather than extra tabs. Do not expose provider or booking tabs in MVP. Deep links must run auth, active membership and resource checks before displaying sensitive data.


## 4. Global screen anatomy and design tokens

Every authenticated screen follows the design system: warm off-white canvas, restrained accent, readable dark ink, 16–24px horizontal gutters as a starting point, accessible touch targets, high-quality imagery when supplied by users, and rounded cards with minimal shadow. Confirm final numeric tokens in `design-system.md`.

```text
Safe-area top
  App header: title + workspace context + contextual action
  Optional search/filter bar
  Scrollable content / section headers / cards
  Persistent or sticky action where necessary
  Safe-area bottom / tab navigation
```

Do not use low-contrast placeholder text as the only field label. Preserve keyboard-safe scrolling, dynamic type and reduced-motion settings. Use skeletons only where structure is predictable; never display fake maintenance counts while loading.


## 5. State ownership and caching

| State | Owner | Examples |
|---|---|---|
| Server-authoritative | TanStack Query + generated client | Assets, rules, occurrences, documents, workspace membership |
| Local transient | Component state / React Hook Form | Form fields, modal visibility, selected tab |
| Small app-wide UI | Zustand | Theme choice, temporary filters, onboarding UI |
| Sensitive session | OIDC library + SecureStore as appropriate | Tokens and session metadata |
| Upload progress | Upload controller / component state | Bytes sent, finalize stage |

Every workspace-owned query key includes `workspaceId`; asset-scoped keys also include `assetId`. Switching workspaces cancels or isolates in-flight mutations, clears tenant-specific visible data and refetches authorized resources. Never show previous workspace data during a new workspace loading state.


## 6. API mapping rules

All endpoints in this document refer to **operations**, not guaranteed concrete ABP routes. Implement typed service functions (`listAssets`, `createAsset`, `getMaintenanceOverview`, etc.) backed by OpenAPI-generated clients. Do not hardcode guessed URLs. Authentication is global; authorization additionally checks active workspace membership and permission for each operation. Server validation errors must be mapped to fields where possible, but do not display internal exception details.


## 7. Global loading, empty, error and offline patterns

| State | UX behavior |
|---|---|
| Initial loading | Accessible skeleton for layout; no fabricated values |
| Background refresh | Preserve last authorized data, show subtle refresh indicator |
| Empty | Explain meaning and provide one valid next action |
| Validation error | Field-level error + summary if needed |
| 401 | Attempt supported session refresh once, otherwise return to sign-in |
| 403 | Explain access is unavailable; no leaked record details |
| 404 | Neutral “This item is unavailable” |
| 409 | Explain conflict, reload authoritative state |
| 429 | Respect retry guidance; no aggressive loop |
| 5xx/network | Retry affordance and stable form draft where safe |
| Offline | Cached read-only information only where policy permits; no false save success |

On workspace switch, do **not** preserve the prior tenant's cached content on screen, even during an error. Error messages must avoid showing foreign tenant identifiers or private metadata.


## 8. Global form and destructive action rules

Use React Hook Form + Zod for client feedback; server is authoritative. Required fields have persistent labels; validate trim, length, date and number formats; show errors adjacent to inputs. Disable duplicate submit while pending and use idempotency where supported. For destructive or archival operations, require confirmation with the affected entity name and consequence. Do not implement hard delete where the API only supports archive. After successful mutations, invalidate workspace-scoped queries; show confirmation only after server success.


## 9. Global accessibility and analytics

Support VoiceOver/TalkBack labels, logical focus order, scalable text, color-independent statuses, minimum comfortable tap targets and keyboard navigation. Announce mutation success/error accessibly. Use semantic date/unit formatting rather than hardcoded US formatting.

Telemetry may include screen names, interaction types, timings and error categories, but must not log authentication tokens, presigned URLs, file contents, private notes or sensitive original filenames. Analytics must not be used as a security audit substitute.


## 10. Screen S01 — Welcome and sign-in

**Entry/authorization:** Unauthenticated users. **Header:** Welcome / Sign in.  
**API operations (proposed):** ``startAuthorizationCodePkce`, `completeOidcCallback`, `getSession``.

**Layout and components:** Full-bleed restrained brand artwork or illustration, short value statement, primary Sign in / Create account action, terms/privacy links, visible progress during browser handoff.

**User interactions:** Launch OIDC authorization in the supported system browser; handle callback; restore an existing valid session; do not collect a password in an unapproved custom form.

**Loading, empty and error states:** Invalid callback/state/nonce; cancelled sign-in; offline; expired session. Show retry without exposing token details.

**Acceptance criteria:** Successful sign-in routes to workspace resolution; cancelled auth returns safely; protected content never flashes before session validation.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 11. Screen S02 — Workspace selection

**Entry/authorization:** Signed-in global user with zero or more memberships. **Header:** Workspace picker.  
**API operations (proposed):** ``listMyWorkspaces`, `selectWorkspace`, `getMembership``.

**Layout and components:** Header, list of workspaces with role subtitle, selected state, Create workspace action, optional join/invite entry only if implemented.

**User interactions:** Select workspace; revalidate active membership; switch context; invalidate/partition tenant caches; navigate Home.

**Loading, empty and error states:** No workspaces -> create CTA; removed membership -> deny; stale selection -> clear; network failure -> retry.

**Acceptance criteria:** No data from previous tenant remains visible; revoked memberships cannot be selected; selection survives app restart only if still authorized.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 12. Screen S03 — Create workspace

**Entry/authorization:** Authenticated user without selected workspace or with creation permission. **Header:** Create workspace.  
**API operations (proposed):** ``createWorkspace`, `listMyWorkspaces``.

**Layout and components:** Single-column form: workspace name, optional description if supported, legal/help text, primary Create action.

**User interactions:** Trim and validate name; submit once; resolve authoritative ABP tenant ID; activate membership only after backend confirms.

**Loading, empty and error states:** Duplicate/invalid name according to API policy; failed creation retains draft; no implicit second tenant record.

**Acceptance criteria:** Exactly one ABP tenant registry entry is created; membership is established; navigation to Home occurs after success.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 13. Screen S04 — Home dashboard

**Entry/authorization:** Active workspace member. **Header:** Home.  
**API operations (proposed):** ``getDashboardSummary`, `listDueMaintenance`, `listRecentAssets``.

**Layout and components:** Greeting/title, workspace switcher, quiet hero summary, due/overdue section, asset preview cards, recent activity, add-asset FAB or clear CTA.

**User interactions:** Tap due item -> occurrence detail; tap asset -> asset detail; add -> new asset; pull to refresh.

**Loading, empty and error states:** New workspace -> empty onboarding CTA; partial endpoint failures -> section-specific retry; no fabricated counts.

**Acceptance criteria:** All counts are tenant-scoped and computed by server; status labels distinguish computed Due/Overdue from persisted lifecycle status.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 14. Screen S05 — Assets list

**Entry/authorization:** Member with asset read permission. **Header:** Assets.  
**API operations (proposed):** ``listAssets`, `listAssetTypes`, `listProperties``.

**Layout and components:** Large title, search field, optional type/property filters, sort selector, image-forward cards with asset name and relevant maintenance preview.

**User interactions:** Search/debounce; filter by property including 'No property'; open detail; add asset; paginate.

**Loading, empty and error states:** Empty -> add first asset; search no results -> clear filters; image missing -> neutral icon; offline -> labeled stale cache.

**Acceptance criteria:** Standalone car/device is visible with no property; tenant X never sees tenant Y assets; pagination handles stable keys.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 15. Screen S06 — Create asset

**Entry/authorization:** Member with asset create permission. **Header:** Add asset.  
**API operations (proposed):** ``listAssetTypes`, `getAssetTypeSchema`, `listProperties`, `createAsset``.

**Layout and components:** Progressive form: type, name, optional property, dynamic type-specific fields, optional image upload after asset creation, save button.

**User interactions:** Choose global/tenant type; load versioned schema; render fields; validate; create; navigate to detail; optionally attach photo.

**Loading, empty and error states:** Schema loading error; missing required dynamic field; changed schema version; duplicate submit; permission revoked.

**Acceptance criteria:** Property remains optional; server validates type/version/custom JSONB fields; draft not accidentally submitted to another workspace.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 16. Screen S07 — Asset detail

**Entry/authorization:** Member with asset read permission. **Header:** Asset detail.  
**API operations (proposed):** ``getAsset`, `listAssetDocuments`, `getAssetMaintenanceOverview`, `listServiceHistory``.

**Layout and components:** Large asset photo/neutral illustration, title/type, optional property, next maintenance, quick actions, documents preview, history timeline.

**User interactions:** Edit asset; open maintenance; add rule; upload document; inspect history; archive via confirmation if supported.

**Loading, empty and error states:** Missing asset -> safe 404; archived asset -> read-only policy; individual sections can retry.

**Acceptance criteria:** No invented mileage or next-service date; data reflects authorized workspace; document URLs not embedded permanently.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 17. Screen S08 — Edit/archive asset

**Entry/authorization:** Member with edit/archive permission. **Header:** Edit asset.  
**API operations (proposed):** ``getAsset`, `updateAsset`, `archiveAsset``.

**Layout and components:** Editable fields prefilled from current server record; property selector including None; archival action separated at bottom.

**User interactions:** Validate changes; save; confirm archive with consequences; handle optimistic concurrency if available.

**Loading, empty and error states:** 409 conflict -> reload/diff guidance; no permission -> safe denial; failed archive leaves item intact.

**Acceptance criteria:** Asset remains same tenant; removing property association is permitted; archive does not silently destroy maintenance history.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 18. Screen S09 — Properties list

**Entry/authorization:** Member with property read permission. **Header:** Properties.  
**API operations (proposed):** ``listProperties``.

**Layout and components:** Compact premium cards: property name, optional address summary if schema supports, asset count only when provided by API, Add property.

**User interactions:** Open detail; add; search if list grows; manage assets by property.

**Loading, empty and error states:** No properties -> optional creation explanation, not a blocker to assets.

**Acceptance criteria:** Assets feature works even when property list empty; property counts do not cross tenants.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 19. Screen S10 — Create/edit property

**Entry/authorization:** Member with property write permission. **Header:** Property form.  
**API operations (proposed):** ``createProperty`, `getProperty`, `updateProperty``.

**Layout and components:** Name, address fields as defined by actual DTO, optional description, save button.

**User interactions:** Create/edit; validate address fields per backend rules; navigate to detail/list.

**Loading, empty and error states:** Validation, duplicate, network, conflict; keep draft.

**Acceptance criteria:** Property never becomes mandatory for creating unrelated assets; backend persists tenant-owned record.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 20. Screen S11 — Property detail

**Entry/authorization:** Member with property read permission. **Header:** Property detail.  
**API operations (proposed):** ``getProperty`, `listAssets` filtered by property`.

**Layout and components:** Property title, metadata, linked assets, add asset in this property, edit action.

**User interactions:** Open asset; create asset with preselected property; edit; archive only if business rules allow.

**Loading, empty and error states:** No linked assets -> add CTA; deleted/foreign ID -> safe not found.

**Acceptance criteria:** Preselected property cannot be used to attach an asset across tenants.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 21. Screen S12 — Maintenance overview

**Entry/authorization:** Member with maintenance read permission. **Header:** Maintenance.  
**API operations (proposed):** ``listMaintenanceOccurrences`, `getMaintenanceSummary``.

**Layout and components:** Segmented views for Upcoming/Due/Overdue/Completed where computed statuses are filters, compact date rows, asset identity and quick action.

**User interactions:** Filter by time/status; open occurrence; complete or skip only when supported; refresh.

**Loading, empty and error states:** No occurrences -> create rule CTA; failed summary -> avoid fake badge counts.

**Acceptance criteria:** Persisted occurrence lifecycle remains Open/Completed/Skipped/Cancelled; Upcoming/Due/Overdue are computed display states.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 22. Screen S13 — Maintenance rule list

**Entry/authorization:** Member with rule read permission. **Header:** Maintenance rules.  
**API operations (proposed):** ``listMaintenanceRules` for asset`.

**Layout and components:** Rules grouped by asset, schedule description, active/inactive indicator, add/edit actions.

**User interactions:** Open rule editor; inspect recurrence; toggle only if API supports.

**Loading, empty and error states:** No rules -> create; invalid/unsupported rule -> clear safe state.

**Acceptance criteria:** Recurrence description matches server semantics; no duplicate next occurrence invented client-side.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 23. Screen S14 — Create/edit maintenance rule

**Entry/authorization:** Member with rule write permission. **Header:** Rule editor.  
**API operations (proposed):** ``createMaintenanceRule`, `updateMaintenanceRule`, `getAsset``.

**Layout and components:** Rule name, maintenance category, recurrence mode (time/mileage only if supported), interval, start/due configuration, optional notes.

**User interactions:** Validate recurrence; show human-readable preview derived from same rules as backend; save; refresh occurrences.

**Loading, empty and error states:** Invalid interval; unsupported recurrence; stale version; unknown mileage -> explain unavailable.

**Acceptance criteria:** No mileage estimate from elapsed time; backend owns recurrence calculation and generated occurrences.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 24. Screen S15 — Maintenance occurrence detail

**Entry/authorization:** Member with maintenance read permission. **Header:** Maintenance detail.  
**API operations (proposed):** ``getMaintenanceOccurrence`, `getAsset`, `getMaintenanceRule``.

**Layout and components:** Asset name, maintenance title, due date/odometer trigger if known, computed urgency, persisted lifecycle, notes, history link.

**User interactions:** Complete; skip/cancel with confirmation if authorized; open asset; open rule.

**Loading, empty and error states:** Occurrence already completed by another device -> refresh; missing item -> safe not found.

**Acceptance criteria:** Buttons match allowed lifecycle transitions; computed Due does not become stored status.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 25. Screen S16 — Complete maintenance

**Entry/authorization:** Member with completion permission. **Header:** Complete maintenance.  
**API operations (proposed):** ``getMaintenanceOccurrence`, `completeMaintenanceOccurrence`, `addMileageReading` if separate`.

**Layout and components:** Completion date, optional cost/currency, notes, optional odometer reading when appropriate, optional receipt after completion.

**User interactions:** Submit exactly once; optionally attach receipt through document upload; return to detail/history on server success.

**Loading, empty and error states:** Date outside policy, invalid amount/odometer, conflict, failed upload; completion and receipt are distinct steps.

**Acceptance criteria:** Completion updates occurrence and service history according to backend transaction; no success before commit; receipt failure does not fake a completed attachment.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 26. Screen S17 — Skip/cancel maintenance

**Entry/authorization:** Authorized member. **Header:** Skip or cancel confirmation.  
**API operations (proposed):** ``skipMaintenanceOccurrence`, `cancelMaintenanceOccurrence``.

**Layout and components:** Short explanation of impact, optional reason only if supported, destructive-style confirm and safe back.

**User interactions:** Confirm transition, refresh detail and dashboard.

**Loading, empty and error states:** Concurrent completion -> 409 and refresh; unauthorized -> deny.

**Acceptance criteria:** No direct client mutation of lifecycle status; action cannot be applied to foreign tenant occurrence.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 27. Screen S18 — Service history

**Entry/authorization:** Member with history read permission. **Header:** Service history.  
**API operations (proposed):** ``listServiceHistory`, `getServiceHistoryEntry``.

**Layout and components:** Chronological timeline grouped by month, asset filter, completion details, receipt indicator if present.

**User interactions:** Open history entry; open linked asset/receipt.

**Loading, empty and error states:** No history -> explanation; missing receipt -> do not show broken link.

**Acceptance criteria:** Historical entries survive normal maintenance state changes according to retention policy.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 28. Screen S19 — Mileage readings

**Entry/authorization:** Member with asset/mileage permission. **Header:** Mileage.  
**API operations (proposed):** ``listMileageReadings`, `addMileageReading``.

**Layout and components:** Latest verified reading, reading history, input with unit, timestamp and source note where supported.

**User interactions:** Enter actual observed mileage, validate against business rules, submit and refresh maintenance.

**Loading, empty and error states:** No reading -> 'Not recorded'; invalid negative/implausible value -> server feedback.

**Acceptance criteria:** Never interpolate or infer odometer; display source and timestamp when available.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 29. Screen S20 — Documents list

**Entry/authorization:** Member with document read permission. **Header:** Documents.  
**API operations (proposed):** ``listAssetDocuments``.

**Layout and components:** Grouped/category-filtered file cards, thumbnails only if supported, filename, category, size, date, upload CTA.

**User interactions:** Open detail; upload; archive; paginate.

**Loading, empty and error states:** No docs -> add CTA; S3 unavailable -> metadata still viewable; no thumbnail -> file icon.

**Acceptance criteria:** Do not expose object keys or permanent URLs; all documents belong to current asset and workspace.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 30. Screen S21 — Upload document

**Entry/authorization:** Member with document upload permission. **Header:** Upload document.  
**API operations (proposed):** ``createUploadIntent`, `finalizeUploadIntent`, `listAssetDocuments``.

**Layout and components:** Native file picker, category, display filename, size, staged progress (preparing/uploading/finalizing), retry/cancel.

**User interactions:** Request authorized intent; PUT bytes to S3; finalize via API; invalidate list after finalize.

**Loading, empty and error states:** Expired URL, app interruption, S3 timeout, size mismatch, finalize conflict, workspace switch.

**Acceptance criteria:** PUT success alone is not 'uploaded'; only finalized Document appears; retry idempotently and never leak presigned URL.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 31. Screen S22 — Document detail/viewer

**Entry/authorization:** Member with document read permission. **Header:** Document detail.  
**API operations (proposed):** ``getDocument`, `createDocumentDownloadUrl`, `archiveDocument``.

**Layout and components:** Metadata, safe preview/download action, category, linked asset, archive option if allowed.

**User interactions:** Reauthorize for fresh short-lived GET; open supported viewer; confirm archive.

**Loading, empty and error states:** Link expiry -> request new URL; member revoked -> deny; unsupported preview -> download.

**Acceptance criteria:** No permanent S3 URL cached; archived access follows policy; foreign document ID reveals nothing.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 32. Screen S23 — Global search

**Entry/authorization:** Member with search permission. **Header:** Search.  
**API operations (proposed):** ``searchWorkspace`, or coordinated scoped asset/maintenance/property queries`.

**Layout and components:** Prominent search input, recent local queries if privacy-approved, filter chips by resource, grouped results with concise context.

**User interactions:** Debounce, clear, filter, open result, paginate, requery on workspace switch.

**Loading, empty and error states:** No matches -> suggestion; offline -> clearly limited cached search; server failure -> retry.

**Acceptance criteria:** Search always tenant-scoped; no provider discovery in Phase 1; deep-linked results reauthorize.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 33. Screen S24 — Notifications inbox

**Entry/authorization:** Member with notification read permission. **Header:** Notifications.  
**API operations (proposed):** ``listNotifications`, `markNotificationRead` if API supports`.

**Layout and components:** Chronological reminders, unread marker, related asset/maintenance context, notification preferences shortcut.

**User interactions:** Open target via guarded deep link; mark read; refresh.

**Loading, empty and error states:** Expired/deleted target -> safe fallback; no notifications -> empty state.

**Acceptance criteria:** Push payload must not reveal private file contents; tapping a notification cannot bypass workspace membership.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 34. Screen S25 — Notification preferences

**Entry/authorization:** Member with settings permission. **Header:** Reminder settings.  
**API operations (proposed):** ``getNotificationPreferences`, `updateNotificationPreferences`, `registerPushDevice``.

**Layout and components:** Push toggle, reminder preferences, device status and permission explanation.

**User interactions:** Request OS permission at appropriate time; save preferences; handle denied OS permission.

**Loading, empty and error states:** OS denied -> show settings guidance; backend update failure -> restore state.

**Acceptance criteria:** Backend preference and OS permission are distinct; no claim of delivery guarantee.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 35. Screen S26 — Settings and profile

**Entry/authorization:** Authenticated user with selected workspace. **Header:** Settings.  
**API operations (proposed):** ``getProfile`, `getWorkspaceSettings`, `listMyWorkspaces``.

**Layout and components:** Account identity, current workspace, units/timezone, notification preferences, document/security info, sign out.

**User interactions:** Open profile, workspace switch, preferences, legal/help, logout.

**Loading, empty and error states:** Offline settings -> show cached values only if safe; failed logout revocation -> local session cleanup per auth policy.

**Acceptance criteria:** Global profile separated from workspace settings; sign out clears sensitive cached state.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 36. Screen S27 — Workspace members and roles

**Entry/authorization:** Workspace owner or authorized administrator. **Header:** Members.  
**API operations (proposed):** ``listTenantMembers`, `inviteMember` if Phase 1 supported, `updateMemberRole`, `removeMember``.

**Layout and components:** Member list, role labels, owner/admin actions, confirmation dialogs; hide invite controls if not implemented.

**User interactions:** Invite/change/remove with server validation; refresh membership.

**Loading, empty and error states:** Owner safety rule, self-removal, last-owner constraint and pending invites are backend-defined.

**Acceptance criteria:** Never rely on role label alone; enforce active membership and permission on each operation.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 37. Screen S28 — Workspace switching

**Entry/authorization:** Signed-in user with multiple active memberships. **Header:** Switch workspace.  
**API operations (proposed):** ``listMyWorkspaces`, `selectWorkspace`, `getMembership``.

**Layout and components:** Bottom sheet or full screen with workspace names and current selection; loading transition.

**User interactions:** Cancel in-flight tenant mutations; switch; reset scoped navigation and query state; fetch new workspace.

**Loading, empty and error states:** Revoked membership, offline switch, pending document upload.

**Acceptance criteria:** No old tenant content flashes; upload remains bound to origin or is cancelled; guarded routes re-evaluate.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 38. Screen S29 — Account session and sign out

**Entry/authorization:** Signed-in user. **Header:** Sign out.  
**API operations (proposed):** ``revokeRefreshToken` if supported, `clearLocalSession``.

**Layout and components:** Short confirmation if needed, clear action, optional session-expiry explanation.

**User interactions:** Revoke where supported; clear secure token storage; remove tenant caches and navigation history.

**Loading, empty and error states:** Revocation endpoint unavailable -> clear local session and communicate limitations without false server revocation claim.

**Acceptance criteria:** Back navigation cannot reveal protected screens; no token or private document persists in ordinary app state.

**QA minimum:** verify keyboard/screen-reader navigation; test fresh and stale data; deny access with removed membership; validate iOS and Android safe areas; confirm no cross-workspace cache leakage.


## 39. Screen-to-milestone delivery matrix

| Milestone | Primary screens |
|---|---|
| M0 | S01 auth proof and tenant identity behavior |
| M1 | S01–S03, S28–S30 (only supported workspace/member capabilities) |
| M2 | S09–S11, type schema selection in S06 |
| M3 | S05–S08 |
| M4 | S12–S19 |
| M5 | S20–S22 |
| M6 | S24–S25 |
| M7 | S04, S23, S26–S27 |
| M8 | Full security, offline, accessibility and release verification |

Do not ship screens that require an unimplemented backend operation; gate or remove those actions rather than simulating success.


## 40. Cross-screen acceptance journeys

1. New user signs in, creates workspace, creates a **standalone vehicle without a property**, adds a maintenance rule, and sees it on Home.
2. User creates a property, adds an appliance, uploads a warranty PDF, finalizes and views it.
3. User records actual mileage, completes maintenance and verifies service history; no estimated mileage appears.
4. User with memberships X and Y switches from X to Y; no X assets, documents, counts, search results or notification details remain visible.
5. User loses membership during an open asset detail; next mutation/download is denied and screen exits safely.
6. User uploads a file, closes the app after PUT, reopens and retries finalization without a duplicate document.
7. User sees Due/Overdue as computed labels while occurrence lifecycle remains Open.
8. User signs out, relaunches and cannot navigate back to protected screens.
9. User with no properties creates an asset and uses all core maintenance features.
10. User opens a stale deep link from a push notification and receives a neutral error without tenant metadata leakage.


## 41. Recommended TypeScript contracts

```ts
export type WorkspaceId = string;
export type AssetId = string;

export type ScreenRequestState<T> =
  | { kind: 'loading' }
  | { kind: 'ready'; data: T; isRefreshing: boolean }
  | { kind: 'empty' }
  | { kind: 'offline'; cachedData?: T }
  | { kind: 'error'; message: string; retryable: boolean };

export type MaintenanceLifecycle =
  | 'Open'
  | 'Completed'
  | 'Skipped'
  | 'Cancelled';

export type MaintenanceDisplayStatus =
  | 'Upcoming'
  | 'Due'
  | 'Overdue';

export const assetKeys = {
  all: (workspaceId: WorkspaceId) =>
    ['workspaces', workspaceId, 'assets'] as const,
  detail: (workspaceId: WorkspaceId, assetId: AssetId) =>
    ['workspaces', workspaceId, 'assets', assetId] as const,
};
```

These are illustrative client types, not a substitute for generated API contracts. Keep display urgency separate from persisted lifecycle.


## 42. Component inventory for AI coding agents

| Component | Screens | Key requirements |
|---|---|---|
| `KepnoraScreen` | All | Safe area, background, consistent padding |
| `WorkspaceSwitcher` | Home/Settings/S28 | Membership recheck and cache isolation |
| `AssetCard` | Home/Assets/Property | Image fallback, status, accessible tap |
| `MaintenanceRow` | Home/Maintenance/Detail | Computed urgency vs lifecycle |
| `DocumentRow` | Asset/Documents | Metadata only; no permanent URL |
| `UploadProgressCard` | Upload | Distinct PUT and finalize phases |
| `FormField` | All forms | Persistent label and inline error |
| `EmptyState` | Lists | Valid CTA and no invented content |
| `PermissionBoundary` | Admin/actions | UI affordance only; server still authorizes |
| `ConfirmActionSheet` | Archive/skip/cancel | Describes consequences |
| `RetryPanel` | Remote states | Controlled retry, no endless loop |
| `SearchResultRow` | Search | Typed destination + tenant guard |

Use tokens from `design-system.md`; do not introduce ad hoc colors or spacing in each screen.


## 43. Screen performance budgets and implementation notes

Use paginated asset, document and maintenance lists; avoid fetching full asset libraries on Home. Defer document presigning until the user actually opens a file. Use appropriate image caching only for authorized content and clear tenant-sensitive caches on switch/sign-out. Avoid rendering complex dynamic forms before asset type schema loads. Use stable list keys and memoized card components where profiling supports it. Measure cold start, navigation, input latency and low-end Android performance; exact performance thresholds are release decisions, not invented targets.


## 44. Security and privacy checklist

- [ ] Every protected screen requires a valid authenticated session.
- [ ] Every workspace-owned request checks active membership on the server.
- [ ] Resource IDs are always resolved within the current workspace.
- [ ] No query key omits workspace scope for tenant-owned data.
- [ ] Workspace switch cannot display prior tenant content.
- [ ] Deep links and push targets reauthorize before display.
- [ ] Upload intent, finalize and download are separately authorized.
- [ ] Presigned URLs are not logged or persisted as ordinary cache data.
- [ ] Documents do not expose object keys or public URLs.
- [ ] Logout clears tokens and sensitive local state.
- [ ] Sensitive data is excluded from crash reporting and analytics.
- [ ] UI permissions never substitute for backend authorization.


## 45. UI test catalog

| ID | Test | Expected |
|---|---|---|
| UI-001 | Fresh install | Welcome/sign-in without protected flash |
| UI-002 | Valid session restored | Correct workspace revalidated |
| UI-003 | No workspaces | Create workspace CTA |
| UI-004 | Workspace switch X→Y | No X content or cache leak |
| UI-005 | Revoked workspace membership | Denied, safe redirect |
| UI-006 | Create standalone asset | PropertyId remains null |
| UI-007 | Dynamic schema required field | Client and server errors shown |
| UI-008 | Empty assets | Helpful add CTA |
| UI-009 | Paginated assets | No duplicate cards |
| UI-010 | Maintenance Open + overdue date | Overdue display, Open lifecycle |
| UI-011 | Complete occurrence twice | Idempotent/conflict-safe |
| UI-012 | Unknown mileage | 'Not recorded', never estimated |
| UI-013 | PUT success, finalize fails | Not displayed as completed document |
| UI-014 | Upload interrupted | Retry path and preserved origin workspace |
| UI-015 | Expired presigned GET | Reauthorize for new URL |
| UI-016 | Unauthorized deep link | Neutral denial |
| UI-017 | Offline mutation | No false success |
| UI-018 | Dynamic text 200% | No clipped primary controls |
| UI-019 | VoiceOver/TalkBack | Correct labels and order |
| UI-020 | Dark mode | Readable contrast and semantics |
| UI-021 | Keyboard open on long form | Save remains reachable |
| UI-022 | 409 conflict | Current server state recoverable |
| UI-023 | Sign out + back gesture | Protected screen not accessible |
| UI-024 | Provider/booking Phase 1 | No misleading working routes |
| UI-025 | S3 file missing | Safe error and retry guidance |
| UI-026 | Member role changes | Action availability refreshed |
| UI-027 | Notification deep link old tenant | Membership checked |
| UI-028 | Search no results | Filters clear without stale results |
| UI-029 | Property deleted/archived | Asset association follows server policy |
| UI-030 | Concurrent asset edit | Conflict is handled safely |


## 46. Implementation sequence for AI coding agents

1. Read the architecture, API and design system documents before coding.
2. Pin Expo/React Native/ABP dependencies and validate authentication POC.
3. Implement design tokens, `KepnoraScreen`, form and state primitives.
4. Implement authenticated shell, workspace selection and tenant-safe query keys.
5. Build property and asset screens against generated contracts.
6. Add maintenance rule, occurrence, completion and mileage flows.
7. Implement document upload state machine and safe viewer.
8. Implement dashboard/search/notification/settings after underlying endpoints exist.
9. Add deep-link guards, offline handling and comprehensive cross-tenant tests.
10. Run device accessibility/performance QA and update this spec for observed behavior.

Never implement fake backend success, infer undocumented endpoints or silently change persisted maintenance status semantics.


## 47. Open product and technical decisions

1. Exact tab labels and final navigation hierarchy.
2. Final premium token values and selected font family.
3. Workspace member invitation capabilities included in MVP.
4. Archive/delete availability and restoration policy.
5. Notification inbox persistence vs push-only reminders.
6. Whether global search has one endpoint or federates scoped queries.
7. Offline caching duration and local encryption requirements.
8. Document preview types and image thumbnail pipeline.
9. Odometer unit conversion and historical reading validation.
10. Dynamic asset type field presentation and schema migration UX.
11. Exact error contract, permission names and OpenAPI-generated DTOs.
12. Whether multi-workspace switching is exposed during onboarding or only after setup.

Record decisions in ADRs or the owning API/architecture spec; do not invent them in UI implementation.


## 48. Definition of Done

- [ ] Every Phase 1 screen has a defined route, entry guard, data contract and test coverage.
- [ ] All mutations show server-confirmed results only.
- [ ] All list screens support loading, empty, error and authorized refresh states.
- [ ] All workspace-owned caches and deep links are tenant-safe.
- [ ] Standalone assets with null PropertyId work end to end.
- [ ] Maintenance display urgency is not persisted as lifecycle state.
- [ ] Document upload requires successful finalize.
- [ ] Premium visual components use centralized design tokens.
- [ ] Accessibility and iOS/Android device checks pass.
- [ ] Phase 2/3 features are not misrepresented as working MVP functionality.
- [ ] API names, DTOs and permissions are verified against actual generated contracts.
- [ ] Security and UI test catalogs are executed and tracked.


## 49. Final implementation principle

**Show only authorized, server-grounded information; make the next user action obvious; and never trade tenant isolation or data correctness for a polished visual effect.** The premium UI should make asset care feel simple without concealing state transitions, upload failures or maintenance uncertainty.

