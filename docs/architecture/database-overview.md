# Kepnora — Database Overview

**Status:** Draft v1.0 — Pending Architecture Review  
**Scope:** MVP (Phase 1), with explicit extension points for later phases  
**Target stack:** PostgreSQL, Entity Framework Core, ASP.NET Core / ABP  
**Last updated:** 2026-10-30

> **Source and approval note:** This document is drafted from Kepnora requirements established in project discussions. The full Kepnora Markdown specification repository was not available for line-by-line reconciliation while preparing this draft. Sections labeled **Confirmed** reflect decisions in those discussions; sections labeled **Proposed** are design recommendations, not approved schema. Validate this document against `kepnora-project-context.md`, `architecture-overview.md`, `mvp-build-order.md`, and `tenant-isolation.md` before implementation.

## 1. Purpose

Define the MVP database boundaries, tenancy model, entity relationships, persistence rules, and unresolved decisions. This document is a design baseline; it is **not** authorization to generate migrations before the global identity and tenant-isolation POCs succeed.

## 2. Confirmed product and persistence requirements

- **Workspace = one ABP tenant.** A global user can join more than one workspace.
- MVP workspace roles are **Owner** and **Member** only. Do not add other roles without a spec change.
- Properties are optional for Items. Items such as vehicles and electronics may have no Property.
- UI terminology is **My Items**, **Add Item**, and **Item Details**. Backend/API terminology remains **Asset**, `AssetId`, and `AssetType`.
- Maintenance occurrence persisted statuses are `Open`, `Completed`, `Skipped`, `Cancelled`. `Upcoming`, `Due`, and `Overdue` are **derived presentation statuses**, not additional persisted statuses.
- Vehicle mileage readings are **entered manually**; no automatic projection or inferred odometer values.
- MVP includes authentication, workspaces, properties, asset types/assets, maintenance rules/occurrences/history, manual mileage, documents/photos, reminders, dashboard, search, and settings.
- Files are private; S3-style object storage is planned. Notifications use an outbox/worker design.
- Find Services and booking/payments are later phases and must not be treated as MVP entities.
- Custom Fields are being designed for individual Items; reusable Custom Item Types/templates are conceptually separate. Exact schema and limits require approval.

## 3. Tenancy and identity boundary

### 3.1 Confirmed logical model

```text
Global User (one identity)
    | 0..N
    v
Workspace Membership ---- Workspace (one ABP Tenant)
                                  |
                                  +-- Property (0..N)
                                  +-- Asset / My Item (0..N)
                                  +-- Maintenance, Documents, Notifications
```

**Important ABP compatibility risk:** Do not assume the default tenant-scoped ABP Identity user table supports a single global user seamlessly across multiple tenants. Validate the actual framework behavior in an M0 POC. Authentication identity and workspace authorization are separate concerns.

### 3.2 Proposed identity mapping — subject to POC

- `AppUser`: a globally unique application identity (whether implemented with ABP host-level Identity or an alternative mapping must be decided after POC).
- `Workspace`: domain workspace, associated one-to-one with an ABP tenant ID.
- `WorkspaceMembership`: join between global user and workspace, with `Role` = `Owner` or `Member`, and membership lifecycle status.
- Every workspace-owned row carries `TenantId` (or the documented equivalent) and is filtered by current workspace context.
- Never authorize a request based on a client-supplied `TenantId` alone. Resolve identity, membership, role, and tenant context on the server.
- Membership lookup and tenant switching must not expose data from a workspace the user has not joined.

**Open decision:** Whether `Workspace` is the ABP tenant record itself, an extension of it, or a domain table with a one-to-one `AbpTenantId` mapping.

## 4. Proposed MVP entity catalog

This catalog is **proposed**, not a claim that these exact tables or columns already exist in the repository.

| Domain | Proposed entity | Purpose | Workspace scoped? |
|---|---|---|---|
| Identity | Global user / ABP Identity mapping | Sign-in identity | Global |
| Workspace | Workspace | Workspace metadata and tenant mapping | Tenant definition |
| Workspace | WorkspaceMembership | Global user ↔ workspace and Owner/Member role | Cross-boundary mapping |
| Properties | Property | House or other managed property | Yes |
| Items | AssetType | Built-in/custom item category or template | Global and/or tenant-specific: TBD |
| Items | Asset | Item record, optional `PropertyId` | Yes |
| Items | AssetCustomField | Per-item field name/type/value | Yes; storage model TBD |
| Maintenance | MaintenanceRule | Recurrence and due criteria | Yes |
| Maintenance | MaintenanceOccurrence | Scheduled work instance and persisted status | Yes |
| Maintenance | MaintenanceCompletion | Completion details / service history evidence | Yes; separate table TBD |
| Vehicles | MileageReading | User-entered odometer readings | Yes |
| Files | StoredFile | Private object metadata, not file bytes | Yes |
| Files | Document | Document metadata, warranty/attachment associations | Yes |
| Notifications | NotificationOutbox | Durable event delivery work | Yes |
| Notifications | Notification | User-visible notification/inbox record | Yes |
| Devices | PushDeviceRegistration | Push token associated with user/device | Global or workspace-linked: TBD |

### 4.1 Proposed core relationships

```text
Workspace 1 ---- * WorkspaceMembership * ---- 1 GlobalUser
Workspace 1 ---- * Property
Workspace 1 ---- * Asset
Property  0..1 - * Asset       (Asset.PropertyId is nullable)
AssetType 1 ---- * Asset       (scope of AssetType TBD)
Asset     1 ---- * AssetCustomField  (if normalized design chosen)
Asset     1 ---- * MaintenanceRule
MaintenanceRule 1 ---- * MaintenanceOccurrence
Asset     1 ---- * MileageReading   (vehicle-only validation)
Asset     1 ---- * Document        (additional links TBD)
StoredFile 1 ---- * Document       (link design TBD)
Workspace 1 ---- * NotificationOutbox
```

**Important:** A Property may have maintenance of its own; do not hard-code all maintenance as `AssetId`-only before reviewing the maintenance spec. Options include separate target FKs or a typed target model; choose only after validation.

## 5. Proposed entity details and constraints

### 5.1 Workspace and membership

- `Workspace`: `Id`, `AbpTenantId` (if distinct), `Name`, lifecycle/audit fields.
- `WorkspaceMembership`: `Id`, `WorkspaceId`, `GlobalUserId`, `Role`, `Status`, audit fields.
- Proposed unique constraint: (`WorkspaceId`, `GlobalUserId`).
- Role values restricted to `Owner`, `Member` for MVP.
- **Open decision:** Ownership transfer and the rule preventing deletion of the last Owner.

### 5.2 Property

- Proposed: `Id`, `TenantId`, `Name`, optional address fields, audit fields.
- Proposed index: (`TenantId`, `Name`).
- Deletion/archive behavior and address requirements need confirmation.

### 5.3 Asset (UI: My Item)

- Proposed: `Id`, `TenantId`, `AssetTypeId`, nullable `PropertyId`, `Name`, optional make/model/year/serial data, archive/audit fields.
- **Enforce same-tenant references:** an Asset must not point to a Property or tenant-specific AssetType in another workspace.
- Item custom fields must not replace first-class columns required for filtering, security, or domain behavior.
- Asset type/category defaults versus per-item custom values need separate treatment.

### 5.4 Custom fields — options requiring decision

**Option A — JSONB on `Asset` (simple MVP):**

```json
{
  "tireSize": { "type": "text", "value": "235/45R18" },
  "lastInspection": { "type": "date", "value": "2026-10-15" }
}
```

Advantages: fewer joins, flexible per-item metadata. Risks: validation/versioning, field rename semantics, indexing, complex reporting.

**Option B — normalized fields:** `AssetCustomField(Id, TenantId, AssetId, Name, Type, ValueJson, SortOrder)`.

Advantages: field-level validation and querying. Risks: more rows and joins.

**Recommendation for review:** JSONB is a reasonable MVP candidate for strictly per-item custom values, with server-side type validation, bounded size/field count, and no sensitive secrets. This is **not approved** until the Item Types and Custom Fields specs are reconciled. Proposed supported types: text, number, date, boolean. Do not introduce a reusable template engine without an explicit requirement.

### 5.5 Maintenance

- `MaintenanceRule`: links to an approved target (Asset and/or Property); recurrence specification; active state; audit fields.
- `MaintenanceOccurrence`: links to a rule and target, scheduled due date, persisted `Status` enum (`Open`, `Completed`, `Skipped`, `Cancelled`), completion/skip/cancel timestamps or metadata as needed.
- Derived labels `Upcoming`, `Due`, `Overdue` are calculated from open status, due date, and business timezone/rules.
- Prevent duplicate occurrence generation using a deterministic rule+schedule key or equivalent idempotency constraint.
- Recurrence calculations, timezone behavior, rescheduling, and status transition rules must come from the dedicated maintenance scheduling spec.
- Service history is derived from occurrence and completion data; do not assume a separate history table is necessary.

### 5.6 Mileage

- `MileageReading`: proposed `Id`, `TenantId`, `AssetId`, `Reading`, `RecordedAt`, `CreatedByUserId`, audit fields.
- Validate that the target Asset is a vehicle category/type eligible for mileage.
- Store manually submitted readings only. No automatic estimates or generated mileage readings.
- Units (miles/kilometers), duplicate timestamps, and out-of-order corrections are **open decisions**.
- Do not model generator operating hours as vehicle mileage.

### 5.7 Files and documents

- Store file bytes in private object storage; PostgreSQL stores metadata and associations.
- Proposed `StoredFile`: `Id`, `TenantId`, opaque storage key, content type, size, checksum (optional), created-by/audit fields.
- Proposed `Document`: `Id`, `TenantId`, file reference, title, optional warranty metadata and association to Property/Asset.
- Object keys must not be treated as authorization. Validate workspace membership and ownership before issuing short-lived access URLs.
- Upload lifecycle, retention, deletion, and malware scanning requirements remain to be specified.

### 5.8 Notifications and outbox

- Persist durable notification work in an outbox within the same transaction as the triggering domain change where feasible.
- Worker delivery must be idempotent; retries and duplicate push delivery are possible.
- Keep a separate user-visible notification record if required by the single Notifications inbox UI.
- Device token handling, user preferences, deduplication, and retention policies need dedicated spec review.

## 6. Tenant isolation and security rules

**Proposed baseline; verify with `tenant-isolation.md`:**

1. All workspace-owned tables have a tenant discriminator and appropriate tenant-scoped indexes.
2. EF Core/ABP tenant filters are enabled by default; avoid unreviewed filter bypasses.
3. Server verifies membership and role before entering tenant-scoped business operations.
4. On create/update, validate cross-entity tenant consistency, not only row visibility.
5. Jobs and outbox workers explicitly set tenant context before accessing tenant data.
6. File metadata and object storage access enforce the same tenant boundary.
7. Administrative/host operations use separately authorized paths and audit logs.
8. Include negative tests for guessed IDs, workspace switching, forged tenant IDs, and cross-tenant foreign keys.
9. PostgreSQL row-level security is an optional defense-in-depth proposal, **not** a confirmed requirement.

## 7. PostgreSQL and EF Core conventions — proposed

- Use UUID/GUID primary keys unless existing Kepnora specs specify otherwise.
- Use UTC timestamps (`timestamptz`) for persisted instants; keep timezone identifiers where scheduling requires local wall-clock interpretation.
- Use explicit enum conversions and migrations; avoid accidental numeric enum meaning changes.
- Use optimistic concurrency for high-contention editable records where justified.
- Index tenant-scoped queries with `TenantId` as a leading column where query patterns support it.
- Audit creation/modification metadata consistently, using ABP audited entity conventions where appropriate.
- Prefer soft-delete/archive only when required by product behavior; define unique-index implications.
- Avoid storing passwords, payment details, access tokens, or document bytes in domain tables.

## 8. Data lifecycle and deletion — unresolved

Before migrations, decide: workspace deletion/retention; membership removal; property archive/delete behavior; asset archive/delete behavior; cascading effects on maintenance and documents; file object cleanup; GDPR/privacy retention obligations; and recovery requirements. **Do not select cascade delete defaults without reviewing these flows.**

## 9. Phase boundaries

**Phase 1 / MVP:** Core workspace/property/asset/maintenance/mileage/document/notification persistence described above.

**Phase 2 (later):** Find Services and any approved subscription/billing implementation. Pricing discussions include Free, Plus ($4.99/month or $49.90/year), and Family ($9.99/month or $99.90/year), with proposed workspace-level limits. **Do not create subscription tables or enforce proposed limits in M0 unless separately approved.**

**Phase 3 (later):** Booking and payments; not part of this schema baseline.

## 10. M0 POC acceptance criteria

- [ ] ABP version targeting .NET 10 is verified by a running application and test results.
- [ ] One global user can authenticate and join two independent workspaces without duplicate/ambiguous identities.
- [ ] Owner/Member membership is enforced server-side for each workspace.
- [ ] Switching workspace changes tenant context and visible data correctly.
- [ ] Cross-tenant read, write, relationship assignment, and file access are denied.
- [ ] Background job can resolve tenant context safely.
- [ ] PostgreSQL connection and minimal EF Core migration run successfully.
- [ ] Documented approach for global identity + ABP tenant identity is approved before business migrations.

## 11. Open decisions / review checklist

| ID | Decision | Priority |
|---|---|---|
| DB-01 | ABP global identity implementation and mapping to tenant membership | Blocker |
| DB-02 | Workspace domain table vs ABP tenant extension | Blocker |
| DB-03 | Membership lifecycle, invitation model, last-Owner protection | High |
| DB-04 | AssetType built-in/global versus workspace-custom scope | High |
| DB-05 | Custom fields JSONB vs normalized and exact supported types/limits | High |
| DB-06 | Maintenance target model (Property, Asset, or both) | Blocker before M4 |
| DB-07 | Recurrence timezone and idempotency key rules | High |
| DB-08 | Archive/delete/cascade/retention behavior | High |
| DB-09 | Document associations, upload lifecycle, and file retention | High |
| DB-10 | Mileage units and historical corrections | Medium |
| DB-11 | Notification outbox/inbox schema and delivery semantics | Medium |
| DB-12 | Subscription schema and enforcement timing | Later phase |

## 12. Implementation sequence

1. Reconcile this draft against actual Kepnora spec files; record discrepancies.
2. Complete ABP .NET 10 and global identity/tenant membership POCs.
3. Approve workspace and identity mapping (`DB-01`, `DB-02`).
4. Approve first-class entity boundaries and relationships.
5. Define EF Core mappings and tenant constraints.
6. Create the **minimal M0 foundation migration** only after architecture sign-off.
7. Add domain migrations incrementally with each later milestone; do not prebuild all Phase 1 tables in M0.
8. Add tenant isolation and migration tests to CI.

## 13. References to reconcile

- `docs/kepnora-project-context.md`
- `docs/architecture-overview.md`
- `docs/mvp-build-order.md`
- `docs/architecture/tenant-isolation.md`
- `docs/features/authentication-onboarding.md`
- `docs/features/asset-management.md`
- `docs/features/maintenance-management.md`
- `docs/features/documents-warranties.md`
- `docs/features/notifications-reminders.md`
- `docs/implementation/backend-project-structure.md`
- `docs/api/api-contracts-master.md`

**Approval gate:** Architecture review and global identity/tenant POC must pass before this draft becomes an approved database specification.
