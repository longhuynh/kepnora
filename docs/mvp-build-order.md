# Kepnora MVP Build Order

**Version:** 1.0-draft  
**Status:** Implementation planning specification; ABP identity and version compatibility are pending proof of concept  
**Stack:** .NET 10, ABP Framework, EF Core, PostgreSQL, React Native / Expo, AWS private S3  
**Related documents:** `README.md`, `kepnora-project-context.md`, `architecture-overview.md`, `database-overview.md`

---

# 1. Purpose

This document defines the **implementation sequence, dependencies, delivery contracts, acceptance criteria and release gates** for Kepnora Phase 1. It follows the depth and bottom-up organization of the CoreLyxAI MVP build-order reference, but uses Kepnora's asset, maintenance, document and notification requirements.

The primary goal is to prevent architectural rework. A working mobile screen is not evidence of a completed milestone until its authorization, persistence, negative tests, observability and recovery behavior have been verified.

**Important:** A single global user must be able to belong to multiple workspaces. This is a product requirement, **not** a capability we assume standard ABP Identity provides without customization. The identity/multi-tenant proof of concept is an explicit release-blocking prerequisite.

# 2. Guiding Principle

Build stable contracts and persistence first; then business workflows; then dependent experiences.

```text
ABP compatibility and identity POC
        |
        v
Tenant/workspace authorization + migration baseline
        |
        v
Properties + asset types + assets
        |
        v
Maintenance rules + occurrence generation + history
        |
        +--------------------+
        |                    |
        v                    v
Documents + S3         Notifications + outbox
        |                    |
        +---------+----------+
                  |
                  v
Dashboard + search + settings
                  |
                  v
Hardening + release candidate
```

Do not build the dashboard's Due/Overdue counts before the recurrence engine defines their meaning. Do not issue presigned S3 URLs before resource ownership checks exist. Do not add background jobs that can bypass tenant isolation.

# 3. Definition of Done

A task is complete only when all applicable items below are demonstrated:

- Source code merged with review; project builds using pinned dependencies.
- Database migration generated, reviewed, applied and rollback/forward-fix tested where appropriate.
- ABP Application Service has an explicit permission and tenant authorization boundary.
- OpenAPI contract and generated TypeScript client match the deployed API.
- Validation errors, concurrency conflicts and cross-tenant requests have stable responses.
- Automated unit and integration tests pass with a real PostgreSQL instance.
- Mobile screen covers loading, empty, success, validation, offline and error states.
- Structured logs contain correlation IDs and do not expose secrets or private S3 URLs.
- Repeated requests and worker retries do not create duplicate business effects.
- Documentation and ADRs are updated when implementation decisions change.
- A reproducible milestone demo and evidence artifact are recorded.

# 4. Invariants That Must Not Drift

| ID | Invariant | Verification |
|---|---|---|
| INV-01 | One Kepnora workspace maps to exactly one ABP tenant | Schema inspection and provisioning test |
| INV-02 | `ICurrentTenant` scopes data but does not prove membership | Unauthorized tenant switching test |
| INV-03 | One global user may join multiple workspaces | Two-workspace end-to-end POC |
| INV-04 | No custom parallel `Tenants` registry | Migration schema assertion |
| INV-05 | Tenant-owned domain rows require the same ABP `TenantId` | Composite FK and service tests |
| INV-06 | `Asset.PropertyId` is nullable | Standalone vehicle asset test |
| INV-07 | Published asset-type schema versions are immutable | Update rejection test |
| INV-08 | Occurrence persisted status is Open/Completed/Skipped/Cancelled | SQL constraint and DTO tests |
| INV-09 | Upcoming/Due/Overdue are derived display states | Clock/timezone tests |
| INV-10 | Mileage is recorded, never inferred | No-reading and correction tests |
| INV-11 | Maintenance completion is atomic and idempotent | Concurrent completion test |
| INV-12 | S3 objects are private and finalized after validation | Unauthorized access and orphan cleanup tests |
| INV-13 | Push delivery is at-least-once/best-effort, not exactly once | Crash/retry simulation |
| INV-14 | React Native cache is partitioned by workspace | Tenant-switch stale-cache test |
| INV-15 | PostgreSQL uses T-SQL-like formatting, not SQL Server syntax | SQL review and migration execution |

# 5. Dependency and Milestone Overview

| Milestone | Name | Depends on | Primary deliverable | Exit gate |
|---|---|---|---|---|
| M0 | ABP technical foundation and identity POC | None | Compiling solution + multi-workspace proof | G0 |
| M1 | Authentication, workspace and security | M0 | Authorized workspace lifecycle | G1 |
| M2 | Properties and versioned asset types | M1 | Stable property/type contracts | G2 |
| M3 | Asset inventory | M2 | Complete asset CRUD and search basics | G3 |
| M4 | Maintenance scheduling and history | M3 | Deterministic recurrence and completion | G4 |
| M5 | Documents and private S3 | M3, M1 | Safe upload/finalize/download | G5 |
| M6 | Notifications and durable jobs | M4, M1 | Tenant-safe reminder delivery | G6 |
| M7 | Dashboard, search, settings | M3–M6 | Complete mobile MVP workflows | G7 |
| M8 | Security hardening and beta | M0–M7 | Release candidate | G8 |

**Parallelization:** M5 can begin after M3 while M4 is underway. M6 must wait for maintenance lifecycle semantics. The mobile shell can begin during M0, but business screens should not lock unstable API contracts.

# 6. Source-of-Truth and Version Policy

The domain rules are defined by `database-overview.md` and feature specifications; this build order governs **when** each rule is implemented and verified. The ABP release, edition, Identity/OpenIddict strategy, permissions and job provider are not final until M0's POC succeeds.

Do not mix ABP-generated tenant/identity tables with custom duplicate tables. Do not treat the sample SQL in database documentation as a migration that has already been compiled. Every milestone records exact ABP/EF Core/Npgsql/Expo package versions in the repository lock files and an ADR.

# 7. Repository and CI Baseline

```text
kepnora/
  backend/
    src/
      Kepnora.Domain.Shared/
      Kepnora.Domain/
      Kepnora.Application.Contracts/
      Kepnora.Application/
      Kepnora.EntityFrameworkCore/
      Kepnora.HttpApi/
      Kepnora.HttpApi.Host/
      Kepnora.DbMigrator/
    test/
      Kepnora.Domain.Tests/
      Kepnora.Application.Tests/
      Kepnora.EntityFrameworkCore.Tests/
      Kepnora.HttpApi.Tests/
  mobile/
    app/
    src/
      api/
      auth/
      features/
      navigation/
      state/
      ui/
  docs/
  infra/
  .github/workflows/
```

This is a **proposed layered ABP structure**. Use the actual scaffolded ABP template and version-specific generated project names as the final authority. Avoid duplicating built-in ABP abstractions simply to match a diagram.

## 7.1 CI minimum

1. Restore dependencies with locked versions.
2. Build backend in Release mode and run static analysis.
3. Run domain/application unit tests.
4. Start disposable PostgreSQL and apply migrations from zero.
5. Run cross-tenant and persistence integration tests.
6. Generate OpenAPI; detect breaking contract drift.
7. Typecheck and lint Expo TypeScript.
8. Run mobile unit/component tests.
9. Scan dependencies and secrets.
10. Publish test results and migration/contract artifacts.

# 8. API and Error Contract Rules

Application services expose stable DTOs and permission policies. Whether ABP auto-controller endpoints are used or explicit controllers are written is decided in M0; document actual routes after OpenAPI generation.

| HTTP status | Meaning | Example |
|---|---|---|
| 400 | Invalid input | Unsupported interval or custom field |
| 401 | Unauthenticated | Missing/expired access token |
| 403 | Authenticated but not authorized | No active workspace membership |
| 404 | Resource absent or intentionally concealed | Asset not in authorized workspace |
| 409 | State/concurrency conflict | Occurrence already completed |
| 413 | Upload exceeds permitted size | Oversized receipt |
| 422 | Valid JSON, invalid business rule | Incompatible asset type version |
| 429 | Rate limit | Excessive upload intent requests |
| 500 | Unexpected server error | Correlation ID logged |

**Note:** ABP may emit a framework-specific error envelope. Normalize the generated OpenAPI and mobile error mapper against the actual ABP behavior; do not invent an envelope that silently conflicts with ABP defaults.

# 9. Cross-Cutting Test Data

Create seeded fixtures for:

- User A: Owner of workspace X, Member of workspace Y.
- User B: Owner of workspace Y, no membership in X.
- User C: authenticated but not a member of X or Y.
- Property X1 in workspace X and Property Y1 in workspace Y.
- Asset X1 attached to Property X1; Asset X2 with `PropertyId = NULL`.
- Global vehicle type and tenant-specific custom type.
- Time rule at month end, completion-anchored rule and mileage rule without a reading.
- Open and completed occurrences; receipts with valid/invalid S3 metadata.
- Active and revoked device tokens, quiet-hours preferences.
- Outbox rows pending, failed and already processed.

Every endpoint and background handler must be exercised against these fixtures where relevant.


# 10. M0 — ABP Foundation and Multi-Workspace Identity POC

**Dependencies:** None; this is the blocking architecture gate.  
**Exit gate:** G0.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M0.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M0-01 | Pin a compatible ABP edition/version, .NET 10, EF Core and Npgsql; scaffold and compile the solution. | PR, test and demo reference |


| M0-02 | Choose ABP Identity/OpenIddict or external OIDC; implement Authorization Code + PKCE for native mobile. | PR, test and demo reference |


| M0-03 | Prove a single global login subject can access workspaces X and Y with different roles. | PR, test and demo reference |


| M0-04 | Inspect actual ABP Tenant Management and Identity migrations; prohibit a parallel tenant registry. | PR, test and demo reference |


| M0-05 | Prototype membership-aware authorization independent of `ICurrentTenant`. | PR, test and demo reference |


| M0-06 | Prototype workspace selection/resolution, token refresh and membership revocation. | PR, test and demo reference |


| M0-07 | Set up DbMigrator, Docker PostgreSQL, CI and OpenAPI generation. | PR, test and demo reference |


| M0-08 | Decide whether to use ABP distributed event outbox or a custom outbox; document provider choice. | PR, test and demo reference |


## M0.2 Required deliverables

- Compiling ABP host and DbMigrator
- Identity/tenant proof-of-concept test suite
- Pinned package versions and ADRs
- Minimal authenticated mobile shell

## M0.3 Acceptance criteria

- **AC-M0-01:** Same user logs in once and can read X and Y only after membership checks.
- **AC-M0-02:** Role in X does not grant elevated permission in Y.
- **AC-M0-03:** Spoofed tenant header/route cannot access workspace Z.
- **AC-M0-04:** ABP schema contains one authoritative tenant registry.
- **AC-M0-05:** Revoking Y membership blocks the next protected request.
- **AC-M0-06:** CI applies migrations to an empty PostgreSQL database.

## M0.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M0.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G0 passed solely because UI screens look complete.**


# 11. M1 — Authentication, Workspace Provisioning and Authorization

**Dependencies:** M0.  
**Exit gate:** G1.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M1.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M1-01 | Implement login callback/session storage, refresh and sign-out in Expo SecureStore. | PR, test and demo reference |


| M1-02 | Implement workspace listing, selection and validated switching. | PR, test and demo reference |


| M1-03 | Create workspace through ABP Tenant Management API with idempotent provisioning. | PR, test and demo reference |


| M1-04 | Persist membership and workspace profile, including compensating recovery after partial failure. | PR, test and demo reference |


| M1-05 | Implement Owner/Member permission matrix and last-owner protection. | PR, test and demo reference |


| M1-06 | Add ABP Application Service authorization, tenant context and resource checks. | PR, test and demo reference |


| M1-07 | Create permission/membership test fixtures and audit logs. | PR, test and demo reference |


| M1-08 | Define onboarding flow that allows users to skip adding a property. | PR, test and demo reference |


## M1.2 Required deliverables

- Workspace membership schema and verified foreign keys
- Identity/session DTOs and generated client
- Workspace AppServices and onboarding screens
- Authorization integration tests

## M1.3 Acceptance criteria

- **AC-M1-01:** A newly registered user gets exactly one personal workspace after retry.
- **AC-M1-02:** A user can switch between two authorized workspaces without token or cache bleed.
- **AC-M1-03:** Member cannot manage workspace members; Owner can.
- **AC-M1-04:** Removing last active Owner is rejected transactionally.
- **AC-M1-05:** An inactive member cannot read tenant assets.
- **AC-M1-06:** No property is required to complete onboarding.

## M1.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M1.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G1 passed solely because UI screens look complete.**


# 12. M2 — Properties and Versioned Asset Types

**Dependencies:** M1.  
**Exit gate:** G2.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M2.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M2-01 | Implement Property aggregate, archive and list/detail AppServices. | PR, test and demo reference |


| M2-02 | Add property address/timezone validation and tenant indexes. | PR, test and demo reference |


| M2-03 | Create global asset-type templates and tenant-scoped custom definitions. | PR, test and demo reference |


| M2-04 | Implement immutable asset-type version publishing with JSON Schema validation. | PR, test and demo reference |


| M2-05 | Implement type catalog query exposing global plus current-tenant definitions. | PR, test and demo reference |


| M2-06 | Generate OpenAPI DTOs and mobile property/type forms. | PR, test and demo reference |


| M2-07 | Prevent tenant edits to global templates and cross-tenant custom type access. | PR, test and demo reference |


| M2-08 | Seed realistic Home, Vehicle, Appliance and Electronics templates. | PR, test and demo reference |


## M2.2 Required deliverables

- Properties and asset-type EF Core migrations
- Property/type AppServices and DTOs
- React Native property list/detail and type selector
- Schema version validation tests

## M2.3 Acceptance criteria

- **AC-M2-01:** Property X cannot be referenced by tenant Y.
- **AC-M2-02:** Property archive preserves related records.
- **AC-M2-03:** Global template is readable by X and Y but not editable by members.
- **AC-M2-04:** Published type version cannot be modified.
- **AC-M2-05:** Invalid required custom fields are rejected server-side.
- **AC-M2-06:** Property timezone is validated and normalized.

## M2.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M2.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G2 passed solely because UI screens look complete.**


# 13. M3 — Asset Inventory

**Dependencies:** M2.  
**Exit gate:** G3.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M3.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M3-01 | Implement Asset aggregate with nullable PropertyId and tenant-scoped foreign keys. | PR, test and demo reference |


| M3-02 | Validate asset type/version and custom JSONB fields on create/update. | PR, test and demo reference |


| M3-03 | Implement asset list, detail, create, edit, archive and restore policies. | PR, test and demo reference |


| M3-04 | Support standalone cars and electronics without a property. | PR, test and demo reference |


| M3-05 | Add deterministic pagination, sorting and basic search indexes. | PR, test and demo reference |


| M3-06 | Build React Native asset forms, lists, details and empty states. | PR, test and demo reference |


| M3-07 | Implement optimistic concurrency and structured validation errors. | PR, test and demo reference |


| M3-08 | Verify same-tenant relationship constraints at database and service layers. | PR, test and demo reference |


## M3.2 Required deliverables

- Assets migration, indexes and constraints
- Asset AppServices and OpenAPI contracts
- React Native asset inventory screens
- Cross-tenant and concurrency integration tests

## M3.3 Acceptance criteria

- **AC-M3-01:** Standalone vehicle is created with null PropertyId.
- **AC-M3-02:** Asset X cannot attach to Property Y.
- **AC-M3-03:** Asset cannot use another tenant's custom type.
- **AC-M3-04:** Two conflicting updates produce a stable conflict.
- **AC-M3-05:** Archive preserves history and hides asset from active list.
- **AC-M3-06:** Mobile list refreshes after create/edit/archive.

## M3.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M3.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G3 passed solely because UI screens look complete.**


# 14. M4 — Maintenance Rules, Occurrences, Mileage and History

**Dependencies:** M3.  
**Exit gate:** G4.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M4.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M4-01 | Define time intervals, month-end clamping, timezone and DST policy in ADR. | PR, test and demo reference |


| M4-02 | Implement Scheduled and Completion anchoring with deterministic schedule keys. | PR, test and demo reference |


| M4-03 | Implement manual mileage readings and mileage due calculations without guessing. | PR, test and demo reference |


| M4-04 | Generate occurrences idempotently and enforce unique rule/schedule key. | PR, test and demo reference |


| M4-05 | Persist only Open, Completed, Skipped and Cancelled lifecycle values. | PR, test and demo reference |


| M4-06 | Compute Upcoming/Due/Overdue at read time with rule-local dates and grace policy. | PR, test and demo reference |


| M4-07 | Complete an occurrence in one Unit of Work: record, status, anchor and outbox. | PR, test and demo reference |


| M4-08 | Handle concurrent completion, corrections, rule edits and historical integrity. | PR, test and demo reference |


| M4-09 | Build maintenance list, rule editor, completion and history mobile flows. | PR, test and demo reference |


| M4-10 | Add clock-driven and property-based recurrence tests. | PR, test and demo reference |


## M4.2 Required deliverables

- Maintenance migrations and indexes
- Recurrence domain engine and generation worker
- Maintenance AppServices, DTOs and mobile screens
- Deterministic test vectors and concurrency tests

## M4.3 Acceptance criteria

- **AC-M4-01:** Monthly Jan 31 rule clamps correctly without subsequent drift.
- **AC-M4-02:** Duplicate generation cannot create duplicate occurrences.
- **AC-M4-03:** Missing mileage reading yields Unknown, not an invented due state.
- **AC-M4-04:** Overdue cannot be persisted as lifecycle status.
- **AC-M4-05:** Two concurrent completions create exactly one service record.
- **AC-M4-06:** Completion-anchored next occurrence follows actual completion.
- **AC-M4-07:** Rule edits never silently rewrite completed history.

## M4.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M4.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G4 passed solely because UI screens look complete.**


# 15. M5 — Documents, Photos, Receipts and Private S3

**Dependencies:** M3 and M1.  
**Exit gate:** G5.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M5.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M5-01 | Choose S3 bucket layout, encryption, retention and presigned URL TTL. | PR, test and demo reference |


| M5-02 | Create UploadIntent and Document metadata entities and migrations. | PR, test and demo reference |


| M5-03 | Implement authorization before issuing upload/download presigned URLs. | PR, test and demo reference |


| M5-04 | Validate file size, content type, object metadata and ownership at finalize. | PR, test and demo reference |


| M5-05 | Ensure repeated finalize returns the same document or deterministic conflict. | PR, test and demo reference |


| M5-06 | Implement expired intent and orphaned object cleanup with safe retries. | PR, test and demo reference |


| M5-07 | Build mobile photo/receipt/manual/warranty picker and upload progress. | PR, test and demo reference |


| M5-08 | Add security tests for cross-tenant object keys and revoked membership. | PR, test and demo reference |


## M5.2 Required deliverables

- S3 configuration and IAM policy
- UploadIntent/Document migrations and AppServices
- Mobile upload/download components
- S3 integration and cleanup tests

## M5.3 Acceptance criteria

- **AC-M5-01:** No public ACL or public bucket access is enabled.
- **AC-M5-02:** Tenant X cannot presign or finalize tenant Y's object.
- **AC-M5-03:** A failed upload does not create a finalized document.
- **AC-M5-04:** Repeated finalize never creates duplicate metadata.
- **AC-M5-05:** Expired orphan objects are safely cleaned.
- **AC-M5-06:** Download requires active membership at request time.

## M5.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M5.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G5 passed solely because UI screens look complete.**


# 16. M6 — Push Reminders and Durable Background Processing

**Dependencies:** M4 and M1.  
**Exit gate:** G6.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M6.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M6-01 | Choose ABP jobs/event outbox provider and document failure guarantees. | PR, test and demo reference |


| M6-02 | Implement tenant-scoped notification preferences and user-global device registration. | PR, test and demo reference |


| M6-03 | Generate reminder delivery intents idempotently from open occurrences. | PR, test and demo reference |


| M6-04 | Enforce quiet hours, timezone, due windows and opt-out policies. | PR, test and demo reference |


| M6-05 | Dispatch through a retryable worker with leases and backoff. | PR, test and demo reference |


| M6-06 | Re-check membership, preference and occurrence state immediately before send. | PR, test and demo reference |


| M6-07 | Handle invalid Expo push tokens and provider errors without logging secrets. | PR, test and demo reference |


| M6-08 | Build mobile push opt-in, preferences and notification navigation. | PR, test and demo reference |


| M6-09 | Add crash-after-send, duplicate delivery and tenant context leak tests. | PR, test and demo reference |


## M6.2 Required deliverables

- Notification and delivery migrations
- Durable worker and outbox integration
- Expo push registration and settings UI
- Retry, dedup and security tests

## M6.3 Acceptance criteria

- **AC-M6-01:** One planned reminder intent per dedup key.
- **AC-M6-02:** Completed occurrence is suppressed before dispatch.
- **AC-M6-03:** Revoked membership prevents future sends.
- **AC-M6-04:** A worker switching X to Y does not leak tenant context.
- **AC-M6-05:** Provider failures retry without duplicate database intent.
- **AC-M6-06:** Documentation does not claim exactly-once push delivery.

## M6.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M6.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G6 passed solely because UI screens look complete.**


# 17. M7 — Dashboard, Search and Settings

**Dependencies:** M3–M6.  
**Exit gate:** G7.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M7.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M7-01 | Define tenant-scoped dashboard counts using derived maintenance statuses. | PR, test and demo reference |


| M7-02 | Implement dashboard aggregation with measured PostgreSQL queries. | PR, test and demo reference |


| M7-03 | Implement asset/property/maintenance search with pagination and tenant filters. | PR, test and demo reference |


| M7-04 | Add profile, workspace switch, timezone and notification preference settings. | PR, test and demo reference |


| M7-05 | Implement cache key partitioning and invalidation by active workspace. | PR, test and demo reference |


| M7-06 | Build dashboard, global search and settings React Native screens. | PR, test and demo reference |


| M7-07 | Add deep-link authorization and offline/empty/error states. | PR, test and demo reference |


| M7-08 | Record EXPLAIN ANALYZE baseline for representative large tenant fixtures. | PR, test and demo reference |


## M7.2 Required deliverables

- Dashboard/search/settings AppServices
- Indexes and query plans
- Mobile dashboard, search and settings screens
- Cache isolation and performance tests

## M7.3 Acceptance criteria

- **AC-M7-01:** Dashboard counts match maintenance engine at timezone boundaries.
- **AC-M7-02:** Search never returns another tenant's data.
- **AC-M7-03:** Switching workspace clears or partitions cached queries.
- **AC-M7-04:** Deep link to unauthorized asset is denied.
- **AC-M7-05:** Large tenant queries stay within documented latency budget.
- **AC-M7-06:** Offline state is explicit; stale results are labeled.

## M7.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M7.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G7 passed solely because UI screens look complete.**


# 18. M8 — Hardening, Operational Readiness and Beta

**Dependencies:** All previous milestones.  
**Exit gate:** G8.  
**Demo owner:** Backend + Mobile + QA; one person may fill multiple roles in an early-stage project.

## M8.1 Implementation task breakdown

| Task ID | Work item | Evidence |
|---|---|---|


| M8-01 | Run full authorization matrix and direct-object-reference penetration tests. | PR, test and demo reference |


| M8-02 | Run migrations from empty database and rehearsal on representative data. | PR, test and demo reference |


| M8-03 | Exercise backups, restore, rollback/forward-fix and tenant incident procedures. | PR, test and demo reference |


| M8-04 | Test background worker crash recovery and S3 orphan cleanup. | PR, test and demo reference |


| M8-05 | Review data retention, account deletion, secrets, IAM and audit logs. | PR, test and demo reference |


| M8-06 | Run mobile release builds, crash reporting and accessibility checks. | PR, test and demo reference |


| M8-07 | Load-test asset list, dashboard, recurrence generation and reminder throughput. | PR, test and demo reference |


| M8-08 | Publish OpenAPI contract snapshot and feature consistency report. | PR, test and demo reference |


| M8-09 | Complete beta checklist, support runbooks and known-limitations register. | PR, test and demo reference |


## M8.2 Required deliverables

- Release candidate build and signed artifacts
- Security/performance/restore test evidence
- Runbooks, monitoring dashboards and support procedures
- Approved beta go/no-go record

## M8.3 Acceptance criteria

- **AC-M8-01:** No known critical cross-tenant access defect.
- **AC-M8-02:** Backup restore and migration rehearsal pass.
- **AC-M8-03:** No duplicate maintenance completion under concurrency.
- **AC-M8-04:** All feature flows pass on iOS and Android test devices.
- **AC-M8-05:** Known limitations and pending product policies are documented.
- **AC-M8-06:** Go/no-go owners sign off with evidence links.

## M8.4 Negative and failure tests

- An authenticated user without membership cannot access a workspace-scoped resource.
- Retried or duplicated requests do not create unintended duplicate business effects.
- A failure after the first persisted step is recoverable without orphaned domain state.
- Invalid payloads return deterministic client-visible errors without internal exception leakage.
- Mobile displays a retry/empty/error state without claiming a failed operation succeeded.
- Worker and administrative paths are covered where this milestone introduces them.

## M8.5 Exit gate evidence

Record the exact commit SHA, migration ID, OpenAPI snapshot, test report, demo steps, known limitations and any newly approved ADR. **Do not mark G8 passed solely because UI screens look complete.**


# 19. Milestone Dependency Matrix

| Component | M0 | M1 | M2 | M3 | M4 | M5 | M6 | M7 | M8 |
|---|---|---|---|---|---|---|---|---|---|
| ABP identity and tenant POC | Build | Use | Use | Use | Use | Use | Use | Use | Audit |
| Tenant membership authorization | Prototype | Build | Use | Use | Use | Use | Use | Use | Audit |
| EF Core migrations | Scaffold | Membership | Types | Assets | Maintenance | Documents | Notifications | Indexes | Verify |
| React Native navigation/session | Shell | Auth | Properties | Assets | Maintenance | Documents | Push | Dashboard | Release |
| OpenAPI-generated client | Prototype | Auth | Types | Assets | Maintenance | Documents | Notifications | Search | Freeze |
| Outbox and workers | Choose | Foundation | — | — | Events | Cleanup | Dispatch | — | Recover |
| Security negative tests | Start | Expand | Expand | Expand | Expand | Expand | Expand | Expand | Full suite |

# 20. Migration Order and Database Ownership

The exact physical schema is owned by compiled EF Core migrations and installed ABP modules. The proposed sequence below is **not permission to create custom ABP platform tables**.

```text
ABP platform migrations (Tenant Management / Identity / Permission Management)
    ↓
TenantMembers + WorkspaceProfiles
    ↓
Properties + AssetTypeDefinitions + AssetTypeVersions
    ↓
Assets
    ↓
MaintenanceRules + MaintenanceOccurrences
    ↓
MaintenanceRecords + AssetOdometerReadings
    ↓
UploadIntents + Documents
    ↓
DeviceRegistrations + NotificationPreferences + ReminderDeliveries
    ↓
Chosen outbox/idempotency infrastructure
    ↓
Indexes, seed data, performance fixtures
```

Before applying a migration, check:

1. It references the actual ABP tenant table and selected global identity key.
2. Every tenant-owned foreign key preserves tenant ownership.
3. It does not create `Tenants` in parallel with ABP Tenant Management.
4. It uses PostgreSQL-compatible SQL with PascalCase identifiers.
5. `MaintenanceOccurrences.LifecycleStatus` cannot store derived due states.
6. A rollback or safe forward-fix exists for production data.
7. It is tested against an empty and a previously migrated database.

## 20.1 SQL style example

```sql
CREATE UNIQUE INDEX "UX_MaintenanceOccurrences_Rule_ScheduleKey"
    ON "MaintenanceOccurrences"
    (
        "TenantId",
        "RuleId",
        "ScheduleKey"
    );

SELECT
    O."Id",
    O."DueOn",
    O."LifecycleStatus"
FROM "MaintenanceOccurrences" AS O
WHERE O."TenantId" = @TenantId
  AND O."LifecycleStatus" = 'Open'
ORDER BY
    O."DueOn",
    O."Id";
```

The `@TenantId` placeholder is illustrative; parameter binding must use the actual Npgsql/EF Core API. Do not use SQL Server-specific functions.

# 21. API Contract Delivery Workflow

For each feature:

1. Agree on the Application Service responsibility and permission requirement.
2. Define request and response DTOs in `Application.Contracts`.
3. Define validation, idempotency and concurrency behavior.
4. Implement domain rules and EF Core persistence.
5. Expose the actual ABP API endpoint and generate OpenAPI.
6. Regenerate the TypeScript client from the real contract.
7. Implement mobile screen against generated types.
8. Run positive, validation, unauthorized, cross-tenant and retry tests.
9. Freeze a contract snapshot at milestone exit.

**Do not handwrite assumed ABP routes and then claim they are framework defaults.** Use OpenAPI from the running host as the final endpoint catalog.

# 22. Domain Transaction Boundaries

| Workflow | Required database atomicity | Recovery |
|---|---|---|
| Create workspace | Tenant + membership provisioning intent | Idempotent retry/compensation if separate UoWs |
| Create asset | Asset + validated type version reference | Rollback |
| Complete maintenance | History + occurrence + rule anchor + outbox | Idempotent replay/conflict |
| Finalize upload | UploadIntent state + Document row | Reconcile S3 metadata/orphans |
| Plan reminder | Unique delivery intent + outbox event | Retry |
| Send push | Claim + delivery state; external provider separate | At-least-once semantics |

ABP Unit of Work coordinates database writes within its actual transaction scope; it does not make S3 or Expo push operations transactional.

# 23. Background Worker Reliability

Workers must carry a trusted tenant ID in their job payload, validate that the tenant still exists and establish `ICurrentTenant.Change(tenantId)` within a scoped handler. Each iteration restores the prior context. Workers must not disable tenant filters globally.

**Occurrence generation:** enumerate eligible rules in bounded batches, calculate deterministic keys and upsert under unique constraints. **Outbox dispatch:** claim rows with leases and retry backoff; use idempotent handlers. **S3 cleanup:** reconcile expired intents and storage objects before deleting. **Reminder dispatch:** re-check membership, preference and occurrence status immediately before calling Expo.

The chosen ABP job/outbox implementation must be proven by crash-recovery tests in M0/M6. Avoid building a second independent event outbox beside ABP's own outbox for the same domain events.

# 24. React Native Delivery Rules

- Use Expo Router navigation with a protected session boundary.
- Keep authentication tokens in platform secure storage; avoid storing secrets in AsyncStorage.
- Use a generated OpenAPI client for server contracts.
- Partition TanStack Query cache keys by active workspace ID.
- Clear or invalidate tenant-scoped state on workspace switch, logout and membership revocation.
- Use React Hook Form and Zod for client validation, while treating server validation as authoritative.
- Handle app background/resume and token refresh without accidentally switching tenant context.
- Do not display cached data from workspace X after switching to workspace Y.
- Deep links require fresh server-side authorization.

# 25. Security Test Matrix

| Actor | Workspace | Action | Expected |
|---|---|---|---|
| User A / Owner X | X | Create asset | Allowed |
| User A / Member Y | Y | View asset | Allowed |
| User A / Member Y | Y | Manage members | Denied |
| User A | Z | View asset | Denied |
| User B / Owner Y | X | Download document | Denied |
| Revoked member | Former workspace | Receive reminder | Suppressed |
| Worker with trusted X job | X | Generate occurrence | Allowed |
| Worker with stale Y context | X | Access asset | Rejected |
| Unauthenticated caller | Any | List assets | 401 |
| Tenant member | Any | Edit global template | Denied |

Include direct-object-reference attacks, tampered route/header IDs, stale tokens, soft-deleted records, raw SQL bypass attempts and concurrent writes.

# 26. Quality and Performance Gates

Performance budgets are **targets to set after baseline measurement**, not invented guarantees. For each milestone, record representative tenant size, database row counts, hardware, query plan, p50/p95 latency and error rate.

Recommended measurement scenarios:

- Asset list for tenants with 100, 10,000 and 100,000 assets.
- Dashboard with large maintenance history.
- Batch occurrence generation after a week of worker downtime.
- Reminder planning with thousands of due occurrences.
- Search over common asset names and serial numbers.
- S3 finalize with expired/mismatched object metadata.

Use PostgreSQL `EXPLAIN (ANALYZE, BUFFERS)` and application traces before changing indexes or caching behavior.

# 27. Release Risk Register

| Risk | Likelihood | Impact | Mitigation / gate |
|---|---|---|---|
| ABP global identity incompatible with proposed membership model | High until POC | Critical | M0 proof of concept |
| Duplicated tenant tables | Medium | Critical | Schema review before M1 |
| ABP permissions ignore workspace role | Medium | Critical | Cross-workspace tests |
| Tenant filter bypass in worker/raw SQL | Medium | Critical | Security matrix and integration tests |
| Monthly recurrence date drift | Medium | High | Deterministic clock tests |
| Double maintenance completion | Medium | High | UoW + unique constraint + concurrency |
| Orphaned S3 objects | High over time | Medium | Expiry cleanup/reconciliation |
| Duplicate push notification | Expected possibility | Medium | Dedup and user-facing semantics |
| React Native stale tenant cache | Medium | High | Workspace-partitioned query keys |
| Version/package drift | Medium | High | Pin and verify dependencies in CI |

# 28. Decisions Required Before Production

**Must resolve before M0 exit:** ABP edition/version, global identity architecture, workspace resolver, membership-aware permission evaluator, and ABP platform schema ownership.

**Must resolve before M4 exit:** timezone/DST semantics, overdue grace period, month-end anchoring, rule-edit policy and mileage correction rules.

**Must resolve before M5/M6 exit:** S3 upload limits, presigned URL TTL, document retention, outbox/job provider, reminder cadence, quiet-hours semantics and invalid push token handling.

**Must resolve before M8 exit:** data deletion, retention, backup objectives, incident response, privacy notices and beta support responsibilities.

# 29. Suggested AI Coding Agent Task Format

Each coding-agent task should include:

```markdown
## Goal
One independently verifiable change.

## Existing contracts
Relevant ABP module, DTO, database entity and approved ADR.

## Scope
Exact files/modules allowed to change.

## Non-negotiable invariants
Tenant membership, TenantId, nullable PropertyId, lifecycle status, etc.

## Implementation requirements
Validation, transaction, error mapping, logging and retry behavior.

## Tests
Positive, unauthorized, cross-tenant, concurrency and failure cases.

## Acceptance
Build passes, migrations apply, OpenAPI updated, demo reproducible.

## Out of scope
Future marketplace, booking, payments and provider CRM.
```

Agents must not silently choose an identity provider, create a custom tenant registry or change persistence semantics without an ADR.

# 30. Phase 1 Exclusions and Phase 2 Boundary

Phase 1 covers personal workspaces, assets, properties, custom asset types, maintenance, documents, reminders, dashboard/search and settings. Houston/Cypress provider discovery, service quotes, provider profiles, bookings, payments and provider CRM belong to later phases and must not expand M0–M8.

A marketing site or internal admin utility may exist, but a full customer web application is not required for the mobile-first Phase 1 MVP.

# 31. Final Go / No-Go Checklist

- [ ] ABP + .NET 10 compatibility and global identity POC passed.
- [ ] One authoritative ABP tenant registry; no duplicate custom tenant table.
- [ ] Owner/Member permissions tested across multiple workspaces.
- [ ] PostgreSQL migrations applied cleanly and restore rehearsed.
- [ ] Standalone assets work without a property.
- [ ] Asset type versions are immutable and custom JSONB validated.
- [ ] Recurrence generation is deterministic across month ends and timezones.
- [ ] Maintenance completion is atomic and idempotent.
- [ ] S3 uploads/downloads remain private and authorized.
- [ ] Reminders handle retry, revocation, quiet hours and suppression.
- [ ] Dashboard/search are tenant-scoped and performant.
- [ ] Expo app handles session refresh, tenant switching and offline errors.
- [ ] Cross-tenant security suite and concurrency tests pass.
- [ ] Operational runbooks, logs, alerts and backup recovery are validated.
- [ ] Open decisions and known limitations are documented for beta.

**Implementation principle:** Kepnora's ABP architecture is only as reliable as its proven multi-workspace identity, membership authorization, tenant-safe persistence and recoverable business workflows. Finish those foundations before optimizing screens or expanding the product scope.
