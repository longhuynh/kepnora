# Kepnora — API Design and Application Service Contracts

**Intended path:** `docs/architecture/api-design.md`  
**Version:** 1.0-draft  
**Status:** Phase 1 API design specification — endpoint routes and ABP conventions require compiled POC verification  
**Platform:** .NET 10 · ASP.NET Core · ABP Framework · EF Core · PostgreSQL · React Native/Expo  
**Related:** `../architecture-overview.md`, `../database-overview.md`, `../mvp-build-order.md`, `tenant-isolation.md`, `authentication-authorization.md`, `background-jobs-and-outbox.md`, `file-storage.md`, `architecture-decision-records.md`

---

## 1. Purpose

Specify how Kepnora Phase 1 exposes stable, authorized, testable HTTP contracts for the mobile application. This document covers endpoint organization, ABP Application Services, DTOs, tenant context, validation, error responses, pagination, concurrency, idempotency, OpenAPI generation and representative feature workflows.

**Critical distinction:** All HTTP paths in this document are **proposed product routes**, not claims about what a particular ABP version auto-generates. The final route inventory comes from the running pinned ABP host and its generated OpenAPI document.

## 2. API design principles

1. The global identity authenticates a user; workspace membership authorizes a workspace.
2. One workspace corresponds to one ABP Tenant; no duplicate custom tenant registry.
3. A request cannot set its own trusted `TenantId`, owner identity or resource permissions.
4. DTOs are public contracts; domain entities and EF Core models are never exposed directly.
5. APIs use predictable HTTP semantics and consistent validation/error handling.
6. Mutations are atomic inside the database and idempotent when retries are expected.
7. Tenant-owned resource lookups are scoped by `(TenantId, Id)`.
8. Pagination and filtering occur server-side, never by downloading all records.
9. Published asset-type schema versions remain immutable.
10. Maintenance lifecycle and derived urgency state are distinct.
11. OpenAPI and generated TypeScript types prevent manual client/server drift.
12. External S3 and Expo Push operations are not part of a PostgreSQL transaction.

## 3. Scope and non-goals

**Included:** authentication integration, workspace list/provision/switch, members, properties, asset types, assets, maintenance rules/occurrences/history, mileage readings, documents, notification devices/preferences, dashboard, search and settings.

**Excluded:** provider discovery/quotes (Phase 2), booking/payments (Phase 3), a full customer web application, unrestricted cross-workspace search, public file sharing and unversioned dynamic schemas.

## 4. Framework compatibility gate

Before implementing routes, pin .NET, ABP edition/version, Identity/OpenIddict or external OIDC, EF Core, Npgsql, API controller strategy, validation mechanism, exception envelope and generated OpenAPI behavior. Compile a vertical-slice POC. Do not hardcode guessed ABP auto-controller names or assume one global user across tenant-local ABP Identity stores works out of the box.

## 5. Logical layers

```text
Expo screen / TanStack Query
    ↓ generated TypeScript API client
HTTP endpoint (ABP auto-controller or explicit controller)
    ↓ authentication + workspace authorization
Application.Contracts DTO + validation
    ↓ Application Service / ABP Unit of Work
Domain aggregate / domain service
    ↓ EF Core repository + PostgreSQL constraints
Outbox events / S3 presigning / worker handoff as appropriate
```

**Application.Contracts** defines request/response shapes and permission constants. **Application** orchestrates use cases. **Domain** owns invariants. **EntityFrameworkCore** owns persistence. **HttpApi** owns transport customization only when needed. **HttpApi.Host** owns authentication, middleware and OpenAPI.

## 6. Route conventions

Proposed versioned prefix: `/api/v1`. Workspace-owned resources use `/api/v1/workspaces/{workspaceId}/...`. Global identity/session and authorized workspace-list endpoints are outside the workspace prefix. Versioning mechanism (URL vs header vs ABP API versioning support) is an **open ADR subdecision**; examples use URL versioning for readability.

| Operation | Method | Pattern |
|---|---|---|
| Collection list | GET | `/api/v1/workspaces/{workspaceId}/assets` |
| Detail | GET | `/api/v1/workspaces/{workspaceId}/assets/{assetId}` |
| Create | POST | `/api/v1/workspaces/{workspaceId}/assets` |
| Replace/edit | PUT or PATCH | `/api/v1/workspaces/{workspaceId}/assets/{assetId}` |
| Archive | POST | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/archive` |
| Domain action | POST | `/api/v1/workspaces/{workspaceId}/maintenance/occurrences/{id}/complete` |

Do not introduce `TenantId` as a trusted query/body parameter. Route IDs are identifiers, not proof of access.

## 7. Authorization pipeline

1. Authenticate access token; validate issuer, audience, expiry and signature.
2. Resolve stable global user ID using the M0-approved identity strategy.
3. Parse requested workspace ID and reject conflicting tenant selection mechanisms.
4. Load **active** `TenantMembers` membership from trusted store.
5. Check named permission in the selected workspace.
6. Enter scoped `ICurrentTenant.Change(workspaceId)`.
7. Load domain resources by `(TenantId, Id)`; validate same-tenant references.
8. Execute business operation in ABP Unit of Work.
9. Return safe DTO and audit correlation ID; restore tenant context on exit.

`ICurrentTenant` and ABP `IMultiTenant` filters are data-scoping mechanisms, not replacements for membership authorization.

## 8. Proposed permission catalog

```csharp
public static class KepnoraPermissions
{
    public const string GroupName = "Kepnora";
    public static class Workspaces
    {
        public const string Read = "Kepnora.Workspaces.Read";
        public const string Manage = "Kepnora.Workspaces.Manage";
        public const string MembersManage = "Kepnora.Workspaces.Members.Manage";
    }
    public static class Properties
    {
        public const string Read = "Kepnora.Properties.Read";
        public const string Write = "Kepnora.Properties.Write";
    }
    public static class Assets
    {
        public const string Read = "Kepnora.Assets.Read";
        public const string Write = "Kepnora.Assets.Write";
    }
    public static class Maintenance
    {
        public const string Read = "Kepnora.Maintenance.Read";
        public const string Write = "Kepnora.Maintenance.Write";
        public const string Complete = "Kepnora.Maintenance.Complete";
    }
    public static class Documents
    {
        public const string Read = "Kepnora.Documents.Read";
        public const string Write = "Kepnora.Documents.Write";
    }
}
```

These constants are proposed Kepnora names, **not built-in ABP permissions**. Extend the catalog for asset types, settings and notifications after agreeing the Owner/Member matrix. Permission evaluation must remain workspace-aware.

## 9. Standard request conventions

- `Authorization: Bearer <access-token>` for protected requests.
- `Content-Type: application/json` for JSON mutations.
- `Accept: application/json` for JSON responses.
- `X-Correlation-Id` may be accepted or generated; never trust it for authorization.
- `Idempotency-Key` for retry-sensitive POST actions, subject to a documented scope/TTL.
- `If-Match` for versioned mutations **if** ETag strategy is selected; otherwise use a version field.
- Dates and timestamps use ISO 8601; persisted instants are UTC, while due dates retain rule-local semantics.
- Currency amounts use decimal precision and an explicit currency code if multi-currency is supported.

Avoid simultaneously requiring `If-Match`, ABP concurrency stamp and an independent numeric version unless a deliberate ADR defines their interaction.

## 10. Identifier conventions

Use UUIDs for internal entity IDs and tenant IDs. Never derive a tenant ID from a user-visible workspace name. A public UUID is not a secret. Use distinct route parameter names (`workspaceId`, `assetId`, `ruleId`, `occurrenceId`, `documentId`) to reduce accidental binding errors.

## 11. DTO naming and mapping

Proposed names: `CreateAssetInput`, `UpdateAssetInput`, `AssetDto`, `AssetListItemDto`, `GetAssetsInput`, `PagedResultDto<AssetListItemDto>`. Use ABP-provided DTO base types where they match the pinned version, but do not expose `IMultiTenant`, audit fields or EF navigation objects by accident.

DTO mapping must not allow client-controlled `TenantId`, `CreatedBy`, `ObjectKey`, `LifecycleStatus` transitions or derived due state. Whitelist editable fields explicitly.

## 12. Validation layers

| Layer | Responsibility | Example |
|---|---|---|
| Expo/Zod | Immediate user feedback | Required asset name |
| ASP.NET Core/ABP DTO validation | Shape, length, format | `Name` max length |
| Application Service | Authorization, cross-entity scope | Property belongs to tenant |
| Domain | Business invariants | Cannot complete cancelled occurrence |
| PostgreSQL | Last-line integrity | Unique schedule key, composite FK |

Client validation improves UX but is never authoritative. For dynamic asset types, validate `CustomFields` against the **published asset type version** on the server.

## 13. Response and error policy

Use the actual ABP exception envelope after POC; the following is a **conceptual normalized client error**, not a promise that ABP emits this JSON by default:

```json
{
  "code": "Kepnora:AssetTypeNotAvailable",
  "message": "The selected asset type is not available in this workspace.",
  "details": null,
  "correlationId": "7c0d55f5-1e8c-48ce-94a7-c3aab3bb5930"
}
```

| Status | Use | Example |
|---|---|---|
| 200 | Successful read/action with result | Asset detail |
| 201 | Resource created | Asset create |
| 204 | Success without body | Archive if selected contract |
| 400 | Invalid syntax/field | Invalid interval |
| 401 | Missing/expired login | No bearer token |
| 403 | Authenticated but forbidden | Non-member workspace |
| 404 | Not found or concealed | Cross-tenant asset ID |
| 409 | Concurrency/idempotency conflict | Already completed |
| 413 | Oversized upload | Receipt too large |
| 422 | Domain validation if chosen policy | Unsupported custom field combination |
| 429 | Rate limit | Too many upload intents |
| 500 | Unexpected failure | Correlation ID only |

Choose consistent 400 vs 422 mapping during M0/M1 and test it. Never leak another tenant's resource name, stack trace or S3 object key in an error.

## 14. Pagination and sorting

For typical lists, use `skipCount`, `maxResultCount`, `sorting` only if the chosen ABP DTO conventions and OpenAPI match; otherwise expose an explicit cursor or page token. Always cap page size server-side and enforce a stable secondary sort by ID.

```http
GET /api/v1/workspaces/{workspaceId}/assets?skipCount=0&maxResultCount=25&sorting=Name
```

Allowlist sort fields (`Name`, `CreatedAt`, `UpdatedAt`); never interpolate raw sorting strings into SQL. For very large lists, evaluate keyset pagination in a separate ADR.

## 15. Filtering and search

Filter by asset type, property, archive state and keyword within the validated tenant. Normalize keyword length and escape wildcard behavior. Search indexes and query plans are implementation details measured using PostgreSQL `EXPLAIN (ANALYZE, BUFFERS)`. Do not query all tenants and filter in memory.

## 16. Concurrency and retry semantics

For mutable aggregates, choose **one** concurrency strategy in ADR-023: ABP concurrency stamps or an explicit numeric version. A stale update returns a stable conflict. For creation and domain actions, use idempotency keys or unique natural keys where needed.

Maintenance completion must insert one service record, transition one occurrence and write its outbox event in a single database transaction. Duplicate requests must not create a second service record.

## 17. API versioning and compatibility

A change is breaking if it removes a field clients depend on, changes type/meaning, changes permission semantics or reinterprets an enum value. Additive optional fields are usually nonbreaking, but generated clients and validation must be tested. Maintain OpenAPI snapshots and a deprecation policy before public beta.

## 18. OpenAPI and TypeScript generation

The running host is the authoritative source for endpoint paths, response codes and DTO schema. CI should:

1. Build and run the pinned ABP host.
2. Export the OpenAPI specification.
3. Compare it with the approved contract snapshot.
4. Generate the TypeScript client using a pinned generator.
5. Typecheck mobile app and run contract tests.
6. Fail on unapproved breaking drift.

Do not manually duplicate request interfaces in React Native when they can be generated. Avoid treating documentation route examples as final until verified.

## 19. Mobile networking and state

Use a shared API client that attaches bearer tokens and correlation IDs. Refresh tokens through the approved OIDC flow; do not indiscriminately retry non-idempotent POSTs after a 401. Partition TanStack Query keys by workspace ID and cancel/invalidate in-flight tenant queries when switching workspaces.

```typescript
export const assetKeys = {
  list: (workspaceId: string, filters: AssetFilters) =>
    ['workspace', workspaceId, 'assets', filters] as const,
  detail: (workspaceId: string, assetId: string) =>
    ['workspace', workspaceId, 'asset', assetId] as const,
};
```

The mobile app may display stale/offline content explicitly, but must never show workspace X's cached private data while workspace Y is active.


## 20. Identity and Workspace APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 20.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/me` | Read current global profile | Authenticated |


| `GET` | `/api/v1/workspaces` | List active memberships | Authenticated |


| `POST` | `/api/v1/workspaces` | Provision workspace idempotently | Authenticated |


| `GET` | `/api/v1/workspaces/{workspaceId}` | Workspace detail | Member |


| `PATCH` | `/api/v1/workspaces/{workspaceId}` | Update workspace profile | Owner |


| `GET` | `/api/v1/workspaces/{workspaceId}/members` | List members | Owner |


| `POST` | `/api/v1/workspaces/{workspaceId}/members/invitations` | Invite member | Owner |


| `PATCH` | `/api/v1/workspaces/{workspaceId}/members/{memberId}` | Change member role/status | Owner |


### 20.2 Domain and implementation rules

Provisioning creates the ABP tenant, workspace profile and first Owner membership with idempotent recovery. The global user identity and invitation mechanism depend on the M0 POC. Switching workspaces is primarily a client selection plus server-side membership validation, not a trusted client claim.

### 20.3 Acceptance tests

- **API-20-01:** One global user can access two tenants with different roles.
- **API-20-02:** Revoked member cannot list tenant resources.
- **API-20-03:** Two concurrent provisioning retries produce one Ready workspace.
- **API-20-04:** Concurrent last-owner removals cannot leave zero Owners.

### 20.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 21. Property APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 21.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/properties` | List properties | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/properties` | Create property | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/properties/{propertyId}` | Get property | Read |


| `PATCH` | `/api/v1/workspaces/{workspaceId}/properties/{propertyId}` | Edit property | Write |


| `POST` | `/api/v1/workspaces/{workspaceId}/properties/{propertyId}/archive` | Archive property | Write |


### 21.2 Domain and implementation rules

Properties have tenant ownership, address and timezone validation. Property archive preserves associated assets and historical records. An asset is allowed to have no property.

### 21.3 Acceptance tests

- **API-21-01:** Tenant X cannot read or edit Property Y.
- **API-21-02:** Archive does not hard-delete asset history.
- **API-21-03:** Property timezone must be a supported identifier.

### 21.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 22. Asset Type and Schema APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 22.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/asset-types` | List global and tenant types | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/asset-types` | Create custom definition | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/asset-types/{typeId}/versions` | List versions | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/asset-types/{typeId}/versions` | Publish immutable version | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/asset-types/{typeId}/versions/{versionId}` | Read schema | Read |


### 22.2 Domain and implementation rules

Global definitions have null tenant ownership and are read-only to ordinary users. Tenant custom definitions belong to the workspace. Publishing a version is an immutable operation; editing the draft requires a new version.

### 22.3 Acceptance tests

- **API-22-01:** User cannot modify a global template.
- **API-22-02:** Tenant X cannot publish Tenant Y custom type.
- **API-22-03:** Published schema is immutable.
- **API-22-04:** Invalid JSONB custom attributes fail server validation.

### 22.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 23. Asset Inventory APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 23.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/assets` | List/filter assets | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/assets` | Create asset | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}` | Asset detail | Read |


| `PATCH` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}` | Edit asset | Write |


| `POST` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/archive` | Archive asset | Write |


### 23.2 Domain and implementation rules

Asset DTO contains name, type/version, optional property, validated custom fields and concurrency token. The server derives TenantId. PropertyId remains nullable for standalone cars and electronics.

### 23.3 Acceptance tests

- **API-23-01:** Create a car without PropertyId.
- **API-23-02:** Reject a property from another tenant.
- **API-23-03:** Reject a custom type from another tenant.
- **API-23-04:** Concurrent edits return stable conflict.

### 23.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 24. Maintenance Rule APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 24.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/maintenance-rules` | List asset rules | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/maintenance-rules` | Create rule | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/maintenance-rules/{ruleId}` | Rule detail | Read |


| `PATCH` | `/api/v1/workspaces/{workspaceId}/maintenance-rules/{ruleId}` | Edit rule | Write |


| `POST` | `/api/v1/workspaces/{workspaceId}/maintenance-rules/{ruleId}/deactivate` | Deactivate rule | Write |


### 24.2 Domain and implementation rules

Time and manual-mileage rules require a shared domain calculator. Rule anchoring, month-end, DST, grace periods and edit policy are open decisions in ADR-012. A rule edit must not silently rewrite completed history.

### 24.3 Acceptance tests

- **API-24-01:** No duplicate schedule key under retries.
- **API-24-02:** Jan-31 recurrence obeys approved clamp policy.
- **API-24-03:** Missing mileage reading never produces invented mileage.
- **API-24-04:** Rule edit preserves completed records.

### 24.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 25. Maintenance Occurrence and Service History APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 25.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/maintenance/occurrences` | List occurrences | Read |


| `GET` | `/api/v1/workspaces/{workspaceId}/maintenance/occurrences/{occurrenceId}` | Occurrence detail | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/maintenance/occurrences/{occurrenceId}/complete` | Complete occurrence | Complete |


| `POST` | `/api/v1/workspaces/{workspaceId}/maintenance/occurrences/{occurrenceId}/skip` | Skip occurrence | Write |


| `POST` | `/api/v1/workspaces/{workspaceId}/maintenance/occurrences/{occurrenceId}/cancel` | Cancel occurrence | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/service-history` | List service records | Read |


### 25.2 Domain and implementation rules

Persist only Open, Completed, Skipped and Cancelled. Derive Upcoming, Due and Overdue on read using rule-local date and mileage policy. Completion is atomic: occurrence, service record, completion anchor and outbox.

### 25.3 Acceptance tests

- **API-25-01:** Two concurrent completion calls create exactly one record.
- **API-25-02:** Cross-tenant occurrence ID is denied.
- **API-25-03:** Overdue is never persisted as lifecycle status.
- **API-25-04:** Dashboard uses the same due calculator.

### 25.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 26. Mileage Reading APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 26.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/odometer-readings` | List readings | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/odometer-readings` | Record manual reading | Write |


| `POST` | `/api/v1/workspaces/{workspaceId}/odometer-readings/{readingId}/correct` | Correct reading with audit | Write |


### 26.2 Domain and implementation rules

Mileage readings are explicitly user-recorded; they are not inferred from dates. Unit, effective date, correction policy and decreasing-reading behavior must be documented before implementation.

### 26.3 Acceptance tests

- **API-26-01:** No reading returns Unknown mileage due status.
- **API-26-02:** Correction preserves audit trail.
- **API-26-03:** Cannot write a reading for another tenant asset.

### 26.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 27. Document and S3 APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 27.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `POST` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/documents/upload-intents` | Create presigned upload intent | Write |


| `POST` | `/api/v1/workspaces/{workspaceId}/documents/upload-intents/{intentId}/finalize` | Verify and finalize | Write |


| `GET` | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/documents` | List metadata | Read |


| `GET` | `/api/v1/workspaces/{workspaceId}/documents/{documentId}` | Document detail | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/documents/{documentId}/download-url` | Authorized presigned GET | Read |


| `POST` | `/api/v1/workspaces/{workspaceId}/documents/{documentId}/archive` | Archive document | Write |


### 27.2 Domain and implementation rules

Binary bytes go to private S3. The backend owns object keys, validates actual uploaded object metadata, then creates Document metadata in a database transaction. Already-issued presigned URLs may remain usable until expiry after membership revocation.

### 27.3 Acceptance tests

- **API-27-01:** Cross-tenant presign/finalize/download denied.
- **API-27-02:** Successful PUT alone creates no Document.
- **API-27-03:** Duplicate finalize returns one logical Document.
- **API-27-04:** Cleanup cannot delete a finalized object.

### 27.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 28. Notifications and Device APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 28.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `POST` | `/api/v1/me/devices` | Register device token | Authenticated |


| `DELETE` | `/api/v1/me/devices/{deviceId}` | Revoke device token | Authenticated |


| `GET` | `/api/v1/workspaces/{workspaceId}/notification-preferences` | Read preferences | Member |


| `PUT` | `/api/v1/workspaces/{workspaceId}/notification-preferences` | Update preferences | Member |


| `GET` | `/api/v1/workspaces/{workspaceId}/notifications` | List in-app notifications if approved | Member |


### 28.2 Domain and implementation rules

Device tokens belong to the global user; preferences and reminder deliveries are tenant scoped. Reminder planning and Expo push are worker workflows, not synchronous side effects of HTTP mutation. In-app notification inbox is optional and must not be claimed as implemented without a schema decision.

### 28.3 Acceptance tests

- **API-28-01:** Revoked membership suppresses pending reminders.
- **API-28-02:** Completed occurrence suppresses reminder.
- **API-28-03:** Worker retry cannot create duplicate delivery intent.
- **API-28-04:** Device token is not logged.

### 28.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 29. Dashboard, Search and Settings APIs

**Contract status:** Proposed; confirm routes, permission constants and DTOs after ABP POC.

### 29.1 Endpoint inventory

| Method | Proposed route | Use case | Authorization |
|---|---|---|---|


| `GET` | `/api/v1/workspaces/{workspaceId}/dashboard` | Tenant-scoped summary | Read |


| `GET` | `/api/v1/workspaces/{workspaceId}/search` | Tenant-scoped asset/property/maintenance search | Read |


| `GET` | `/api/v1/me/settings` | Global profile settings | Authenticated |


| `PUT` | `/api/v1/me/settings` | Update global settings | Authenticated |


| `GET` | `/api/v1/workspaces/{workspaceId}/settings` | Workspace settings | Member |


| `PUT` | `/api/v1/workspaces/{workspaceId}/settings` | Update workspace settings | Owner |


### 29.2 Domain and implementation rules

Dashboard due counts must use the shared maintenance due calculator. Search must be paginated and tenant scoped. Separate user-global preferences from workspace-specific settings; do not duplicate data ambiguously.

### 29.3 Acceptance tests

- **API-29-01:** Search for Y asset from X returns no result.
- **API-29-02:** Due counts agree with occurrence API.
- **API-29-03:** Switching X to Y cannot show cached X dashboard.
- **API-29-04:** Queries have measured PostgreSQL plans.

### 29.4 Negative and retry cases

- Anonymous caller receives 401 for protected endpoints.
- Authenticated non-member cannot access the workspace.
- Resource ID belonging to another tenant is denied without exposing metadata.
- Invalid DTO fields return stable, safe validation errors.
- Retried mutations do not duplicate side effects; concurrency is checked where applicable.
- Mobile client displays loading, empty, success and recoverable error states.


## 30. Representative asset create contract

```json
{
  "name": "Toyota Camry 2022",
  "propertyId": null,
  "assetTypeVersionId": "e02c9ef4-1ac7-4f47-b6d7-b8efc77f7d6a",
  "customFields": {
    "make": "Toyota",
    "model": "Camry",
    "year": 2022,
    "mileage": 24300
  }
}
```

The example is illustrative. The actual custom field names, allowed types and mileage ownership must match the published type schema. `TenantId` and ownership fields are never accepted from the client. If mileage is maintained through a separate reading model, the create DTO must not silently duplicate its authoritative value.

## 31. Representative asset list response

```json
{
  "totalCount": 2,
  "items": [
    {
      "id": "11111111-1111-4111-8111-111111111111",
      "name": "Toyota Camry 2022",
      "propertyId": null,
      "isArchived": false
    },
    {
      "id": "22222222-2222-4222-8222-222222222222",
      "name": "Home AC Unit",
      "propertyId": "33333333-3333-4333-8333-333333333333",
      "isArchived": false
    }
  ]
}
```

The response envelope must match the chosen ABP DTO and generated OpenAPI; this is only an illustrative representation.

## 32. Representative maintenance completion command

```json
{
  "completedAt": "2026-10-29T22:00:00Z",
  "cost": 49.99,
  "currency": "USD",
  "notes": "Oil and filter replaced",
  "idempotencyKey": "8c82b985-f6a7-46e5-a76e-6a2a06b2d9f1"
}
```

The server loads occurrence and rule under the validated tenant, verifies it is Open, inserts one history record, transitions to Completed, updates completion-anchored schedule state when relevant and persists the outbox event atomically. The currency field and exact DTO must be reconciled with the database schema before implementation.

## 33. C# Application Service pattern

```csharp
public sealed record CreateAssetInput(
    string Name,
    Guid? PropertyId,
    Guid AssetTypeVersionId,
    JsonElement CustomFields);

public interface IAssetAppService
{
    Task<AssetDto> CreateAsync(
        Guid workspaceId,
        CreateAssetInput input,
        CancellationToken cancellationToken = default);
}

public sealed class AssetAppService : ApplicationService, IAssetAppService
{
    // Proposed dependencies: verify signatures against pinned ABP packages.
    private readonly IWorkspaceAccessService _access;
    private readonly ICurrentTenant _currentTenant;
    private readonly IAssetRepository _assets;
    private readonly IAssetDomainService _domain;

    public async Task<AssetDto> CreateAsync(
        Guid workspaceId,
        CreateAssetInput input,
        CancellationToken ct = default)
    {
        var userId = _globalUserContext.RequireUserId();
        await _access.RequireAsync(
            workspaceId, userId, KepnoraPermissions.Assets.Write, ct);

        using (_currentTenant.Change(workspaceId))
        {
            var asset = await _domain.CreateAsync(
                workspaceId, input.Name, input.PropertyId,
                input.AssetTypeVersionId, input.CustomFields, ct);

            await _assets.InsertAsync(asset, autoSave: true, cancellationToken: ct);
            return ObjectMapper.Map<Asset, AssetDto>(asset);
        }
    }
}
```

**Compilation note:** `_globalUserContext`, `IAssetRepository`, `IAssetDomainService` and the permission evaluator are proposed project abstractions, not standard ABP APIs. This code is intentionally illustrative, not a verified compiling source file. Do not paste it without dependency injection, field definitions, actual ABP API signatures and Unit of Work integration.

## 34. PostgreSQL query examples

```sql
SELECT
    A."Id",
    A."Name",
    A."PropertyId"
FROM "Assets" AS A
WHERE A."TenantId" = @TenantId
  AND A."IsArchived" = FALSE
ORDER BY
    A."Name",
    A."Id"
LIMIT @Limit
OFFSET @Offset;
```

Parameter placeholders are illustrative; actual Npgsql binding depends on the API. For large offsets, evaluate keyset pagination. Use PostgreSQL syntax, double-quoted PascalCase identifiers and named constraints, never SQL Server-specific syntax.

## 35. API idempotency store design

A general-purpose idempotency mechanism is optional, but the semantics must be explicit for operations that require it. Proposed key scope: `(TenantId, UserId, OperationName, IdempotencyKey)`, with a request hash, result reference, status and expiry. Do not return a prior result when the same key is replayed with a different request payload. Maintenance occurrence uniqueness and document intent uniqueness remain mandatory even if a generic store is introduced.

## 36. Rate limiting and abuse prevention

Apply limits to authentication-adjacent endpoints, workspace creation, member invitations, upload intents, presigned downloads and expensive search queries. Rate limits should distinguish IP, authenticated user and workspace as appropriate. Return 429 with a documented retry policy. Do not rely on mobile-only throttling.

## 37. Audit and observability

Record correlation ID, global user/service identity, workspace ID, permission, operation, resource ID, duration and outcome. Never log access tokens, refresh tokens, S3 presigned URLs, raw file bytes or sensitive custom field payloads. Monitor 401/403/404 patterns, 409 conflicts, API p95 latency, database pool exhaustion and background queue backlog.

## 38. Security integration test matrix

| ID | Scenario | Expected |
|---|---|---|
| SEC-001 | Anonymous lists assets | 401 |
| SEC-002 | User with no membership lists workspace | 403 |
| SEC-003 | Owner X accesses X assets | Allowed |
| SEC-004 | Member Y manages members | 403 |
| SEC-005 | User X requests Y asset UUID | Safe 404 |
| SEC-006 | X asset references Y property | Rejected |
| SEC-007 | X asset uses Y custom asset type | Rejected |
| SEC-008 | Client posts TenantId in create body | Ignored/rejected |
| SEC-009 | Stale concurrency token | 409 |
| SEC-010 | Concurrent maintenance completion | One record |
| SEC-011 | Duplicate occurrence generation | One occurrence |
| SEC-012 | Cross-tenant S3 upload intent | Denied |
| SEC-013 | Finalize missing S3 object | No Document |
| SEC-014 | Revoked member downloads document | Denied |
| SEC-015 | Worker switches X then Y | No context leak |
| SEC-016 | Dashboard aggregates X and Y | X excludes Y |
| SEC-017 | Search Y asset from X | No result |
| SEC-018 | Workspace switch during mobile fetch | No stale data shown |
| SEC-019 | Same global user Owner X/Member Y | Correct permissions |
| SEC-020 | Token still valid after membership revocation | Access denied within approved bound |

## 39. Performance test matrix

| Scenario | Representative fixture | Measurement |
|---|---|---|
| Asset list | 100 / 10k / 100k assets | p50/p95, query plan |
| Dashboard | Many occurrences/history rows | p95 and buffers |
| Search | Large name/serial corpus | p95 and index use |
| Workspace listing | Multi-membership user | latency and membership cache |
| Completion | Concurrent mobile retries | conflicts, duplicates |
| Presign/finalize | S3 latency/failure | error rate and retries |
| Reminder API state | Pending outbox backlog | consistency and lag |

Performance budgets must be established after measuring actual hardware, database size and concurrency; do not invent fixed SLA guarantees.

## 40. API delivery checklist

- [ ] ABP version, identity model and controller strategy are pinned.
- [ ] Global user across multiple workspaces POC passes.
- [ ] Every workspace endpoint checks membership and named permission.
- [ ] TenantId is derived server-side and cannot be overwritten by DTO mapping.
- [ ] DTO validation and JSONB schema validation are enforced.
- [ ] Cross-tenant foreign keys and domain references are tested.
- [ ] Pagination, filtering and sorting are bounded and deterministic.
- [ ] Concurrency and idempotency semantics are documented and tested.
- [ ] Errors are mapped to the actual ABP envelope and mobile error model.
- [ ] OpenAPI matches the running host and TypeScript client is regenerated.
- [ ] S3 and push side effects remain outside DB atomicity assumptions.
- [ ] All feature-specific and security tests pass on real PostgreSQL.
- [ ] Logs redact tokens, private document data and signed URLs.
- [ ] Mobile cache is partitioned by workspace.
- [ ] API contract snapshot and implementation evidence are recorded.

## 41. Milestone implementation order

1. **M0:** ABP/.NET 10, global identity, tenant resolver and OpenAPI POC.
2. **M1:** Workspace membership, provisioning, permissions and session integration.
3. **M2:** Property and versioned asset-type contracts.
4. **M3:** Asset CRUD, tenant-safe relationships, pagination and optimistic concurrency.
5. **M4:** Maintenance rules, occurrences, mileage and history.
6. **M5:** Private S3 upload intent/finalize/download.
7. **M6:** Device registration, preferences and durable reminder workers.
8. **M7:** Dashboard, search and settings.
9. **M8:** Contract freeze, load tests, security regression and beta release gate.

## 42. Open decisions register

| Decision | Why it matters | Gate |
|---|---|---|
| ABP version/edition and Identity model | Actual API and global user mapping | M0 |
| Explicit controllers vs ABP auto-controllers | Route and OpenAPI consistency | M0 |
| API versioning strategy | Backward compatibility | M0 |
| Permission integration | Role scoping across tenants | M0/M1 |
| Error envelope and 400 vs 422 policy | Mobile error mapper | M1 |
| Concurrency stamp vs numeric version | Update conflict contract | M1/M3 |
| Recurrence anchoring and timezone policy | Maintenance due accuracy | M4 |
| Mileage correction policy | Reliable due calculations | M4 |
| S3 TTL, size, scan and immutability | Private document safety | M5 |
| Push provider/outbox implementation | Worker reliability | M0/M6 |
| Search index strategy | Scale and latency | M7 |
| Retention and deletion | Privacy and recovery | M8 |

## 43. Final principle

**Authenticate the user, authorize the workspace, scope the data, enforce the domain rule, and publish a verified contract.** This sequence applies to every Kepnora API and cannot be replaced by a tenant header, a client-side permission check or a guessed ABP convention.
