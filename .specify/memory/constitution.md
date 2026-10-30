<!--
Sync Impact Report
- Version change: 1.0.1 -> 1.1.0 (expanded technology and delivery requirements).
- Modified principles: none; existing eight principles retained.
- Updated sections: Technology Stack Requirements (Non-Negotiable);
  Development Workflow and Quality Gates.
- Added guidance: categorized tooling, project layout, release policy, and definition of done.
- Removed sections: none; compact stack table replaced with detailed requirements.
- Deferred placeholders: none. Existing implementation decision gates remain unchanged.
- Scope: only .specify/memory/constitution.md changed; templates read it at runtime.
- Review note: remove this temporary Sync Impact Report before committing the constitution.
-->

# Kepnora Constitution

## Core Principles

### I. Asset-Centered, Mobile-First Product

Kepnora MUST deliver personal asset inventory and maintenance management through React Native
on iOS and Android. Phase 1 covers authentication, workspaces, optional properties, built-in
and custom asset types, maintenance, private documents and warranties, reminders, dashboard,
search, and settings. An asset MUST be creatable without a property; `Asset.PropertyId` is
nullable. Onboarding MUST allow users to skip property creation. Archiving a property or asset
MUST preserve associated maintenance history and evidence.

Provider discovery and quotes belong to Phase 2; booking and payments belong to Phase 3.
AI recommendations, telemetry, and a custom admin application MUST NOT enter Phase 1 without
an explicit scope amendment. Kepnora's documents define the product and domain.

### II. ABP Modular Monolith and Explicit Boundaries

The backend MUST use a layered ABP Framework modular monolith: Domain.Shared, Domain,
Application.Contracts, Application, EntityFrameworkCore, HttpApi, HttpApi.Host, and DbMigrator.
Domain owns invariants and repository interfaces; Application orchestrates authorized use cases
and ABP Units of Work; EntityFrameworkCore owns persistence and migrations; HttpApi owns HTTP
translation; HttpApi.Host owns composition, authentication, configuration, and health checks.

Domain MUST NOT depend on HTTP, EF Core, S3, or Expo implementation details. Controllers MUST
remain thin, and infrastructure adapters MUST isolate external providers. Modules MUST have
acyclic dependencies verified by architectural tests. Separate worker hosting is permitted
when required by lifecycle or scaling, while retaining the same domain and application rules.
New services or alternative frameworks require an ADR explaining the need and migration impact.

### III. Workspace Isolation and Authorization in Depth

One workspace MUST map to exactly one ABP tenant. ABP Tenant Management is the authoritative
registry; a parallel custom tenant registry is forbidden. Workspace-owned domain records MUST
carry the corresponding non-null tenant ID. Global catalog templates are explicitly scoped,
read-only exceptions, not tenant-owned assets.

Every protected operation MUST authenticate the subject, verify active workspace membership,
and enforce operation permissions before accessing data. `ICurrentTenant` and client-selected
tenant IDs establish scope, never authorization. ABP multi-tenant filters, ownership checks,
and tenant-aware foreign keys and uniqueness constraints MUST prevent cross-workspace access
and references. Raw SQL, bulk operations, workers, search, and file operations require the same
isolation guarantees. Workers MUST establish and dispose a fresh trusted tenant scope per job.

M0 MUST prove one global subject can own workspace A, participate with different permissions
in B, be denied C, and lose B access after revocation. Default tenant-local identity behavior
MUST NOT be treated as proof that this requirement works.

### IV. Durable History and Deterministic Maintenance

Published custom asset schemas MUST be immutable versions. Each asset's JSONB attributes MUST
be validated server-side against its recorded schema version; schema evolution MUST preserve
interpretability of existing records.

Maintenance rules, occurrences, and performed-work records MUST remain distinct. Only `Open`,
`Completed`, `Skipped`, and `Cancelled` are persisted occurrence lifecycle states. `Upcoming`,
`Due`, and `Overdue` MUST be derived from authoritative rules, time, timezone, and observed
mileage. Missing mileage MUST remain unknown; inferred readings cannot be presented as observed.

Completion MUST atomically and idempotently update occurrence state, preserve performed-work
history, and establish required scheduling/outbox changes. Concurrent or repeated requests MUST
NOT create duplicate business effects. Rule edits MUST NOT rewrite completed history.
Anchoring, month-end clamping, DST, timezone precedence, missed cycles, and any hybrid date/mileage
policy MUST have recorded decisions and deterministic boundary tests before implementation
is accepted. Calendar tests MUST use an injected clock and explicit timezone.

### V. Private Documents and Recoverable Background Work

Documents MUST use private AWS S3 storage and authorized tenant-owned metadata. Uploads MUST
follow intent, short-lived presigned transfer, server verification/finalization, and abandoned
upload cleanup. Authorization MUST precede presigning and finalization; object keys alone do
not grant access. File size, type, and target ownership MUST be validated server-side.

Business changes and their outbox intents MUST commit consistently. Jobs MUST use bounded
batches, durable state, idempotency keys, concurrency control, bounded retries/backoff, and
observable terminal failures. Reminder delivery MUST recheck current maintenance eligibility
and preferences to suppress stale work. Expo push is best effort; internal processing supports
at-least-once retries and MUST NOT promise exactly-once external delivery.

### VI. Typed Contracts and Secure Mobile State

C# MUST enable nullable reference types and TypeScript MUST use strict checking. Public APIs
MUST expose explicit DTOs, bounded pagination, validation, authorization failures, and concurrency
semantics. Generated TypeScript clients MUST come from the actual ABP host's OpenAPI; proposed
documentation routes MUST NOT be mistaken for verified endpoints.

React Native screens MUST compose reusable UI and feature hooks. TanStack Query owns server
state; Zustand is limited to UI state; React Hook Form and Zod provide client form validation.
Server validation and authorization remain authoritative. Workspace-owned cache keys and local
state MUST be partitioned by workspace, with invalidation or clearance on switching, revocation,
and logout. Sensitive session credentials MUST use SecureStore; secrets MUST NOT be embedded
in the app. Offline views MUST disclose stale data, and offline writes require an explicitly
specified synchronization and conflict policy before enablement.

Screens MUST cover loading, empty, success, validation, offline, error, and denied states as
applicable. UI MUST follow Kepnora's design tokens and screen specifications, support accessible
labels and text scaling, and verify camera, file selection, deep links, and notifications on
real iOS and Android devices.

### VII. Evidence-Based Quality Gates

Behavior changes MUST have tests for their acceptance criteria and relevant failure paths.
Domain and application rules MUST use xUnit tests, with at least 80% coverage in those layers;
coverage does not substitute for invariant tests. Every public endpoint MUST have hosted
contract tests for success, validation, authorization, and relevant conflict/error cases.

Real PostgreSQL integration tests MUST prove migrations, JSONB behavior, tenant-aware constraints,
transactions, and concurrency. SQLite or in-memory substitutes cannot establish those guarantees.
Tests MUST cover all INV-01 through INV-15 in `docs/mvp-build-order.md`, including cross-tenant
negative cases, idempotent completion, worker crash/retry, private files, and mobile cache switching.
Mobile component tests and device E2E MUST verify the critical user journeys. Mocks MUST NOT be
reported as evidence of real identity middleware, database enforcement, S3, or push delivery.

### VIII. Observable, Measured, and Maintainable Delivery

Structured Serilog events MUST include correlation, operation, outcome, duration, and appropriate
user/tenant identifiers. Logs and analytics MUST exclude credentials, tokens, presigned URLs,
private file contents, and sensitive custom fields. Health checks and monitoring MUST expose
API failures/latency, database health, worker backlog, outbox age, retries, and terminal failures.

Queries MUST use bounded results and appropriate indexes and MUST avoid N+1 access. Plans MUST
record representative data volumes, hardware, query plans, and p50/p95 latency before setting
release budgets; applicable budgets MUST be agreed and verified before beta release. Backups,
restore drills, migration recovery, and worker restart recovery MUST have repeatable evidence.

## Technology Stack Requirements (Non-Negotiable)

Kepnora's backend baseline is .NET 10, ABP 10.3.0, EF Core with PostgreSQL, Autofac,
Serilog, and Identity/OpenIddict. M0 MUST validate package compatibility and global identity
behavior before this baseline is accepted for implementation.

**Backend**:

- C#, .NET 10, ASP.NET Core, and ABP Framework MUST implement the modular monolith.
  ABP 10.3.0 is the initial compatibility baseline; upgrades MUST be pinned and verified.
- ABP Application Services and Contracts MUST own use-case boundaries. Explicit controllers
  MUST use `AbpController` in `HttpApi/Controllers/` where needed. Standalone Minimal APIs
  MUST preserve the same authorization, validation, Unit of Work, and contract guarantees.
- EF Core with Npgsql MUST provide PostgreSQL persistence, with migrations in `Migrations/`.
- Autofac MUST provide dependency injection through ABP integration.
- Serilog MUST provide structured logging. System.Text.Json or Newtonsoft.Json MUST be used
  consistently with the pinned ABP configuration and verified serialization contracts.
- ABP Identity/OpenIddict MUST pass the global subject and membership POC.
- Private S3 and Expo push MUST use isolated adapters. Durable ABP-compatible jobs and a
  transactional outbox MUST provide retry/recovery; M0 selects the job provider.

**Frontend — React Native**:

- React Native, Expo, and strict TypeScript MUST deliver the iOS and Android application.
  Expo SDK and native dependencies MUST be pinned as a compatible set.
- Expo Router MUST own navigation and authenticated deep-link handling.
- Reusable native components MUST implement `docs/ui/design-system.md` and
  `docs/ui/design-system/tokens.md`, including typography, spacing, color, and accessible states.
- TanStack Query MUST own workspace-scoped server state; Zustand MUST be limited to UI state.
- React Hook Form and Zod MUST handle forms and client validation backed by server checks.
- OpenAPI-generated TypeScript contracts MUST drive a shared authenticated transport with
  typed errors and controlled session refresh.
- SecureStore MUST hold sensitive credentials. Platform adapters MUST isolate camera,
  file selection, notifications, and other native capabilities.

**Database and Storage**:

- PostgreSQL with Npgsql MUST serve development and production. Schema changes MUST include
  reviewed EF Core migrations and upgrade/recovery verification.
- Custom attributes MUST use validated JSONB and immutable published schema versions.
- Database integration tests MUST use real PostgreSQL matching the production major version.
- Document binaries MUST remain in private S3 with authorized transfer and finalization.
- Production MUST have automated daily database backups to protected object storage,
  documented retention/access controls, and repeatable restore drills. Agreed recovery
  objectives MUST determine whether additional continuous recovery is required.

**Testing**:

- xUnit MUST cover backend behavior; NSubstitute MAY isolate dependencies in unit tests.
- Hosted HTTP tests MUST verify actual authorization and API contracts; PostgreSQL tests
  MUST verify migrations, constraints, JSONB, transactions, and concurrency.
- React Native Testing Library and an Expo-compatible runner MUST cover mobile components.
  M0 MUST validate and select the runner; each workspace MUST use it consistently.
- Maestro or Detox MUST be selected through a native device POC. Critical E2E journeys
  MUST run on iOS and Android, including session refresh and workspace switching.
- Domain/Application coverage MUST meet the existing 80% gate alongside invariant tests.

**Build and Deployment**:

- MSBuild and dotnet CLI MUST build the backend using a pinned SDK.
- Yarn and a committed lockfile MUST provide reproducible mobile dependency installation.
- Docker MUST package backend hosts. Production and local Dockerfiles MUST distinguish
  environment requirements where needed; credentials MUST come from secret management.
- Expo-compatible iOS/Android build and release profiles MUST be pinned and documented.
  Signing credentials MUST remain outside source control.
- GitHub Actions MUST enforce PR compilation, test, lint, coverage, and security gates and
  retain verification artifacts. DbMigrator MUST control migration execution.

**Logging and Observability**:

- Serilog MUST emit sanitized structured events with correlation IDs. Local hosts MAY use
  asynchronous file logging at `Logs/logs.txt`; deployed containers MUST expose logs to the
  configured collector rather than rely solely on ephemeral files.
- Host health checks MUST live in `HealthChecks/` and cover relevant dependencies without
  exposing secrets. Monitoring MUST cover API failures/latency, worker backlog, outbox age,
  retries, and terminal failures.
- The deployment plan MUST select the monitoring backend and demonstrate alerts and recovery.

The product frontend MUST use React Native with Expo for iOS and Android. Any framework-hosted
identity UI MUST be limited to authentication flows and integrate with the mobile session
and navigation model.

The repository MUST preserve backend layer responsibilities under `backend/`; the mobile root
MUST be selected once during scaffolding because source documents propose both `mobile/` and
`apps/mobile/`. Do not create duplicate clients to satisfy both examples. Physical project names
MUST follow the verified scaffold. EF Core migrations belong in EntityFrameworkCore and run
through controlled DbMigrator orchestration. SQL MUST use valid PostgreSQL syntax with readable
PascalCase identifiers, uppercase keywords, and named constraints while preserving ABP-owned
schema conventions.

M0 MUST pin and record the exact .NET SDK, ABP edition/packages, EF Core/Npgsql, identity,
Expo/React Native, and test-tool versions, compile the solution, apply migrations, emit OpenAPI,
and complete the identity POC. ABP Identity/OpenIddict is the baseline identity stack; an
external OIDC provider requires a recorded ADR and the same membership/isolation evidence.
No unverified password endpoint, framework API, or provider guarantee may become a contract.

## Development Workflow and Quality Gates

Work MUST follow the dependency gates in `docs/mvp-build-order.md`: M0 foundation and identity;
M1 account/workspace security; M2 properties/types; M3 assets; M4 maintenance; M5 documents;
M6 reminders; M7 dashboard/search/settings; M8 hardening. M5 can proceed after M3 independently
of M4, while M6 depends on maintenance semantics. A dependent milestone MUST NOT claim completion
before prerequisite evidence exists.

Every feature plan MUST include a Constitution Check mapping applicable principles and invariant
IDs to implementation and verification tasks. Architectural choices MUST be recorded in
`docs/architecture/architecture-decision-records.md`; draft examples and open decisions MUST
retain their status until validated. Specifications and plans MUST resolve applicable recurrence,
permissions, schema evolution, retention, and API decisions before dependent work is accepted.

Pull requests MUST pass builds, type checks, linting, applicable unit/integration/contract/device
tests, coverage, dependency-boundary checks, and secret/dependency/security scanning. Unresolved
high or critical findings block release unless explicitly risk-accepted by the project owner
with a remediation deadline. Review MUST check tenant isolation, migrations, contract drift,
retry behavior, accessibility, and scope. Breaking APIs require an ADR and a compatibility or
migration plan covering installed mobile clients.

A feature is done only with reviewed implementation, passing applicable checks, migration and
recovery evidence, matching OpenAPI/client contracts, relevant mobile states, sanitized logs,
updated documentation, and reproducible milestone acceptance evidence. Beta additionally requires
the M8 security, performance, backup/restore, and iOS/Android device release gates.

**Code Organization by Layer**:

This layout defines responsibilities. M0 MUST reconcile physical names with the ABP scaffold
and select one mobile root. Optional projects MUST be added only when needed.

```text
backend/
  src/
    Kepnora.Domain.Shared/          # Constants, enums, shared error codes
    Kepnora.Domain/                 # Entities, domain services, repository interfaces
      Identity/                    # Identity domain integration
      Settings/                    # Settings definitions
    Kepnora.Application.Contracts/  # DTOs and application interfaces
      Permissions/                 # Permission definitions
    Kepnora.Application/            # AppServices, orchestration, DTO mappings
    Kepnora.EntityFrameworkCore/
      EntityFrameworkCore/         # DbContext and entity mappings
      Migrations/                  # EF Core migrations
    Kepnora.HttpApi/
      Controllers/                 # Thin ABP controllers
      Models/                      # HTTP-specific models where required
    Kepnora.HttpApi.Host/
      Program.cs                   # Composition, Autofac, Serilog
      HealthChecks/
      appsettings.json             # Non-secret configuration
      Dockerfile
    Kepnora.DbMigrator/             # Controlled migrations and seeds
    Kepnora.Worker/                 # Optional separate durable worker host
  tests/
    Kepnora.Domain.Tests/
    Kepnora.Application.Tests/
    Kepnora.EntityFrameworkCore.Tests/
    Kepnora.HttpApi.Tests/
    Kepnora.TestBase/               # Shared fixtures and helpers
mobile/                            # Or apps/mobile/, selected once in M0
  app/                             # Expo Router routes and layouts
  src/
    api/                           # Generated contracts and transport
    features/                      # Feature hooks, forms, screens
    components/                    # Reusable native UI
    theme/                         # Design tokens and theming
  e2e/                             # Native device journeys
.github/
  workflows/                       # CI and delivery gates
```

**Code Review Process**:

Reviewers MUST verify DDD boundaries, acyclic dependencies, tenant isolation, and recoverability.
Changes spanning authentication, API contracts, documents, notifications, or sessions MUST receive
review covering both backend and mobile responsibilities. Breaking APIs require a project-owner-
approved ADR and a mobile compatibility/migration strategy. Security exceptions follow the
explicit risk-acceptance rule above.

**Versioning and Breaking Changes**:

Product releases MUST use semantic versioning: MAJOR for incompatible API/behavior changes,
MINOR for compatible features, and PATCH for compatible fixes. MAJOR/MINOR releases MUST include
a changelog entry, relevant ADRs, migration guidance where needed, and release notes for affected
users. Deprecated APIs MUST remain supported with documented warnings for at least two minor
releases before removal; the release plan MUST account for installed mobile clients.
Constitution versioning follows Governance separately.

**Performance and Coupling Standards**:

CI MUST reject module cycles. Data access reviews MUST verify query count, projection,
pagination, and indexes; eager loading MUST be bounded and justified by the use case.
Mobile checks MUST measure cold start, critical screen responsiveness, list scrolling, memory,
and file transfers on representative iOS and Android devices. API checks MUST measure dashboard,
search, maintenance, and document operations at representative tenant volumes. Budgets MUST be
recorded and agreed before beta, then enforced against regressions.

**Definition of Done**:

1. Implementation builds with pinned dependencies and respects ABP and React Native boundaries;
   new compiler/type warnings are resolved or explicitly justified in review.
2. Applicable unit, integration, contract, and mobile tests pass, including negative cases and
   invariants; Domain/Application coverage meets the 80% minimum.
3. OpenAPI and generated clients agree; technical and relevant user documentation are updated.
4. Architectural decisions are recorded in `docs/architecture/architecture-decision-records.md`.
5. User-visible changes have a `CHANGELOG.md` entry associated with the release version.
6. Maintainer review and CI gates pass, with reproducible milestone evidence recorded.
7. Schema changes include tested migrations and recovery procedures; retries preserve integrity.
8. Applicable performance budgets, accessibility/state checks, privacy controls, and operational
   recovery checks pass. Beta additionally satisfies all M8 release gates.

## Governance

This constitution is the authoritative project governance document. Product and domain details
come from `docs/kepnora-project-context.md`, `docs/product/mvp-scope.md`,
`docs/database-overview.md`, and the feature documents. Architecture details come from
`docs/architecture-overview.md`, `docs/architecture/`, and `docs/implementation/`.
`docs/mvp-build-order.md` governs sequencing and invariant evidence;
`docs/testing/test-strategy.md`, `docs/deployment/deployment-guide.md`, and `docs/ui/` govern
verification, operations, and presentation within these constitutional rules.

When documents conflict, plans MUST identify the conflict and record its resolution through an
ADR or constitutional amendment before treating either proposal as implemented. Architecture
and technology decisions MUST satisfy Kepnora's product scope, React Native frontend, and
domain invariants.

Amendments MUST state the motivation, changed rules, affected specifications, migration impact,
and verification plan and receive project-owner approval. Versioning is semantic: MAJOR for
incompatible removal/redefinition of principles, MINOR for new principles or materially expanded
guidance, PATCH for clarifications without changed obligations. Ratification remains the original
adoption date; Last Amended changes whenever this document changes.

Every plan and review MUST assess compliance. Exceptions MUST name the affected rule, rationale,
owner, scope, compensating controls, and expiration/remediation milestone in an approved ADR.
An exception cannot silently redefine a principle; permanent changes require an amendment.
Open implementation gates are tracked decisions and MUST NOT be represented as completed work.

**Version**: 1.1.0 | **Ratified**: 2026-10-30 | **Last Amended**: 2026-10-30
