# Kepnora

Kepnora is a **mobile-first personal asset and maintenance management platform** for organizing the things people own, tracking what needs care, recording what has been done, and keeping supporting documents together.

The central product question is:

> What do I own, where is it, and what do I need to take care of next?

This document is the **entry point and documentation map**, following the organization and level-of-detail philosophy of the CoreLyxAI reference documentation. It is **not** an API, database, or authorization implementation specification. Those concerns belong in their dedicated documents. Each file will be authored and reviewed individually; planned files are not presented as completed work.

---

## 1. Product Vision

A homeowner may own HVAC equipment, appliances, a water heater, and a lawn mower. A vehicle owner may need mileage-based oil changes and tire rotations. A renter may want to manage a laptop and other electronics without owning a property.

Kepnora brings these into one inventory, connects maintenance schedules and history to individual assets, and stores relevant receipts, manuals, photos, and warranty information.

The intended experience is:

```text
Add a property (optional) or standalone asset
                  |
                  v
         Create or select an asset type
                  |
                  v
        Record asset details and documents
                  |
                  v
      Configure maintenance rules as needed
                  |
                  v
         See upcoming / due / overdue work
                  |
                  v
      Complete maintenance and preserve history
                  |
                  v
        Receive relevant future reminders
```

**Product principles:**

1. An asset does not require a property; `Asset.PropertyId` is nullable.
2. Keep manual entry useful without AI, provider integrations, or hardware telemetry.
3. Keep historical maintenance records auditable even when a schedule changes.
4. Prefer understandable reminders over aggressive notification frequency.
5. Prevent access across workspaces at the API, data, file, and worker boundaries.
6. Do not imply a mileage reading that the user has not entered.

---

## 2. Target Users and Jobs to Be Done

| Persona | Typical assets | Primary jobs |
|---|---|---|
| Homeowner | HVAC, water heater, roof, appliances | Remember recurring service; retain receipts and warranty details |
| Renter | Electronics, furniture, owned appliances | Track possessions without creating a property |
| Vehicle owner | Car, motorcycle | Track time- and odometer-based maintenance |
| Household member (optional/deferred rollout) | Shared home and vehicles | Access authorized records in a shared workspace |

### Representative journeys

**First-time user:** Sign in → provision a personal workspace → optionally add a property → add an asset → optionally add a maintenance rule → view dashboard.

**Vehicle owner:** Add a car without a property → record odometer → configure a mileage-based rule → see remaining mileage when enough data exists → record completion.

**Homeowner:** Add a property → add water heater → attach warranty and manual → schedule annual inspection → record service cost and receipt.

**Returning user:** Open dashboard → inspect due maintenance → complete or skip an occurrence → view updated history and future schedule.

---

## 3. Technology Direction

| Layer | Selected direction | Implementation notes |
|---|---|---|
| Mobile | React Native, Expo, TypeScript | Expo Router; iOS and Android |
| Client data | TanStack Query | Tenant-aware cache keys and invalidation |
| Local UI state | Zustand where needed | No duplicate source of truth for server records |
| Forms | React Hook Form + Zod | Backend validates independently |
| Backend | .NET 10, ASP.NET Core, **ABP Framework** | Layered modular monolith; pin ABP version after compatibility proof |
| Backend application boundary | ABP Application Services and Contracts | DTOs, permissions, validation, Unit of Work |
| Persistence | EF Core + PostgreSQL | JSONB for validated custom asset attributes |
| Tenant registry | ABP Tenant Management | One workspace maps to one ABP tenant; avoid duplicate tenant registries |
| Identity | **Decision pending** | Global identity across multiple ABP tenants needs a working proof of concept |
| Documents | Private AWS S3 | Presigned upload/download with backend authorization |
| Notifications | Expo Push Notifications | Worker, preferences, durable delivery intents |
| API typing | OpenAPI-generated TypeScript client | Generated contracts checked against actual API |
| Background execution | ABP-compatible durable processing | Exact scheduler/provider pending |

ABP is not an optional library in the architecture: it is the chosen backend framework. However, **the specific ABP edition/version, authentication setup, and multi-workspace identity implementation are not yet validated**. Documentation must not treat untested behavior as a framework guarantee.

---

## 4. Product Modules

```text
Authentication / Global Identity
            |
            v
ABP Tenant Management + Workspace Membership
            |
            +--> Properties (optional)
            |
            +--> Asset Type Catalog (global + workspace-defined)
            |            |
            +----------> Assets
                          |
                          +--> Maintenance Rules
                          |        |
                          |        +--> Occurrences / Completion History
                          |                     |
                          |                     +--> Reminder Planning
                          |
                          +--> Documents / Photos / Warranties

Dashboard / Search / Settings consume authorized module APIs.
```

### Module ownership

| Module | Owns | Does not own |
|---|---|---|
| Identity & Workspace | Login identity mapping, workspace membership, tenant selection | Asset data |
| Properties | User-defined locations and property metadata | Independent assets |
| Asset Catalog | Type definitions, versioned custom-field schemas | Maintenance history |
| Assets | Asset identity, ownership, details, optional property relation | Reminder delivery |
| Maintenance | Rules, occurrences, completions, costs, readings | Push provider transport |
| Documents | Upload intents, metadata, secure object references | Public file URLs |
| Notifications | Preferences, reminder intent, delivery status | Maintenance source-of-truth state |
| Dashboard & Search | Read projections and authorized queries | Canonical writes to other modules |

---

## 5. Canonical Business Rules

### 5.1 Workspace isolation

A **Kepnora workspace maps to one ABP tenant**. `AbpTenants` is the authoritative tenant registry. Workspace membership is a separate authorization concept; the presence of a tenant ID or ABP `ICurrentTenant` context does **not** prove a user may access that workspace. A user may belong to multiple workspaces, subject to an identity architecture proof of concept.

### 5.2 Asset ownership

Assets are always workspace-owned. `PropertyId` is optional. A referenced property must belong to the same workspace. Global asset-type templates may be visible across workspaces but must not become editable tenant-owned records merely because a tenant can read them.

### 5.3 Maintenance status

Persisted occurrence lifecycle values are **`Open`, `Completed`, `Skipped`, `Cancelled`**. **`Upcoming`, `Due`, `Overdue`** are calculated presentation states, not persisted lifecycle values. The distinction must hold in SQL, C#, DTOs, API examples, worker logic, and mobile screens.

### 5.4 Mileage and recurrence

Mileage-based rules require real odometer readings; missing readings must be represented as unknown. Time-based rules must define timezone and month-end behavior. Scheduled-anchor and completion-anchor recurrence must not be conflated. Hybrid rules and DST edge cases remain open decisions until specified.

### 5.5 Notifications and files

Private files require authorized upload and download workflows. A successful presigned upload does not itself create a trusted document record; finalize/verification is required. Push delivery is at-least-once/best-effort; duplicates must be reduced with durable idempotency and suppression rules, not promised to be impossible.

---

## 6. Recommended Documentation Structure

This structure mirrors the **responsibility-based grouping** in CoreLyxAI, not its source-code-analysis feature names. The status column distinguishes existing reference material from files to be authored individually.

```text
docs/
  README.md                                  # THIS FILE — entry point
  kepnora-project-context.md                # Product context and constraints
  product-feature-summary.md                # Cross-feature inventory
  architecture-overview.md                  # ABP architecture and boundaries
  database-overview.md                      # PostgreSQL + ABP schema
  mvp-build-order.md                        # Milestones and acceptance gates
  future-features.md                        # Phase 2/3 backlog

  architecture/
    abp-module-design.md                    # Module graph and dependencies
    identity-and-workspace-model.md         # Global identity POC and membership
    tenant-isolation.md                     # Authorization and data scoping
    asset-type-schema-engine.md             # Versioned dynamic field validation
    maintenance-scheduling-engine.md        # Recurrence, mileage and due logic
    reminder-delivery-engine.md             # Durable jobs and push
    document-storage-engine.md              # S3 workflows and security
    architecture-decision-records.md        # Decisions and unresolved gates

  api/
    api-contracts-master.md                 # ABP endpoints, DTOs, errors
    authentication-and-workspaces.md
    properties-and-assets.md
    maintenance.md
    documents-and-notifications.md

  features/
    authentication.md
    onboarding.md
    properties.md
    assets.md
    maintenance.md
    documents.md
    notifications.md
    dashboard.md
    search.md
    settings.md

  implementation/
    backend-project-structure.md            # ABP solution/modules/tests
    frontend-project-structure.md           # Expo Router, components, state
    sql-coding-conventions.md               # PostgreSQL with T-SQL-like style
    testing-strategy.md

  product/
    mvp-scope.md
    pricing-page.md                          # Proposed; not committed
    customer-validation-strategy.md

  ui/
    theme-and-product-direction.md
    navigation-and-screen-map.md
    design-system.md
```

**Authoring rule:** A path appearing above is a *planned destination*, not evidence that its specification exists or is approved. Each file should be written, reviewed, and accepted before it becomes canonical. Keep audit notes and discarded drafts outside the primary `docs/` tree.

---

## 7. Essential Context Files

Once authored and validated, these are the first files a coding agent should read:

```text
README.md
kepnora-project-context.md
architecture-overview.md
database-overview.md
architecture/identity-and-workspace-model.md
architecture/tenant-isolation.md
mvp-build-order.md
```

A coding agent must also read the relevant feature, API, and engine documents before implementing that module. The README alone does not authorize database migrations or identity decisions.

---

## 8. MVP Scope

**Included in Phase 1:**

- Mobile onboarding, authenticated sessions, personal workspace provisioning.
- Property/location management with optional asset association.
- Asset inventory and versioned asset-type definitions/custom fields.
- Time-based maintenance rules, mileage readings and mileage-based rules.
- Maintenance due views, completion records, costs and history.
- Photos, receipts, manuals and warranty metadata in private storage.
- Push reminders with notification preferences and delivery tracking.
- Dashboard, search, profile/settings, tenant-safe API contracts.

**Deferred or conditional:**

- Shared household invitations/membership UI until identity and permissions are proven.
- Provider discovery, quotes and local service comparison (Phase 2).
- Provider booking, payments and provider CRM (Phase 3).
- AI agents, IoT integrations, automatic mileage collection and marketplace workflows.

---

## 9. Architecture and Security Decision Gates

| Gate | Question to resolve | Evidence required before production implementation |
|---|---|---|
| G-01 | Which ABP release supports the pinned .NET/EF Core/Npgsql stack? | Scaffold, restore, compile and migration smoke test |
| G-02 | How is one global user represented across ABP tenants? | Two-tenant login/membership POC |
| G-03 | How are tenant-specific Owner/Member permissions evaluated? | Positive and negative authorization integration tests |
| G-04 | How is workspace context supplied to APIs? | Verified resolver, middleware and application-service boundary |
| G-05 | Which durable job/outbox implementation is used? | Crash/retry/deduplication test |
| G-06 | What are timezone, DST and hybrid maintenance policies? | Explicit ADR and date-boundary tests |
| G-07 | How are workspace deletion and document retention handled? | Deletion/retention policy and recovery tests |

The documentation must label a design as **selected**, **proposed**, or **pending validation**. A selected direction is not equivalent to a tested implementation.

---

## 10. Coding and Naming Conventions

- Documentation files: lowercase kebab-case; root `README.md` is an exception.
- C#: PascalCase for types/properties/methods; async methods use `Async`; follow ABP conventions for module layers and contracts.
- SQL: **PostgreSQL syntax, T-SQL-like presentation**. Uppercase keywords, PascalCase quoted identifiers, four-space indentation, explicit types and named constraints.
- Do not substitute SQL Server-only types/functions (`UNIQUEIDENTIFIER`, `DATETIME2`, `GETUTCDATE()`) into PostgreSQL DDL.
- Preserve ABP-owned table names and migration conventions; do not rename framework tables merely to match formatting preferences.

Example:

```sql
CREATE TABLE "WorkspaceMembers"
(
    "TenantId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "Role" VARCHAR(20) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "JoinedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "PK_WorkspaceMembers"
        PRIMARY KEY ("TenantId", "UserId"),

    CONSTRAINT "CK_WorkspaceMembers_Role"
        CHECK ("Role" IN ('Owner', 'Member'))
);
```

**Example only:** Identity user FK targets and the exact ABP table mapping must be decided and verified before turning this into a migration.

---

## 11. Recommended Authoring Order

1. `README.md` — product map and rules (this file).
2. `kepnora-project-context.md` — complete domain context and non-goals.
3. `architecture/identity-and-workspace-model.md` — resolve the ABP global-user and tenant design before API assumptions.
4. `architecture-overview.md` — ABP modules, deployment, runtime boundaries.
5. `database-overview.md` — schema and migrations consistent with the validated identity model.
6. `architecture/tenant-isolation.md` — detailed authorization and tenant data protection.
7. `mvp-build-order.md` — dependency-aware milestones.
8. Feature specs and engine specs, one file at a time.
9. `api/api-contracts-master.md` — contract catalog grounded in approved domain and feature specs.
10. Implementation and UI files; final consistency and test-matrix review.

**Do not begin Group 3 API contracts by guessing identity endpoints or tenant behavior.** Close the relevant gates first.

---

## 12. Documentation Quality Checklist

Every implementation specification should include, where relevant:

- Purpose, scope, explicit non-goals and ownership boundary.
- Domain definitions, invariants, state transitions and failure cases.
- Exact database/entity mapping and migration considerations.
- ABP application-service contracts, authorization and tenant handling.
- Request/response DTOs and stable error behavior.
- React Native UX flows, loading/empty/error/offline states.
- Concurrency, idempotency, transaction and worker semantics.
- Security abuse cases, observability and operational recovery.
- Acceptance criteria, integration tests and negative test cases.
- Links to authoritative related files and a list of unresolved decisions.

Prefer **complete examples and testable behavior** over repeated summaries or artificial line-count targets. Do not append a compatibility note to an incorrect code example and call the file refactored: replace the example.

---

## 13. Product Direction Beyond MVP

**Phase 2:** optional local provider discovery, quote requests, comparisons and service coordination, subject to validation and geographic coverage.

**Phase 3:** booking, payments, provider workflows and CRM. None of these should force marketplace abstractions into Phase 1 core asset and maintenance data.

Pricing ideas, including a possible premium subscription, are hypotheses until customer validation and billing scope are documented.

---

## 14. Final Product Principle

Kepnora should not become merely a list of possessions, a generic reminder app, or a provider marketplace before the core experience works.

Its core value is:

```text
Know what you own.
Know what needs care.
Keep proof of what was done.
Never lose the context.
```
