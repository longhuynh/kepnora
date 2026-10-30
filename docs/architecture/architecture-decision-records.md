# Kepnora — Architecture Decision Records (ADRs)

**Intended path:** `docs/architecture/architecture-decision-records.md`  
**Version:** 1.0-draft  
**Last reviewed:** 2026-10-29  
**Scope:** Phase 1 MVP; Phase 2 boundaries noted where necessary  
**Status:** Decision register and proposed ADRs, not an implementation certification

**Companion documents:** `../README.md`, `../kepnora-project-context.md`, `../architecture-overview.md`, `../database-overview.md`, `../mvp-build-order.md`, `tenant-isolation.md`.

---

## 1. Purpose

This document is Kepnora's **single architecture decision register**. Its purpose is to preserve why a technical choice was made, which alternatives were considered, what must remain true across the codebase, and what evidence is required before a proposal becomes an approved decision.

A decision record is **not** a substitute for working code or proof. In particular, the ABP Identity and multi-workspace model is still subject to an implementation POC. Other documents must not silently treat unresolved choices as settled.

The product is a mobile-first personal asset and maintenance management platform. Phase 1 includes workspaces, optional properties, assets, versioned custom fields, maintenance, service history, documents, reminders, dashboard, search and settings. Provider discovery and quotes belong to Phase 2; booking and payments belong to Phase 3.

## 2. ADR Status Vocabulary

| Status | Meaning | Allowed use |
|---|---|---|
| Accepted — design | Product/architecture direction agreed; implementation not necessarily verified | Specifications may depend on it |
| Proposed | Preferred design pending evidence or stakeholder decision | No irreversible dependency |
| POC required | Must be demonstrated on pinned dependencies | Blocks dependent implementation |
| Deferred | Explicitly excluded from current milestone | May be revisited |
| Superseded | Replaced by a newer ADR | Preserve historical rationale |
| Rejected | Considered and intentionally not selected | Do not implement without a new ADR |

**Never confuse** “Accepted — design” with “implemented”, “deployed”, “security reviewed” or “migration applied”.

## 3. Decision Ownership and Change Control

Each ADR has an ID, status, context, decision, rationale, consequences, alternatives, implementation notes and verification. Changes to tenant ownership, identity, maintenance lifecycle or document authorization require an ADR update **before** a coding agent or engineer changes the implementation.

Suggested workflow:

1. Open a new ADR or propose a revision.
2. Link the exact requirement and affected modules.
3. Describe at least one plausible alternative.
4. Identify migration, API, security and mobile consequences.
5. Define tests that can falsify the proposed decision.
6. Approve the decision and update linked specifications.
7. Record a supersession relationship rather than deleting history.

## 4. Decision Register

| ADR | Decision | Status | Gate |
|---|---|---|---|
| ADR-001 | Mobile-first React Native + Expo | Accepted — design | M0/M8 |
| ADR-002 | .NET 10 + ABP layered modular monolith | Accepted — design; compatibility POC | M0 |
| ADR-003 | PostgreSQL + EF Core | Accepted — design | M0 |
| ADR-004 | One workspace = one ABP tenant | Accepted — design | M0/M1 |
| ADR-005 | Global identity across multiple ABP tenants | POC required | **M0 blocker** |
| ADR-006 | Explicit tenant membership and authorization | Accepted — design; integration POC | M0/M1 |
| ADR-007 | Workspace selection and tenant resolution | Proposed | M0/M1 |
| ADR-008 | Tenant isolation defense in depth | Accepted — design | M1–M8 |
| ADR-009 | Optional property association | Accepted — design | M3 |
| ADR-010 | Versioned asset-type schemas in JSONB | Accepted — design | M2/M3 |
| ADR-011 | Maintenance lifecycle vs derived due status | Accepted — design | M4 |
| ADR-012 | Recurrence anchoring, timezone and rule edits | Proposed | M4 |
| ADR-013 | Atomic/idempotent maintenance completion | Accepted — design | M4 |
| ADR-014 | Private S3 with presigned upload/finalize | Accepted — design | M5 |
| ADR-015 | Outbox, workers and push delivery | Accepted — design; provider choice open | M0/M6 |
| ADR-016 | Expo push and user/tenant preferences | Accepted — design; policy details open | M6 |
| ADR-017 | API contracts and generated mobile client | Proposed | M0/M1 |
| ADR-018 | Mobile state, storage and tenant cache | Accepted — design | M1/M7 |
| ADR-019 | Archive, retention and deletion policy | Proposed | M3/M8 |
| ADR-020 | SQL naming and migrations | Accepted — design | M0–M8 |
| ADR-021 | Security logging and operational observability | Proposed | M1/M8 |
| ADR-022 | Phase boundaries: provider discovery and booking | Accepted — design | Phase 2/3 |
| ADR-023 | Concurrency mechanism for aggregates | POC required | M1/M3 |
| ADR-024 | PostgreSQL Row-Level Security | Deferred pending evaluation | M8+ |
| ADR-025 | Workspace provisioning and failure recovery | Proposed | M1 |
| ADR-026 | Search and dashboard query strategy | Proposed | M7 |

## 5. Non-negotiable Cross-Document Invariants

- ABP Tenant Management is the authoritative tenant registry; **no duplicate custom `Tenants` table**.
- One workspace maps to one ABP tenant.
- A user can belong to multiple workspaces, with different roles; feasibility must be proven in M0.
- `ICurrentTenant` is a data-scope mechanism, **not proof of membership**.
- Domain `TenantId` is required for workspace-owned data.
- `Asset.PropertyId` is nullable.
- Global asset-type templates are shared read-only; tenant types are scoped.
- Published custom-field schemas are immutable versions.
- Persisted maintenance occurrence lifecycle is only `Open`, `Completed`, `Skipped`, `Cancelled`.
- `Upcoming`, `Due`, `Overdue` are derived display states.
- Mileage comes from explicit readings, never inferred.
- Documents are private S3 objects with authorized metadata/finalization.
- External push delivery is not exactly-once.
- SQL examples use PostgreSQL syntax with PascalCase, uppercase keywords and named constraints.
- Phase 2 is provider discovery/quotes; Phase 3 is booking/payments.

---

# ADR-001 — Mobile-First Client

**Status:** Accepted — design  
**Gate:** M0 mobile shell; M8 beta validation

### Context
Kepnora's primary workflows occur while users inspect vehicles, appliances, equipment and maintenance records. Photos, reminders and quick edits are core mobile interactions.

### Decision
Use **React Native + Expo + TypeScript** for the Phase 1 client, with Expo Router for navigation, TanStack Query for server state, Zustand for limited client state, React Hook Form + Zod for forms, and SecureStore for sensitive session credentials.

### Alternatives considered
Native Swift/Kotlin apps; Flutter; responsive web-first SPA.

### Consequences
A shared cross-platform UI reduces duplicate work but native capabilities and push handling require device testing. Expo library compatibility must be pinned. No full customer web app is required for Phase 1.

### Verification
iOS/Android beta builds; camera/file-picker flow; login refresh; deep links; offline/error states; tenant switch without stale data.

# ADR-002 — Backend Runtime and Modularity

**Status:** Accepted — design; **POC required for actual package compatibility**  
**Gate:** M0

### Context
Kepnora needs authenticated APIs, modular domain boundaries, persistence, background jobs and multi-tenancy without premature distributed deployment.

### Decision
Target **.NET 10 + ASP.NET Core + ABP Framework**, structured as a **layered modular monolith**. Use Domain.Shared, Domain, Application.Contracts, Application, EntityFrameworkCore, HttpApi, HttpApi.Host and DbMigrator layers, adapting names to the scaffolded ABP template.

### Alternatives considered
Custom ASP.NET Core architecture without ABP; microservices; serverless-only backend.

### Consequences
ABP provides conventions and module infrastructure but introduces version, licensing/edition and integration decisions. No code should assume a particular ABP API until the compatible version is pinned and compiled.

### Verification
Scaffold compiles, DbMigrator applies migrations, OpenAPI is emitted, authentication and multi-tenant POC passes. Record exact ABP edition, version, EF Core and Npgsql versions.

# ADR-003 — Primary Database

**Status:** Accepted — design  
**Gate:** M0

### Context
The MVP requires relational integrity for assets, ownership, maintenance history, occurrences and idempotent writes, plus flexible custom fields.

### Decision
Use **PostgreSQL + EF Core** with JSONB for validated custom asset attributes and schema definitions. Keep a single primary database at MVP launch.

### Alternatives considered
SQL Server; document database as primary store; per-tenant database.

### Consequences
PostgreSQL-specific migrations and indexing strategies must be tested. JSONB does not replace schema validation. Tenant-aware relational constraints are essential.

### Verification
Real PostgreSQL integration tests; migration/restore drills; `EXPLAIN (ANALYZE, BUFFERS)` on representative queries.

# ADR-004 — Workspace-to-Tenant Mapping

**Status:** Accepted — design  
**Gate:** M0/M1

### Context
Kepnora requires workspace isolation while ABP already owns a Tenant Management module.

### Decision
**One Kepnora Workspace = one ABP Tenant**. The installed ABP tenant entity and its table are authoritative. Store workspace-specific profile fields as an ABP extension or dedicated 1:1 profile; choose one.

### Rejected alternative
A parallel custom `Tenants` table with independent IDs and lifecycle.

### Consequences
All workspace-owned domain rows use the ABP tenant ID. Migration code must reference actual ABP-generated schema rather than guess table names.

### Verification
Create two workspaces, inspect ABP tenant IDs and FKs, verify no duplicate tenant registry.

# ADR-005 — One Global User Across Multiple Workspaces

**Status:** **POC required — BLOCKING**  
**Gate:** M0, before M1 implementation

### Context
One person may own workspace X and be a member of workspace Y. Standard ABP tenant-local identity, user stores and role grants may not support this directly.

### Preferred direction
Authenticate against one **global subject** and represent access via `TenantMembers(TenantId, UserId, Role, Status)`. Two candidate approaches:

**Option A:** Host/global ABP Identity + OpenIddict, with a custom workspace membership/permission bridge.

**Option B:** External OIDC with stable global subject, mapped into ABP application authorization.

**No option is selected as final until POC evidence exists.**

### Required POC steps
1. Pin compatible ABP version and edition.
2. Create global user U and two ABP tenants X and Y.
3. Log in once as U and access X and Y with distinct permissions.
4. Remove U from Y; verify next protected Y request is denied.
5. Prove ABP permission checks do not accidentally share roles across tenants.
6. Verify refresh tokens, logout and native PKCE flow.
7. Inspect identity schema and define real user FK target for `TenantMembers`.
8. Document tenant resolver and cross-workspace switching behavior.

### Rejected assumption
“ABP Identity automatically handles one global user in many tenants.”

### Consequences
Membership schema, token claims, permission provider and onboarding are blocked until this ADR is resolved. Do not add a fictional global user table solely to satisfy diagrams.

### Acceptance
Recorded demo, compiled code, integration tests and updated ADR status. If both options fail, redesign the identity layer before domain modules proceed.

# ADR-006 — Membership Is Independent of Tenant Context

**Status:** Accepted — design; integration POC required  
**Gate:** M0/M1

### Context
An authenticated request may carry a valid tenant ID that the caller does not own.

### Decision
Maintain explicit active membership and operation permission checks **before** entering tenant-scoped domain operations. `ICurrentTenant` does not authorize access. Owner/Member role semantics are tenant-specific.

### Alternatives considered
Trust tenant header; trust JWT tenant claim; rely only on EF tenant filter; use ABP roles without a separate membership model.

### Consequences
A host/global-safe membership lookup is required. Revocation must be bounded by cache invalidation/TTL. Last Owner removal must be prevented transactionally.

### Verification
User A Owner X/Member Y, user B Owner Y; cross-tenant tests; revoked membership and spoofed route/header tests.

# ADR-007 — Workspace Selection and Tenant Resolution

**Status:** Proposed  
**Gate:** M0/M1

### Context
Mobile users need explicit workspace switching; ABP has tenant resolution conventions that can include headers, domains or query parameters.

### Proposed decision
Use an explicit workspace ID in protected API routes or a verified equivalent. Server resolves the selected workspace, checks membership and permission, then sets `ICurrentTenant.Change(workspaceId)` within a scoped operation.

### Alternatives considered
Subdomains; tenant header; tenant claim only.

### Consequences
Routes and ABP auto-controller behavior must be confirmed from generated OpenAPI. Reject or ignore body-provided `TenantId`; never allow conflicting route/header/body scopes.

### Verification
Spoofed headers, mismatched route/header, stale membership, deep-link access and workspace-switch tests.

# ADR-008 — Defense-in-Depth Tenant Isolation

**Status:** Accepted — design  
**Gate:** M1–M8

### Context
Application authorization alone can fail through raw SQL, workers or overlooked entity relationships.

### Decision
Combine active membership, named permission checks, ABP `IMultiTenant` filters, explicit tenant predicates for sensitive queries and composite `(TenantId, Id)` foreign keys. Tenant-owned rows require non-null `TenantId`.

### Alternatives considered
UUID secrecy; filtering results in the mobile client; single global query with in-memory tenant filtering.

### Consequences
Extra indexes and mapping work. Background workers need trusted tenant context and must restore it after processing. Raw SQL and bulk operations need review.

### Verification
Two-tenant PostgreSQL integration suite, cross-tenant FK rejection, worker context-leak test, search/dashboard isolation tests.

# ADR-009 — Properties Are Optional for Assets

**Status:** Accepted — design  
**Gate:** M3

### Context
Vehicles, computers and other personal assets may not belong to a house or property.

### Decision
`Asset.PropertyId` is nullable. Properties organize assets but are not required during onboarding or asset creation. Tenant ownership remains mandatory.

### Rejected alternative
Create an artificial default property for every standalone asset.

### Consequences
Asset list/filter UI must handle “No property”; FK must be tenant-aware when `PropertyId` is non-null.

### Verification
Create vehicle with null `PropertyId`; cross-tenant property FK rejected; property archive preserves assets.

# ADR-010 — Versioned Asset Types and Custom Fields

**Status:** Accepted — design  
**Gate:** M2/M3

### Context
Asset categories need configurable fields without adding a database column for every type.

### Decision
Use `AssetTypeDefinitions`, immutable `AssetTypeVersions` and JSONB `Assets.CustomFields`. Definitions may be global (`TenantId = NULL`) or tenant-scoped. Validate payloads against the selected published version on the server.

### Alternatives considered
Entity-attribute-value tables; unversioned JSON blobs; fixed schema per category.

### Consequences
Schema publication and evolution require explicit compatibility policy. A foreign key to a version does not alone enforce tenant visibility of its definition.

### Verification
Immutable version tests, invalid JSON fields rejected, tenant custom type denied outside owner tenant, global template read-only.

# ADR-011 — Maintenance Lifecycle and Due Display State

**Status:** Accepted — design  
**Gate:** M4

### Context
A maintenance occurrence has an actual workflow state and a time/mileage-derived urgency state. Combining them creates contradictory data.

### Decision
Persist only **`Open`, `Completed`, `Skipped`, `Cancelled`** as occurrence lifecycle statuses. Compute **`Upcoming`, `Due`, `Overdue`** at read time from rule policy, rule-local date and/or actual mileage reading. Unknown mileage must remain unknown.

### Rejected alternative
Persist `Overdue` as an occurrence lifecycle status updated by a nightly job.

### Consequences
Dashboard, search and reminder planning must share the same due-status calculator. Do not introduce additional persisted statuses without a new ADR and migration.

### Verification
SQL CHECK constraint; clock/timezone tests; due calculation consistency across API and dashboard.

# ADR-012 — Recurrence Anchoring and Time Semantics

**Status:** Proposed  
**Gate:** M4

### Context
Recurring maintenance may follow the original schedule or the date work was actually completed. Month-end dates and daylight-saving transitions create edge cases.

### Proposed decision
Support `Scheduled` and `Completion` anchoring, with deterministic occurrence schedule keys. Use rule-local dates and an explicit timezone. Support time-based or manual mileage-based rules in Phase 1; **hybrid time-or-mileage is not defined by the current proposed schema**.

### Open subdecisions
- Exact monthly clamp policy for Jan 31 → Feb and subsequent months.
- Grace windows for Due/Overdue.
- Handling of user timezone travel and DST.
- Rule edit effect on already-open occurrences.
- Mileage resets/corrections and readings recorded out of order.

### Consequences
The recurrence engine must be a pure, extensively tested domain component. Do not implement date arithmetic independently in the dashboard, worker and mobile client.

### Verification
Deterministic test vectors, month-end/DST tests, duplicate generation, completion anchoring and historical immutability.

# ADR-013 — Atomic and Idempotent Maintenance Completion

**Status:** Accepted — design  
**Gate:** M4

### Context
Mobile retries and concurrent requests must not create duplicate service history or partially complete an occurrence.

### Decision
In one database Unit of Work: validate the open occurrence, insert one service record, transition to `Completed`, update completion anchor when needed and write an outbox event. Enforce uniqueness for a non-null occurrence reference and use an idempotency key or deterministic conflict response.

### Alternatives considered
Independent writes per table; client-side duplicate suppression only.

### Consequences
Concurrency control is mandatory. The record's `AssetId` must match the rule's asset, not merely the same tenant.

### Verification
Two concurrent completion requests produce one record; retry returns prior result or stable conflict; forced rollback leaves no partial history.

# ADR-014 — Private S3 and Two-Step Document Finalization

**Status:** Accepted — design  
**Gate:** M5

### Context
Receipts, photos, warranties and manuals can contain sensitive personal information and are larger than ordinary relational data.

### Decision
Store binaries in **private AWS S3** and metadata in PostgreSQL. The server authorizes an `UploadIntent`, issues a short-lived presigned PUT, verifies uploaded object metadata, then finalizes the document in a database transaction. Downloads require fresh authorization and short-lived presigned GET.

### Alternatives considered
Public S3 objects; binary files in PostgreSQL; arbitrary client-supplied object keys.

### Consequences
S3 and PostgreSQL are not transactionally atomic; orphan cleanup and intent expiration are required. Previously issued URLs remain usable until expiry even after membership removal.

### Open subdecisions
Maximum size, allowed MIME types, URL TTL, virus scanning policy, retention, encryption key policy.

### Verification
Cross-tenant intent/download tests; repeated finalize; orphan cleanup; no public bucket access.

# ADR-015 — Outbox and Durable Workers

**Status:** Accepted — design; **provider selection open**  
**Gate:** M0/M6

### Context
Maintenance completion, occurrence generation, reminder planning and push dispatch require retry and crash recovery.

### Decision
Use durable outbox semantics and idempotent background handlers. **Choose either ABP-supported outbox infrastructure or a custom outbox for a given event stream, not both.** Workers use trusted tenant context and bounded batches.

### Alternatives considered
Fire-and-forget tasks in HTTP handlers; direct push during the completion transaction; duplicate outbox systems.

### Consequences
At-least-once processing is expected; external side effects can duplicate after crash. Leases, backoff, dedup keys and dead-letter handling must be defined.

### Verification
Crash after DB commit; crash after external send; retry; tenant context restoration; outbox backlog recovery.

# ADR-016 — Push Notifications and Preferences

**Status:** Accepted — design; reminder policy proposed  
**Gate:** M6

### Context
Users need maintenance reminders on iOS/Android, with preferences and membership-aware suppression.

### Decision
Use Expo push for the initial mobile implementation. Device registrations belong to the global user; notification preferences and delivery intents belong to a tenant membership. At dispatch, re-check active membership, occurrence status, preferences and quiet hours.

### Open subdecisions
Reminder offsets, repeat cadence, local timezone precedence, quiet-hour boundary, token rotation/retention, notification copy.

### Consequences
Push tokens require protection and redaction. Exactly-once device presentation cannot be guaranteed.

### Verification
Revoked member suppression; completed occurrence suppression; quiet hours; invalid token handling; retry without duplicate intent rows.

# ADR-017 — Application Contracts and OpenAPI

**Status:** Proposed  
**Gate:** M0/M1

### Context
React Native and ABP backend must share stable request/response shapes and error semantics.

### Proposed decision
Define DTOs in Application.Contracts, expose ABP Application Services through confirmed endpoints and generate a typed TypeScript API client from the running host's OpenAPI. Treat generated OpenAPI as the source of truth for routes.

### Alternatives considered
Hand-maintained TypeScript interfaces; handwritten assumed ABP auto-controller URLs.

### Consequences
CI should detect contract drift. Decide how to normalize ABP error envelopes and map 401/403/404/409/422 consistently.

### Verification
Client generation in CI, contract snapshot, error integration tests and one mobile end-to-end flow.

# ADR-018 — Mobile Tenant-Safe State and Secure Storage

**Status:** Accepted — design  
**Gate:** M1/M7

### Context
A user may switch between X and Y while requests are in flight. Cached data from X must not appear in Y.

### Decision
Store session credentials in Expo SecureStore. Prefix TanStack Query keys with workspace ID, partition or reset Zustand tenant state, cancel/invalidate in-flight requests on switch and reauthorize deep links on the server.

### Alternatives considered
Global cache keys; client-side workspace filtering; long-lived sensitive tokens in plain storage.

### Consequences
More explicit state boundaries and switch tests. Mobile state never grants access.

### Verification
Switch X→Y during asset fetch; logout; expired session; revoked membership; deep link to unauthorized asset.

# ADR-019 — Archive, Deletion and Retention

**Status:** Proposed  
**Gate:** M3/M8

### Context
Assets and properties may have historical maintenance records, documents and audit trails. Hard deletion can destroy valuable history.

### Proposed decision
Archive assets and properties by default; preserve immutable/published schema versions and completed maintenance history. Define corrections as auditable actions. Establish retention and account deletion policy before beta.

### Open subdecisions
Document deletion grace period, user erasure vs financial/service history, audit retention, S3 cleanup scheduling and legal requirements.

### Consequences
Soft-delete behavior must be explicit per entity; ABP default filters should not hide historical records unintentionally.

### Verification
Archive property preserves assets; archive asset preserves service history; deletion runbook and restore tests.

# ADR-020 — SQL Style and Migration Ownership

**Status:** Accepted — design  
**Gate:** M0–M8

### Context
The project uses PostgreSQL but prefers SQL formatted similarly to T-SQL.

### Decision
Use PostgreSQL syntax with double-quoted PascalCase identifiers, uppercase keywords, explicit `UUID`, `TIMESTAMPTZ`, `NUMERIC`, named PK/FK/UQ/CK constraints and readable multiline indentation. EF Core migrations are authoritative for domain schema; ABP modules own their platform migrations.

```sql
CREATE INDEX "IX_Assets_TenantId_Name"
    ON "Assets"
    (
        "TenantId",
        "Name"
    )
    WHERE "IsArchived" = FALSE;
```

### Rejected alternative
SQL Server-specific `UNIQUEIDENTIFIER`, `DATETIME2`, `TOP`, `GETUTCDATE()` or hand-recreated ABP platform tables.

### Consequences
Do not copy conceptual SQL snippets into production migrations without reconciling EF Core/ABP mappings and verifying constraint existence.

### Verification
Apply from empty database, inspect generated SQL, run real PostgreSQL constraint tests and rollback/restore drills.

# ADR-021 — Observability and Audit

**Status:** Proposed  
**Gate:** M1/M8

### Context
Cross-tenant incidents, failed uploads, missed reminders and recurrence bugs need traceable evidence.

### Proposed decision
Use structured logs, correlation IDs, audit events and metrics across API and workers. Record actor global ID or service principal, tenant ID, operation, resource ID and outcome; redact tokens, presigned URLs and document contents.

### Alternatives considered
Plain console logging; logs containing full payloads.

### Consequences
Define retention and access controls. Audit log access must itself be tenant-scoped or explicitly host-authorized.

### Verification
Trace one asset mutation and one worker dispatch end to end; demonstrate redaction and incident query.

# ADR-022 — Phase 1, 2 and 3 Boundaries

**Status:** Accepted — design  
**Gate:** Roadmap

### Context
Adding marketplace, booking and payments before core asset/maintenance reliability would broaden the MVP and destabilize dependencies.

### Decision
**Phase 1:** personal asset management, optional properties, maintenance, documents, reminders, dashboard, search, settings.  
**Phase 2:** service provider discovery, provider profiles, service areas and quote/contact workflows (exact scope to be specified separately).  
**Phase 3:** booking, payment and more advanced provider operations.

### Consequences
Phase 2 design can start during Phase 1, but Phase 2 implementation should not displace M0–M8 critical gates. The earlier estimate of 4–6 months for Phase 1 is a planning estimate, **not a committed release date**.

### Verification
Feature scope reviews and separate Phase 2 requirements/ADRs before provider implementation.

# ADR-023 — Optimistic Concurrency Strategy

**Status:** POC required  
**Gate:** M1/M3

### Context
Concurrent updates to assets, memberships and maintenance occurrences can overwrite data or violate last-owner/completion invariants.

### Candidate approaches
**A:** ABP concurrency stamps mapped through supported EF Core conventions.  
**B:** Application-managed numeric version token with explicit EF concurrency mapping.

### Decision
**Not yet selected.** Choose one strategy per aggregate; do not accidentally require two independent tokens.

### Consequences
DTOs need expected-version semantics and stable 409 responses. Last-owner removal requires a transactional invariant beyond a simple optimistic check on one row.

### Verification
Concurrent asset edit; double completion; simultaneous last-owner removal; client conflict recovery.

# ADR-024 — PostgreSQL Row-Level Security

**Status:** Deferred pending evaluation  
**Gate:** Security hardening, not assumed for MVP

### Context
RLS could add database-level tenant isolation beyond ABP filters and composite FKs.

### Decision
Do not claim RLS is enabled. Evaluate it after proving transaction-scoped tenant session state, connection pooling behavior, migration role behavior and worker safety.

### Alternatives considered
ABP-only filters; separate database per tenant.

### Consequences
A partially configured RLS policy can block legitimate migrations or allow privileged bypass. Adoption requires a separate ADR and production tests.

### Verification
Connection reuse tests, role separation, worker queries and bypass attempts.

# ADR-025 — Workspace Provisioning and Recovery

**Status:** Proposed  
**Gate:** M1

### Context
Creating an ABP tenant and writing a membership may cross different services or transaction boundaries. A crash between steps can leave an orphaned workspace.

### Proposed decision
Use an idempotent provisioning operation keyed by the global user and request key, with explicit `Pending`, `Ready`, `Failed`/recovery semantics if a single transaction is not possible. Reconcile tenant creation, workspace profile and first Owner membership.

### Alternatives considered
Fire sequential calls and assume success; create a second tenant table to track progress.

### Consequences
Recovery and compensation must be implemented without deleting unrelated ABP tenant state. Never mark onboarding complete until membership exists and access works.

### Verification
Crash after tenant creation; repeated request; concurrent creation; membership failure; retry to one Ready workspace.

# ADR-026 — Tenant-Scoped Search and Dashboard

**Status:** Proposed  
**Gate:** M7

### Context
Dashboard and search aggregate data across assets, properties and maintenance. Cross-tenant leakage and inconsistent due counts are risks.

### Proposed decision
Run tenant-scoped PostgreSQL queries and use the shared maintenance due-status calculator. Use indexes based on measured queries; consider full-text/trigram search only when justified.

### Alternatives considered
Fetch all tenant rows into mobile memory; global search then client filter; independently calculate overdue state in UI.

### Consequences
Query plan testing and pagination are required. No materialized or cached view may omit tenant scope.

### Verification
X/Y search isolation; dashboard totals at timezone boundaries; `EXPLAIN (ANALYZE, BUFFERS)` with representative tenant sizes.

---

## 6. Cross-Document Consistency Matrix

| Concern | Canonical ADR | Supporting spec | Must never appear elsewhere |
|---|---|---|---|
| ABP tenant ownership | ADR-004 | Database overview | Custom parallel tenant registry |
| Global user identity | ADR-005 | Tenant isolation | “ABP supports this out of the box” |
| Authorization | ADR-006/008 | Tenant isolation | `ICurrentTenant` as proof of membership |
| Standalone assets | ADR-009 | Database overview | Required `PropertyId` |
| Dynamic fields | ADR-010 | Database overview | Mutable published schema |
| Maintenance lifecycle | ADR-011 | Database overview | Persisted `Due`/`Overdue` |
| Maintenance recurrence | ADR-012/013 | MVP build order | Unspecified date drift or duplicate completion |
| S3 | ADR-014 | Architecture overview | Public bucket or unverified finalize |
| Push | ADR-015/016 | MVP build order | Exactly-once external delivery claim |
| Mobile cache | ADR-018 | Tenant isolation | Cache without workspace key |
| SQL | ADR-020 | Database overview | SQL Server-specific DDL |
| Roadmap | ADR-022 | Project context | Booking/payments in Phase 1 |

## 7. Required Architecture Proofs Before Feature Implementation

**M0 — mandatory:** Compile pinned ABP + .NET 10; inspect ABP tenant and identity schema; demonstrate global user U in tenant X and Y; demonstrate different permissions and revocation; choose auth provider and tenant resolver; choose outbox provider.

**M1 — mandatory:** Idempotent workspace provisioning, active membership check on every protected operation, last-owner protection, tenant-switch API and cache contract.

**M2–M4 — mandatory:** Immutable asset schema versions, composite tenant constraints, nullable property, recurrence test vectors and idempotent completion.

**M5–M6 — mandatory:** Private S3 finalize and cleanup, safe worker tenant context, recipient membership revalidation and push retry semantics.

**M7–M8 — mandatory:** Tenant-scoped dashboard/search, operational restore tests, cross-tenant security suite, mobile beta checks.

## 8. Proposed ADR File Split (Optional Later)

This consolidated file is the **single source of truth** for now. If the ADR count grows, split into `ADR-001-mobile-client.md`, `ADR-002-backend-abp.md`, etc., but keep the register and statuses synchronized. Do not maintain two conflicting copies of the same decision.

## 9. Architecture Review Checklist

- [ ] Every Accepted decision has an explicit verification method.
- [ ] Every POC-required decision blocks dependent implementation.
- [ ] No ABP table name or API behavior is asserted without version-specific proof.
- [ ] Membership, tenant context and permission are distinguished.
- [ ] Cross-tenant references are checked in services and relational constraints.
- [ ] No maintenance lifecycle/due-state conflation exists.
- [ ] S3 and push external consistency limitations are documented.
- [ ] Mobile tenant cache behavior is specified.
- [ ] PostgreSQL SQL syntax and migration ownership are consistent.
- [ ] Phase 2 and Phase 3 functionality remain outside Phase 1 delivery.
- [ ] Changes update affected docs and tests together.

## 10. Final Decision Principle

**Preserve product invariants, expose unresolved framework assumptions, and require executable proof before irreversible architecture choices.** Kepnora's success depends more on correct identity, tenant authorization, durable maintenance state and recoverable external workflows than on early feature breadth.
