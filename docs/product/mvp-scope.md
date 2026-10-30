# Kepnora — Phase 1 MVP Scope

**Canonical path:** `docs/product/mvp-scope.md`  
**Version:** 1.0-draft · **Updated:** 2026-10-29  
**Status:** Proposed baseline; open decisions require approval  
**Audience:** Product, engineering, design, QA and AI coding agents

> This document consolidates Phase 1 boundaries without overriding canonical architecture, database, feature or API specifications.

---

## 1. Purpose and decision status

This specification is the Phase 1 MVP scope baseline for Kepnora. It defines in-scope capabilities, exclusions, dependencies, acceptance criteria and release gates. Product requirements are distinguished from proposals. This document does not imply that any feature is implemented or that the M0 technical proof of concept is complete.


## 2. Product objective

Help households and small asset-owning teams inventory assets, maintain service history, keep private receipts/manuals and receive reminders. The first release is mobile-first and should be useful even when a user has only one vehicle and no property.


## 3. Target audiences

**Primary:** individual and household owners of vehicles, appliances, electronics and homes. **Secondary:** small teams coordinating assets in one or more workspaces. Not an enterprise CMMS, real estate marketplace, professional booking platform or payment processor.


## 4. MVP outcomes

1. A user can sign in and securely access one or more workspaces.
2. A user can register an asset without creating a property.
3. An asset can have type-specific fields and a maintenance schedule.
4. Actual completed service and actual mileage can be recorded.
5. Private receipts/manuals can be uploaded and retrieved securely.
6. Eligible maintenance reminders can be delivered on a best-effort basis.
7. A user can find an asset, see what is due and review history.


## 5. Scope at a glance

| Capability | Phase 1 MVP | Phase 2 | Phase 3 |
|---|---|---|---|
| Global account and workspaces | Yes | Improve | Improve |
| Properties and standalone assets | Yes | Improve | Improve |
| Versioned asset type schemas | Yes | Extend | Extend |
| Maintenance and actual mileage | Yes | Improve | Improve |
| Private documents | Yes | Improve | Improve |
| Expo push reminders | Yes | Improve | Improve |
| Dashboard/search/settings | Yes | Improve | Improve |
| Service-provider discovery | No | Yes | Yes |
| Provider booking/scheduling | No | No | Yes |
| Payments | No | No | Yes |


## 6. Non-negotiable architecture invariants

- One workspace maps to exactly one ABP Tenant; ABP Tenant Management is the authoritative tenant registry.
- A global user can belong to multiple workspaces; active membership and named permission are checked on every tenant operation.
- `ICurrentTenant` and `IMultiTenant` are data-scoping mechanisms, **not authorization**.
- `Asset.PropertyId` remains nullable.
- Published asset type versions are immutable.
- Maintenance occurrence persisted lifecycle statuses are only `Open`, `Completed`, `Skipped`, `Cancelled`.
- `Upcoming`, `Due`, `Overdue` are derived display states.
- Mileage comes from user-entered actual readings; no estimation or projection.
- Maintenance completion is concurrency-safe and idempotent.
- S3 objects are private; upload uses intent → PUT → finalize.
- Push delivery is at-least-once/best-effort, not guaranteed exactly once.
- Mobile query caches are workspace-partitioned.
- PostgreSQL is the canonical relational database.


## 7. M0 technical foundation is a release blocker

Before locking API contracts or identity flows, demonstrate .NET 10 and the pinned ABP Framework version can compile and run together. Prove a **single global user** can access two ABP tenants with different memberships/permissions without cross-tenant data exposure. Validate migrations, tenant resolution, permission definitions and OpenAPI generation. Failure blocks downstream implementation; do not silently assume ABP defaults satisfy the product requirement.


## 8. Authentication in scope

- Secure global sign-in and sign-out with OIDC Authorization Code + PKCE.
- Mobile session recovery and refresh according to selected identity provider capabilities.
- Tokens stored in platform secure storage, not plain AsyncStorage.
- Expired/revoked sessions lead to safe reauthentication.
- Deep links must revalidate access.
- Password reset, email verification and social login depend on selected identity provider and must be explicitly approved rather than assumed.


## 9. Workspace management in scope

- Create/select a workspace; one workspace = one tenant.
- Global user membership can span workspaces.
- Owner/Member roles with server-enforced permissions.
- Workspace switch cancels/partitions requests and clears sensitive previous-workspace state.
- Basic member management and notification settings, subject to the approved membership API contract.
- No cross-workspace sharing of tenant-owned assets or documents.


## 10. Onboarding in scope

Sign in → create or select workspace → optionally create property → add first asset → optionally create maintenance rule or upload a document. Users may skip property creation. No forced property, credit card, service-provider registration or payment setup.


## 11. Properties in scope

Create, list, view, edit and archive properties with name, type and optional address/timezone. Property is a grouping mechanism only. Archiving a property does not delete or archive its assets, maintenance history or documents. Standalone assets with `PropertyId = NULL` must remain fully supported.


## 12. Asset types in scope

Support shared global definitions and workspace-specific custom types with immutable published schema versions and JSONB custom fields. Validate fields server-side against the asset's pinned schema version. Avoid unapproved schema dialects or implicit upgrades of existing assets. Type editing/publishing UX must be consistent with the custom-asset-types spec.


## 13. Assets in scope

Create, list, detail, edit and archive assets; optional property association; type-specific data; tenant-safe filtering; relevant photos and document links. Asset ownership is scoped to the workspace. Cross-tenant property IDs and asset type definitions must be rejected. Asset archive must preserve service history.


## 14. Maintenance rules in scope

Create and manage time-based and mileage-based recurring rules as specified in the scheduling engine. Generate concrete occurrences with deterministic schedule keys. Recurrence policies (including end-of-month, DST, completion-based anchors and rule edits) must be explicitly defined and tested; do not duplicate scheduling logic in mobile.


## 15. Maintenance occurrences in scope

Persist only `Open`, `Completed`, `Skipped`, `Cancelled`. Compute `Upcoming`, `Due`, `Overdue` from rule/occurrence data and an explicit clock/timezone. Users can complete, skip or cancel according to permissions and allowed transitions. Preserve historical completion data and prevent duplicate effects under retry/concurrency.


## 16. Odometer and mileage in scope

Users manually enter actual odometer readings. Display the latest valid reading and its recorded time, with an unknown state if none exists. Mileage-based maintenance uses actual recorded values only; no future mileage prediction, device sync or inferred distance. Corrections and non-monotonic readings follow an explicit validation/audit policy.


## 17. Documents and warranties in scope

Private S3-backed upload/download for asset-related photos, receipts, manuals and warranty files where the canonical document model permits them. Upload flow: authorized intent, presigned object PUT, finalize with metadata validation. Signed links are short-lived and never logged. Warranty expiration dates are **not** assumed persisted in the current schema; do not claim automated warranty-expiry reminders without a schema decision.


## 18. Notification engine in scope

Determine eligibility for upcoming/due/overdue maintenance reminders, apply preferences/quiet hours and enqueue durable notification intents. Worker rechecks maintenance state and workspace access before send. Register Expo device push tokens, process tickets/receipts, retry transient failures, and deduplicate best-effort. OS permission or provider acceptance does not prove delivery.


## 19. Dashboard in scope

Show tenant-scoped asset counts, actionable maintenance summary and recent relevant activity using existing domain data. All counts and statuses follow canonical maintenance semantics; archived property must not implicitly remove assets from counts. Dashboard must handle new/empty workspace and network errors.


## 20. Search in scope

Tenant-scoped asset/property/maintenance search as approved by the API master. Provide predictable pagination and no leakage across workspaces. Search may prioritize asset name, type and property grouping; full-text global search infrastructure is not an MVP prerequisite.


## 21. Settings and members in scope

Basic workspace identity/settings, membership list and role actions (subject to permission model), notification preferences and sign-out. Platform-wide operator admin portal is deferred. Do not add tenant billing or complex role builders without explicit product approval.


## 22. Mobile experience in scope

React Native + Expo + TypeScript with Expo Router, TanStack Query, Zustand for UI-only state, React Hook Form + Zod, SecureStore and generated OpenAPI client. Required screen states: loading, empty, success, validation, offline, conflict and permission denied. Support iOS and Android and accessibility basics.


## 23. Offline behavior in scope

A safe read-only cached experience is acceptable if workspace isolation and privacy are preserved. Offline write synchronization is **not** required in MVP: do not queue maintenance completions or create requests unless durable idempotency/conflict handling is separately designed. Explain failures and allow safe retries.


## 24. Explicit exclusions — Phase 1

- Service-provider search, recommendations and Places API integration.
- Provider profiles, booking, calendar scheduling, payments and refunds.
- Automatic mileage capture, OBD/IoT or telematics integrations.
- Warranty expiration tracking requiring unapproved database columns.
- Property valuation, leasing and rent collection.
- Complex facilities hierarchies, rooms and floor plans.
- Cross-tenant asset sharing or public document links.
- Dedicated platform admin portal, advanced billing and subscription enforcement.
- Guaranteed push delivery, SMS or email notification channels unless separately approved.
- AI diagnostics, OCR and automatic maintenance recommendations.


## 25. Phase 2 boundary — Find Services

Add in-app discovery of nearby relevant service providers, potentially behind `IProviderDiscoveryService` to avoid provider lock-in. Discovery can show business information and links, but **does not** imply booking, provider availability or payment handling. Requires new privacy, location and provider-data decisions.


## 26. Phase 3 boundary — Book and Pay

Add booking, provider availability/scheduling, confirmation and payments only after provider and commercial models are approved. Requires payment security, cancellations/refunds, disputes, provider onboarding and additional legal/compliance review. None of these should be implemented as hidden Phase 1 dependencies.


## 27. Proposed mobile screen inventory

| Area | Screens |
|---|---|
| Auth | Sign in, session recovery |
| Workspace | Create/select workspace, switch |
| Home | Dashboard |
| Properties | List, detail, create/edit |
| Asset types | Picker and authorized custom type flow |
| Assets | List, detail, create/edit |
| Maintenance | List, occurrence detail, rule form, complete/history |
| Mileage | Add reading, reading history where supported |
| Documents | List, upload progress, preview/download |
| Search | Search results and filters |
| Settings | Workspace, members, notification preferences |

Actual route names remain proposals until Expo Router and OpenAPI contracts are validated.


## 28. API contract boundary

Use the approved `/api/v1/workspaces/{workspaceId}/...` routes from `docs/api/api-contracts-master.md` as **proposals**, not proof of generated ABP endpoints. Generate the TypeScript client from the running backend OpenAPI document. Consistent error envelopes, tenant membership, pagination and concurrency handling are release requirements.


## 29. Database and migrations boundary

Use existing PostgreSQL/EF Core schema as source of truth. Do not create a parallel custom `Tenants` table. Use tenant-aware composite FKs for asset→property and other tenant-owned references. Do not invent columns for unapproved product requirements. Migrations must be reviewed and integration-tested on real PostgreSQL.


## 30. Security and privacy acceptance

- Every tenant-owned read/write validates active membership and permission.
- Cross-tenant IDs never expose resource existence or data.
- Revoked membership takes effect on subsequent requests.
- S3 files are private; presigned URLs expire and are not logged.
- Mobile workspace switch prevents stale data flashes.
- Device tokens and addresses are treated as private data.
- Audit sensitive mutations without leaking secrets.
- Background workers restore tenant context per job.


## 31. Reliability and idempotency acceptance

- Duplicate maintenance completion requests do not create duplicate history or next occurrences.
- Occurrence generation is deterministic under concurrency.
- Upload finalize is safe under retries and cleans up orphans.
- Outbox retries do not create uncontrolled duplicate intents.
- Network errors surface meaningful recovery actions.
- Worker crashes and delayed pushes are covered by tests.


## 32. Performance and scale acceptance

Measure rather than invent targets. Establish baseline latency and query plans for property/asset lists, maintenance dashboard and tenant-scoped search. Cursor or stable pagination must prevent unbounded lists. Background jobs should be bounded per tenant and monitored. Concrete latency/SLO numbers are **open product/engineering decisions**.


## 33. Analytics and success signals

Suggested metrics, pending privacy review: onboarding completion, first asset creation, percentage with maintenance rule, successful document finalize, active workspaces, reminder eligibility and provider error rates. Avoid collecting private address, serial number, document contents or push token in analytics. These are instrumentation proposals, not monetization goals.


## 34. Milestone M0 — foundation

**Deliver:** pinned .NET/ABP/Expo dependencies, compiling backend, PostgreSQL migration baseline, global identity/two-tenant POC, OpenAPI generation and tenant permission tests.

**Exit gate:** one global user belongs to two tenants with distinct roles; unauthorized tenant data access fails; tests demonstrate no parallel tenant registry.


## 35. Milestone M1 — account/workspace

**Deliver:** auth and session handling, workspace create/select/switch, membership/permissions, onboarding shell.

**Exit gate:** membership revocation enforced, mobile cache tenant-safe, login/logout/recovery tested on iOS and Android.


## 36. Milestone M2 — properties/types

**Deliver:** optional property CRUD/archive and versioned global/workspace asset types with validation.

**Exit gate:** standalone asset path remains possible; published schema immutable; cross-tenant references rejected.


## 37. Milestone M3 — assets

**Deliver:** asset CRUD, optional property assignment, type-specific forms, list/detail/filter, archival.

**Exit gate:** real PostgreSQL tests, mobile states, generated client and tenant safety.


## 38. Milestone M4 — maintenance

**Deliver:** rule management, deterministic occurrences, completion/skip/cancel/history and manual mileage.

**Exit gate:** month-end/DST, concurrent completion, retries and unknown mileage cases pass.


## 39. Milestone M5 — documents

**Deliver:** private S3 intent/PUT/finalize/download, attachments and orphan cleanup.

**Exit gate:** authorization and content validation, upload retry, no public bucket access.


## 40. Milestone M6 — notifications

**Deliver:** eligibility planner, durable outbox, Expo push registration, worker and receipt handling.

**Exit gate:** stale/completed maintenance suppression, membership recheck, retry/quiet-hours tests.


## 41. Milestone M7 — dashboard/search/settings

**Deliver:** dashboard, tenant-scoped search, workspace/member settings and notification preferences.

**Exit gate:** counts consistent with domain rules, no cross-workspace leakage, complete navigation and empty states.


## 42. Milestone M8 — beta hardening

**Deliver:** security/performance testing, deployment and monitoring, store build validation, backups/recovery, accessibility, end-to-end workflows and release candidate.

**Exit gate:** all release-blocking bugs resolved and evidence documented for each invariant.


## 43. Feature acceptance matrix

| Feature | Minimum demonstrable acceptance |
|---|---|
| Auth | Valid session, refresh/recovery, logout |
| Workspace | Two-tenant membership and safe switching |
| Property | Optional CRUD/archive without asset cascade |
| Asset type | Published version pinned and validated |
| Asset | CRUD with `PropertyId = NULL` |
| Maintenance | Correct lifecycle and deterministic schedule |
| Mileage | Manual actual reading only |
| Documents | Private S3 upload/finalize/download |
| Push | Eligible reminder with stale-state suppression |
| Dashboard | Correct tenant-scoped counts |
| Search | Correct results and pagination |
| Settings | Authorized member/preferences actions |


## 44. Negative test matrix

| ID | Scenario | Expected |
|---|---|---|
| MVP-N01 | Member requests foreign workspace asset | Denied without leak |
| MVP-N02 | Valid JWT but revoked membership | Denied |
| MVP-N03 | Asset created without property | Success |
| MVP-N04 | Asset assigned foreign property | Denied |
| MVP-N05 | Published type schema edited | Rejected |
| MVP-N06 | Duplicate maintenance completion | One effect |
| MVP-N07 | Missing odometer reading | Unknown, no estimate |
| MVP-N08 | Maintenance completed before push send | Reminder suppressed |
| MVP-N09 | S3 finalize wrong tenant | Denied |
| MVP-N10 | Switch workspace mid-fetch | No stale data |
| MVP-N11 | Offline write | Clear safe failure |
| MVP-N12 | Property archive with linked assets | Assets/history retained |
| MVP-N13 | DST/month-end recurrence | Stable correct date |
| MVP-N14 | Concurrent occurrence generator | No duplicates |
| MVP-N15 | Expired presigned upload | Safe retry |


## 45. Release checklist

- [ ] M0 global identity/ABP compatibility POC passed.
- [ ] M1–M8 gates passed with reproducible evidence.
- [ ] Domain invariants tested on real PostgreSQL.
- [ ] OpenAPI and generated mobile client are synchronized.
- [ ] All user flows have loading/empty/error/offline states.
- [ ] Tenant isolation tested in API, worker and mobile.
- [ ] Private S3 and token storage security reviewed.
- [ ] Background jobs/outbox are observable and recoverable.
- [ ] iOS and Android EAS builds verified.
- [ ] Store privacy disclosures and support process prepared.
- [ ] Backups, incident response and rollback plan rehearsed.
- [ ] Product owner signs off on all open decisions affecting MVP.


## 46. Definition of Done for any MVP story

A story is done only when source builds with pinned dependencies; migration is reviewed; AppService enforces tenant membership/permissions; OpenAPI and generated TypeScript match; validation/conflict/cross-tenant errors are stable; unit/integration tests run against real PostgreSQL; mobile covers loading/empty/success/validation/offline/error; logs omit secrets; retries are idempotent; docs/ADRs are updated; demo evidence exists.


## 47. Risks and mitigations

| Risk | Mitigation |
|---|---|
| ABP global identity assumptions | M0 POC is release blocker |
| Recurrence edge cases | Deterministic algorithm + DST/month-end tests |
| Tenant leakage | Membership guards + composite FKs + mobile cache partition |
| File access leakage | Private S3 + scoped presigned URLs + finalize checks |
| Push duplication/loss | Outbox, retries, suppression and observability |
| Scope creep | Phase 2/3 exclusion list and change control |
| Unstable mobile contracts | Generate client from backend OpenAPI |


## 48. Change control

Any request that adds a table/column, changes lifecycle states, introduces new billing or provider integration, or changes workspace authorization must include: rationale, user outcome, phase assignment, API/schema impact, security implications, migration plan, tests, and approval. Update ADRs and feature matrix before implementation.


## 49. Open decisions

1. Exact ABP/.NET 10 compatible versions and global identity implementation.
2. Identity provider capabilities (email verification, password reset, social login).
3. Role/permission matrix and member invitation semantics.
4. Asset type schema dialect and publish/edit workflow.
5. Maintenance recurrence anchor and timezone precedence.
6. Document ownership/warranty expiration schema extension.
7. Offline cache retention policy.
8. Quantitative beta performance and reliability thresholds.
9. App Store/Play Store launch geography and language support.
10. Pricing/monetization strategy (separate product decision).


## 50. Dependencies and authoritative references

- `docs/mvp-build-order.md` — milestone order and INV-01–INV-15.
- `docs/architecture-overview.md` — modular monolith and infrastructure.
- `docs/architecture/tenant-isolation.md` — authorization model.
- `docs/architecture/maintenance-scheduling-engine.md` — recurrence semantics.
- `docs/architecture/notification-engine.md` — reminder/outbox semantics.
- `docs/features/asset-management.md` and `custom-asset-types.md`.
- `docs/features/property-management.md`, `maintenance-management.md`, `documents-warranties.md`.
- `docs/api/api-contracts-master.md` — proposed endpoint contracts.
- `docs/implementation/mobile-project-structure.md` — mobile architecture.
- `docs/product/roadmap.md`, `pricing.md`, `feature-matrix.md` — future companion documents.


## 51. Final scope statement

**Phase 1 = Own & Maintain. Phase 2 = Find Services. Phase 3 = Book & Pay.** Do not hide Phase 2/3 work inside Phase 1 infrastructure or UI. The MVP is successful when a user can securely track a standalone asset, manage its maintenance from actual data, retain private evidence and receive eligible reminders in the correct workspace.

