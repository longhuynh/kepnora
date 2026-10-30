# Kepnora Project Context

**Version:** 1.0  
**Status:** Product context baseline; architecture decisions marked where unverified  
**Product:** Kepnora  
**Primary purpose:** Cross-session project bible and implementation context  
**Companion:** [`README.md`](README.md)

---

## 1. Vision

Kepnora is a mobile-first personal asset and maintenance management platform. It helps people answer four questions:

1. What do I own?
2. Where is each asset located, if anywhere?
3. What maintenance is due next?
4. Where are the documents and history associated with that asset?

The product starts as a trustworthy personal inventory and maintenance record, **not** a contractor marketplace, booking service, AI assistant, or smart-home telemetry system.

```text
One personal workspace
    -> optional properties / locations
    -> assets, including standalone vehicles and electronics
    -> maintenance rules and recorded work
    -> documents and warranty evidence
    -> dashboard, search, reminders
```

The user's immediate value must not depend on purchasing a subscription, connecting a provider, or owning a home.

## 2. Product Mission

People manage maintenance through fragmented calendars, notes, email receipts, paper manuals, spreadsheets, and memory. Kepnora consolidates the underlying asset, its maintenance obligations, and its evidence into one consistent record.

The MVP should make it easy to:

- Register a vehicle without first creating a property.
- Register a home and associate HVAC, appliances, and equipment with it.
- Track maintenance due by date or by an explicitly entered odometer reading.
- Complete work, capture cost and notes, and retain history.
- Attach receipts, photos, manuals, and warranty documents privately.
- Receive reminders that reflect the actual state of the maintenance rule.
- Search assets and find their supporting records quickly.

Success means a user trusts the history and due dates enough to return to Kepnora instead of maintaining a parallel spreadsheet.

## 3. Target Users

| User | Assets | Key need | Special constraint |
|---|---|---|---|
| Homeowner | HVAC, water heater, roof, appliances, mower | Recurring upkeep and documents | May own several properties |
| Renter | Electronics, furniture, owned appliances | Inventory and receipts | Must not require a property |
| Vehicle owner | Cars, motorcycles | Time and mileage maintenance | Odometer is manually entered; never inferred |
| Household collaborator | Shared assets | Authorized shared maintenance | Membership/invitations may be phased |

A person can fit multiple categories. Avoid forcing users to choose one permanent persona.

## 4. Positioning

Kepnora is **asset-centered** rather than calendar-centered. The asset is the durable organizing unit. Maintenance rules, work history, documents, warranty information, and optional property associations hang from it.

Kepnora is **maintenance-first** rather than insurance-first, expense-first, or marketplace-first. Future provider features must not distort Phase 1 data ownership or authorization.

Kepnora is **mobile-first**: capture a receipt, photograph equipment, log a service, or check due work from a phone.

## 5. Current Technology Stack

| Concern | Direction | Status |
|---|---|---|
| Mobile | React Native + Expo + TypeScript | Selected |
| Navigation | Expo Router | Selected direction |
| API cache | TanStack Query with workspace-scoped keys | Selected direction |
| Local state | Zustand for limited UI state | Selected direction |
| Forms | React Hook Form + Zod | Selected direction |
| API | ASP.NET Core on .NET 10 + ABP Framework | Selected; exact compatible version to verify |
| Architecture | Layered modular monolith | Selected direction |
| Application boundary | ABP Application Services, Contracts, permissions, Unit of Work | Selected direction |
| Persistence | PostgreSQL + EF Core | Selected |
| Custom attributes | Validated JSONB with schema version | Selected direction |
| Tenant registry | ABP Tenant Management (`AbpTenants`) | Proposed canonical mapping; validate in POC |
| Authentication | ABP Identity/OpenIddict or external OIDC | **Open decision** |
| Documents | Private AWS S3, presigned operations | Selected direction |
| Notifications | Expo push + durable worker/outbox | Selected direction; provider details open |
| Client contract | OpenAPI-generated TypeScript | Selected direction |

**SQL style:** Use readable T-SQL-like formatting (PascalCase identifiers, uppercase keywords, named constraints, four-space indentation), but use **valid PostgreSQL syntax**. Preserve ABP-owned schema names and conventions rather than renaming framework tables for cosmetic consistency.

## 6. Product Principles and Invariants

1. **Property is optional.** `Asset.PropertyId` is nullable. A vehicle or laptop may belong directly to a workspace.
2. **One authoritative tenant registry.** A workspace corresponds to one ABP tenant. Do not create an independent duplicate `tenants` table.
3. **Tenant context is not authorization.** `ICurrentTenant.Change(id)` selects scope; it does not establish membership.
4. **Tenant ownership must be enforced in depth.** Check active membership and permissions, apply `IMultiTenant` filters, and prevent cross-tenant foreign-key references.
5. **Maintenance history is durable.** Updating a rule must not rewrite past completed work.
6. **Persisted occurrence lifecycle is narrow.** `Open`, `Completed`, `Skipped`, `Cancelled` are persisted. `Upcoming`, `Due`, and `Overdue` are computed display states.
7. **Mileage is evidence-based.** Never invent or estimate a reading and present it as observed.
8. **Documents are private.** Access is authorized before presigning S3 operations; object keys do not grant access by themselves.
9. **Push is best effort.** Workers and retries are designed for at-least-once delivery; never promise exactly once.
10. **Backend remains the authority.** Mobile-side Zod validation, cache, and tenant selection cannot replace server checks.
11. **Schema evolution is explicit.** Custom asset fields use versioned definitions; old records remain interpretable.
12. **Unresolved framework behavior is not a guarantee.** Multi-workspace identity must pass a real ABP proof of concept.

## 7. Core Product Modules

### 7.1 Identity and session

Authenticates a person, manages mobile sessions and refresh/revocation, and provides a stable subject for membership lookup. The exact implementation is pending the ABP identity proof of concept. Do not specify a password-login endpoint until the identity provider is selected.

### 7.2 Workspaces and membership

Each workspace is represented by an ABP tenant. Kepnora-specific membership records associate a global user with one or more workspaces and a workspace role. A user may be Owner in one workspace and Member in another. ABP roles and permissions must be evaluated with the current workspace in mind.

### 7.3 Properties

Represents optional physical locations (for example, a home or rental property). Supports create, edit, archive, and timezone metadata. Archiving a property must not silently delete its assets or history.

### 7.4 Asset catalog and inventory

Stores actual owned assets and their type definitions. Built-in templates may be global; custom definitions are workspace-scoped. Custom fields are validated against a versioned schema and stored as JSONB. Asset references to a property are optional.

### 7.5 Maintenance

A rule defines recurring or threshold-based maintenance. An occurrence represents a generated actionable instance. A maintenance record represents performed work and its cost, notes, and evidence. Scheduling, completion, concurrency, and reminder planning must remain consistent.

### 7.6 Documents and warranties

Stores metadata for photos, receipts, manuals, and warranty documents. Binary data stays in private S3. Upload uses intent, presigned transfer, verification/finalization, and cleanup of abandoned objects.

### 7.7 Notifications

Manages device registrations, workspace/user preferences, reminder intents, delivery attempts, retry/backoff, and deduplication. Notification scheduling depends on maintenance state, not an independent due-date truth.

### 7.8 Dashboard and search

Dashboard summarizes assets and maintenance requiring attention. Search is tenant-scoped and should include property, asset, and relevant maintenance metadata with stable pagination and authorization.

### 7.9 Settings

Handles profile, workspace selection, notification preferences, and sign-out. Switching workspaces must clear or segregate tenant-specific client cache and must be reauthorized server-side.

## 8. Main User Journeys

### Journey A — First launch

1. User opens the Expo application.
2. User signs in or registers using the selected identity flow.
3. Backend resolves a stable global user subject.
4. Provisioning creates or recovers a personal ABP tenant and Owner membership idempotently.
5. User optionally adds a property; skipping this does not block asset creation.
6. User adds an asset and optionally its maintenance rule.
7. Dashboard shows accurate empty states or next actions.

**Failure case:** If tenant creation succeeds but membership creation fails, retry must recover the existing tenant rather than create another workspace.

### Journey B — Standalone vehicle

1. User creates a vehicle asset with `PropertyId = null`.
2. User enters a known odometer reading.
3. User configures a mileage-based maintenance rule.
4. System computes threshold progress from recorded readings.
5. User completes work and records actual mileage and cost.
6. History persists; next due point is calculated under the rule's anchoring policy.

**Failure case:** Missing odometer information must produce an unknown mileage state, not a guessed value.

### Journey C — Home equipment

1. User adds a property.
2. User adds a water heater associated with that property.
3. User attaches a warranty and purchase receipt.
4. User configures annual inspection.
5. Reminder worker creates and sends eligible notifications.
6. User records completion with date, cost, and service receipt.

**Failure case:** If the user completes the occurrence before a queued reminder sends, the worker must suppress stale reminders where feasible.

### Journey D — Multiple workspaces

1. User authenticates once with a stable global identity.
2. User sees only workspaces with active membership.
3. User selects workspace B; backend checks membership and permission.
4. ABP tenant context is established for authorized operations.
5. Client cache and navigation become workspace-specific.
6. Revocation of B membership denies subsequent B requests.

**Open architectural gate:** This flow requires proof of compatibility with the selected ABP Identity/OpenIddict configuration.

## 9. Maintenance Domain Vocabulary

| Term | Meaning | Persistence |
|---|---|---|
| Maintenance rule | Schedule or threshold definition | Persisted |
| Occurrence | Actionable scheduled instance | Persisted |
| Maintenance record | Actual performed service | Persisted |
| `Open` | Occurrence not resolved | Persisted lifecycle |
| `Completed` | Work recorded | Persisted lifecycle |
| `Skipped` | Explicitly skipped | Persisted lifecycle |
| `Cancelled` | Invalidated/cancelled instance | Persisted lifecycle |
| `Upcoming` | Not yet due, within configured display horizon | Derived presentation |
| `Due` | Due now under policy | Derived presentation |
| `Overdue` | Past due under policy | Derived presentation |
| Unknown mileage | Insufficient observed readings | Derived presentation |

**Anchoring:** A time-based rule may use a fixed scheduled anchor or completion-based anchor. Month-end clamping, timezone boundaries, DST, and missed-cycle handling must be specified in `features/maintenance.md` and ADRs before production implementation.

**Hybrid recurrence:** Combining date and mileage thresholds is an open policy decision. Do not silently choose `AND` or `OR` behavior in code.

## 10. Asset Data Model Concepts

An asset has a stable ID, tenant ownership, type definition and schema version, display name, optional property association, lifecycle state, and custom attribute JSON. Assets can have documents, maintenance rules, odometer readings (where applicable), and historical records.

Global asset type definitions are catalog data, not tenant-owned assets. A workspace may read allowed global templates and its own custom types. Editing global templates is not a workspace permission.

Cross-tenant references must be rejected even if UUIDs are valid and the caller can guess them. The database specification must use composite tenant-aware foreign keys where appropriate.

## 11. ABP Architecture Direction

```text
Expo application
      |
      | HTTPS / OAuth 2.0 (PKCE where supported)
      v
ASP.NET Core ABP HttpApi.Host
      |
      +--> Identity / authenticated subject
      +--> Workspace membership and authorization
      +--> Application Services + Contracts
                 |
                 v
           Domain services / aggregates
                 |
                 v
       EF Core repositories + PostgreSQL

Background jobs / outbox ---> tenant-scoped domain work ---> Expo push
Document services ----------> authorized presigned S3 operations
```

### Module responsibilities

- **Domain.Shared:** shared constants, enums, permission names, localization keys.
- **Domain:** entities, invariants, domain services, domain events.
- **Application.Contracts:** public DTOs, interfaces, validation-facing contracts.
- **Application:** ABP Application Services, authorization orchestration, transactions.
- **EntityFrameworkCore:** DbContext, entity mappings, repositories, migrations.
- **HttpApi:** explicit HTTP endpoints where needed; avoid duplicating auto-exposed services.
- **HttpApi.Host:** hosting, authentication, tenant resolution, dependency injection, OpenAPI.
- **DbMigrator:** controlled migration and seed orchestration.
- **Tests:** domain, application, integration, and contract tests.

This is a proposed layered project breakdown. Exact generated project names and package references must be derived from the pinned ABP template.

## 12. Tenant and Identity Design

### Canonical workspace mapping

A Kepnora workspace corresponds to one ABP tenant record. Its ABP tenant ID is the tenant ID stored on workspace-owned domain entities. Workspace-specific metadata may live in an extension entity keyed by that same ID; it must not be a competing tenant registry.

### Membership is separate from tenant context

`tenant_members` (or an ABP-conventionally named equivalent) records which global user belongs to which workspace and the role/status of that membership. Before executing a tenant-scoped application service, verify active membership and permission. `ICurrentTenant` determines the data filter scope; it does not grant access.

### Identity POC required

ABP's tenant-local Identity patterns must not be assumed to support a single global user across multiple workspaces. The implementation must demonstrate the same authenticated subject acting as Owner in workspace A and Member in workspace B, while having no access to workspace C.

### Tenant-specific worker operations

Background jobs carry trusted tenant IDs, establish the correct ABP tenant context inside a bounded scope, and never reuse a previous job's context. Job payload tenant IDs are not client authorization evidence.

## 13. Data Isolation and Security

Security controls operate at multiple levels:

1. **Authentication:** validate token issuer, audience, signature, expiry, and subject.
2. **Membership:** verify user is active in the requested workspace.
3. **Permission:** check operation-specific authorization with the workspace role.
4. **Data filter:** ABP `IMultiTenant` filters scope ordinary EF Core queries.
5. **Object ownership:** verify loaded resource and referenced entities belong to the same tenant.
6. **Database:** composite foreign keys and unique constraints prevent cross-tenant relations.
7. **S3:** authorize document metadata before issuing a presigned URL.
8. **Workers:** enforce tenant context and deduplication outside HTTP requests.

Test negative cases for guessed IDs, revoked membership, tenant switching, raw SQL, bulk operations, disabled filters, background jobs, and object storage.

## 14. Documents and File Security

Document metadata includes tenant ownership, associated asset/record, document type, object key, content type, size, upload state, and timestamps. Binary content is private in S3.

Recommended flow:

```text
Authorized upload intent
    -> validate type, size, target ownership
    -> create pending intent
    -> issue short-lived presigned upload
    -> client transfers bytes directly to S3
    -> finalize with server-side verification
    -> publish durable document metadata
    -> cleanup expired pending uploads
```

A presigned URL is a temporary bearer capability. Never log it, place it in public analytics, or treat it as proof of user membership.

## 15. Notification Strategy

Notifications are generated from authoritative maintenance state and preferences. A durable outbox or equivalent persistence mechanism separates business transactions from external push delivery. Workers retry transient failures and record delivery attempts.

A unique delivery intent or deduplication key prevents duplicate planning; external push providers may still deliver more than once. Device tokens belong to a global user, while preferences may differ per workspace.

## 16. Dashboard and Search

Dashboard displays counts and prioritized due maintenance. Search supports a predictable query model and stable pagination. Both are tenant-scoped, permission-checked, and backed by indexed queries rather than loading the entire tenant inventory.

Search should not reveal cross-tenant entity existence through errors, suggestions, autocomplete, or counts. Client cache keys must include active workspace ID.

## 17. MVP Scope

### Included in Phase 1

- Authentication and personal workspace provisioning.
- Properties and standalone assets.
- Built-in and custom asset types with validated versioned attributes.
- Time-based and manually measured mileage-based maintenance.
- Maintenance completion, skip/cancel, history and costs.
- Private photos, manuals, receipts and warranty records.
- Push reminders and notification preferences.
- Dashboard, search, settings, and mobile release readiness.

### Explicitly deferred or conditional

- Provider discovery, quotes, bookings, payment, and provider CRM.
- AI recommendations, automatic telemetry, and smart-home integrations.
- Complex household invitations until membership and role POC is complete.
- Advanced hybrid recurrence until policy is defined.
- Full offline write synchronization; online-first UX is acceptable for MVP.
- Customer-facing web dashboard unless separately approved.

A hypothetical premium price such as $4.99/month is not a committed business decision.

## 18. Future Roadmap

**Phase 2:** provider discovery and quote requests, initially focused on a defined local market. Preserve the ability to expand without coupling provider data to asset ownership.

**Phase 3:** bookings, payments, provider-facing CRM/workflows, and operational integrations. These require separate trust boundaries, authorization roles, and financial data models.

Later options may include predictive maintenance and AI assistance, but these are not prerequisites for the MVP.

## 19. Known Constraints

- Exact ABP release compatible with .NET 10 and PostgreSQL provider packages must be validated.
- ABP Identity/OpenIddict versus external OIDC is undecided.
- Single-user/multi-workspace semantics require a running proof of concept.
- Notification delivery depends on mobile platform permissions and provider availability.
- Mileage cannot be accurate without user-entered readings.
- Timezone/DST rules and missed recurrence behavior require formal policy.
- Private S3 storage requires lifecycle management and authorized cleanup.
- Provider marketplace and payments introduce materially different compliance and support obligations.

## 20. Open Architectural Decisions

| Decision | Why it matters | Evidence required |
|---|---|---|
| ABP edition/version | Module API, .NET 10 compatibility | Scaffold, build, integration tests |
| Identity provider | Login, refresh, subject mapping | Mobile PKCE login POC |
| Global identity / membership | Multi-workspace correctness | Owner A / Member B / denied C tests |
| Tenant resolution | Header, route or domain selection | Spoofing and revocation tests |
| Permission provider | Role differs by workspace | Tenant-sensitive authorization tests |
| Background job implementation | Retry, durability, tenant scoping | Crash/restart tests |
| DST/timezone | Accurate reminders | Boundary-date test suite |
| Hybrid recurrence | Correct due status | Product decision + examples |
| Document retention/deletion | Storage and privacy | Lifecycle specification |
| Family invitations | Onboarding and access control | Explicit scope decision |

Do not silently fill these gaps while implementing Group 3 API contracts.

## 21. Glossary

| Term | Meaning |
|---|---|
| Workspace | User-facing collection of owned assets; maps to an ABP tenant |
| Tenant | ABP-owned isolation and configuration boundary |
| Tenant member | A global user authorized to access a workspace |
| Property | Optional physical location containing assets |
| Asset | Owned item with identity, type and optional property |
| Asset type | Definition of attributes and validation schema |
| Rule | Recurrence or threshold policy for maintenance |
| Occurrence | Scheduled maintenance instance |
| Service record | History of work actually performed |
| Odometer reading | Explicitly recorded mileage evidence |
| Upload intent | Temporary authorized file transfer record |
| Outbox | Durable event/delivery intent for asynchronous processing |
| ABP Application Service | Authorized application boundary with DTO contracts |
| `ICurrentTenant` | ABP tenant context; not proof of membership |
| `IMultiTenant` | ABP interface supporting tenant-scoped entities |

## 22. Documentation Conventions

The `docs/` tree follows the **organization philosophy** of the CoreLyxAI reference, but not its architecture-intelligence domain logic.

- Root: README, project context, architecture, database, build order.
- `architecture/`: ABP module design, tenancy, identity, ADRs.
- `product/`: MVP scope and future roadmap.
- `features/`: individual mobile-first product workflows.
- `api/`: endpoint catalog, DTOs, error model, pagination and versioning.
- `implementation/`: project structure, conventions, deployment and testing.
- `ui/`: design system, navigation, states and interaction guidance.

**Single-file workflow:** Write one file to implementation depth, review it, then proceed. Do not replace detailed approved documents with short summaries to make the folder look consistent.

**Status language:** Distinguish selected direction, proposed design, open decision, and tested implementation. Documentation is not proof that code exists.

## 23. Related Specifications

| File | Responsibility | State |
|---|---|---|
| `README.md` | Entry point and documentation map | Authored |
| `kepnora-project-context.md` | This project bible | Authored |
| `architecture-overview.md` | ABP system and module architecture | To author individually |
| `database-overview.md` | Complete ABP/PostgreSQL schema | To author individually |
| `architecture/tenant-isolation.md` | Membership, ABP tenant mapping, authorization | To author individually |
| `architecture/architecture-decision-records.md` | Approved and pending ADRs | To author individually |
| `implementation/backend-project-structure.md` | ABP solution and module layout | To author individually |
| `implementation/frontend-project-structure.md` | Expo application organization | To author individually |
| `features/authentication.md` | Identity and mobile session flow | To author individually |
| `features/onboarding.md` | Personal workspace provisioning | To author individually |
| `features/assets.md` | Asset inventory and custom type schemas | To author individually |
| `features/maintenance.md` | Recurrence, completion, history, reminders | To author individually |
| `api/README.md` | Group 3 API contract index | Not started |

The existence of a file in a previous experimental ZIP does not mark it approved in the new clean documentation set.

## 24. How to Use This File

**Product discussion:** Use this file to preserve vision, personas, scope, and constraints. Do not turn open decisions into implicit requirements.

**Architecture discussion:** Use the ABP-first direction, one tenant registry, and explicit membership checks as starting invariants. Confirm compatibility against the actual ABP version.

**Feature authoring:** Trace each feature to a user journey, tenant authorization, database ownership, API contract, UI states, failure cases, and tests.

**Implementation planning:** Use milestone acceptance criteria and working POCs. Avoid generating full auth/tenant API contracts until the identity and workspace switching gates pass.

**AI coding agents:** Treat this as context, not executable implementation instructions. Detailed specifications and verified source code take precedence for concrete syntax and API signatures.

## 25. Final Product Direction

Kepnora should become a reliable personal system of record for assets and their maintenance, with the smallest practical Phase 1 that users can trust. The first release should work for someone who owns a single car just as well as someone who manages several properties.

The technical foundation is a mobile-first Expo application backed by a .NET/ABP modular monolith, PostgreSQL, private S3, and durable notification processing. Multi-workspace security must be demonstrated rather than assumed. The documentation must remain deep, consistent, and written one reviewed file at a time.
