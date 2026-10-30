# Kepnora — API Contracts Master Specification

**Target path:** `docs/api/api-contracts-master.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Status:** Proposed contract baseline for implementation and OpenAPI verification  
**Platform:** ASP.NET Core / .NET 10 / pinned ABP Framework, PostgreSQL, React Native + Expo

**Source documents:** `docs/database-overview.md` (authoritative proposed persistence design), `docs/architecture-overview.md`, `docs/architecture/tenant-isolation.md`, `docs/architecture/authentication-authorization.md`, `docs/features/asset-management.md`, `docs/features/maintenance-management.md`, `docs/features/notification-reminders.md`, `docs/features/dashboard-search.md`, `docs/features/workspace-settings.md`, `docs/testing/test-strategy.md`.

**Contract status legend**

- **Product invariant:** a behavior that must be preserved across API versions.
- **Proposed endpoint/DTO:** design for discussion and implementation; **not** a claim of an existing ABP-generated route.
- **M0 gate:** cannot be finalized until global identity/ABP POC selects authoritative user model and packages.
- **Open decision:** must be recorded in an ADR before coding a dependent feature.

---

## 1. Purpose and API ownership

This file defines a coherent **Phase 1 mobile-facing API surface**: authentication/session integration, workspace selection, membership, properties, asset types, assets, maintenance, observed odometer readings, documents, notifications, dashboard, search and settings.

ABP module-generated endpoints for identity, tenant management, permissions and audit logging **remain owned by ABP**. Do not hand-build duplicate ABP platform endpoints or database tables. This document specifies a thin Kepnora application-facing facade only where product workflows need one.


## 2. Phase 1 boundaries

| In scope | Out of scope |
|---|---|
| Global login and multi-workspace selection | Provider discovery / Places integration (Phase 2) |
| Property and standalone asset CRUD | Booking and payment (Phase 3) |
| Tenant/global versioned asset types | AI semantic search |
| Time- and mileage-based maintenance | Hybrid time-or-mileage rule in current schema |
| Actual manually entered odometer | Automatic/estimated mileage |
| Private S3 upload/finalize/download | Public file hosting |
| Expo push preferences and reminders | Guaranteed push delivery |
| Dashboard, search, settings | Full admin web console unless separately scoped |


## 3. Canonical cross-service invariants

1. Workspace equals one authoritative **ABP Tenant**; no custom parallel `Tenants` registry.
2. One global login subject may belong to several tenants **only after M0 POC verifies identity integration**.
3. Active `TenantMembers` membership **and named permission** are required for tenant data operations.
4. `ICurrentTenant` / `IMultiTenant` scope data; they are not authorization proof.
5. Tenant-owned entities have immutable non-null `TenantId`; global asset templates are the exception.
6. `Asset.PropertyId` is nullable and standalone assets work everywhere.
7. Custom asset fields use immutable schema versions and validated JSONB.
8. Maintenance occurrences persist only `Open`, `Completed`, `Skipped`, `Cancelled`.
9. `Upcoming`, `Due`, `Overdue`, `Unknown` are derived presentation classifications.
10. Mileage is **observed**, never interpolated, projected or silently zero-filled.
11. Maintenance completion inserts at most one scheduled service history row.
12. S3 objects are private; upload is not complete until authorized finalize succeeds.
13. Workers/outbox are durable, idempotent and tenant-safe; external push is not exactly once.
14. All response DTOs are explicit; do not expose EF entities, ABP internals or private S3 keys.
15. Endpoint names below are proposed and must be reconciled against generated OpenAPI.


## 4. Base URL, versioning and route strategy

**Proposed public facade prefix:** `/api/v1`. Tenant-owned resource routes use `/workspaces/{workspaceId}` where `workspaceId` is the ABP TenantId. Example:

```text
GET /api/v1/workspaces/{workspaceId}/assets
```

This is a proposed route shape, **not** ABP's automatic conventional controller naming. If the team chooses native ABP routes, record the final mapping in OpenAPI and update this document.

Breaking changes require `/api/v2` or another approved compatibility strategy. Maintain older mobile-client compatibility across store release delays and Expo OTA updates.


## 5. Authentication and tenant context

Use OIDC Authorization Code + PKCE with the selected identity provider. Mobile sends a bearer access token; do not put refresh tokens in URLs or plain AsyncStorage. Validate issuer, audience, expiry and signing keys.

```http
GET /api/v1/workspaces/{workspaceId}/assets HTTP/1.1
Authorization: Bearer <access_token>
Accept: application/json
X-Correlation-Id: <optional-client-correlation-id>
```

`workspaceId` is **untrusted input**. Backend resolves authenticated global subject, checks active membership and permission, then establishes tenant data scope. Never trust a client-supplied role header. Do not rely on token tenant claim alone for authorization.


## 6. Authorization policy table

| Resource/action | Proposed permission policy |
|---|---|
| Workspace read | Active member + workspace read |
| Workspace settings update | Active Owner + settings manage |
| Members list/manage | Active Owner or explicit named permission |
| Property/asset read | Active member + resource read |
| Property/asset write | Active member + resource manage |
| Asset type custom schema | Active member + type manage |
| Maintenance rule create/update | Active member + maintenance manage |
| Maintenance complete/skip/cancel | Active member + maintenance action |
| Documents upload/read/delete | Active member + document permission + owning resource |
| Notification preferences | Active member; own preferences |
| Dashboard/search | Active member + relevant read permission |

**Names are conceptual** until ABP permission definitions are pinned. Role labels alone must not substitute for permission checks.


## 7. Tenant-safe lookup rule

For a resource path `{workspaceId}/assets/{assetId}`, validate the membership first, then query using both tenant and entity ID. For a nested resource, verify the parent is in the same tenant. Prefer tenant-aware composite foreign keys in PostgreSQL.

```csharp
var asset = await db.Assets.SingleOrDefaultAsync(
    x => x.TenantId == workspaceId && x.Id == assetId, ct);
```

Return a neutral not-found response for a foreign resource rather than exposing its owner or existence.


## 8. Common response conventions

**Proposed defaults:** JSON camelCase; UTF-8; UTC timestamps in ISO 8601 with `Z`; calendar dates as `YYYY-MM-DD`; GUIDs as lowercase canonical strings; enum values as documented strings; `null` explicitly when meaningful.

List endpoints use `items`, `nextCursor`, optionally `totalCount` only when inexpensive and requested. Avoid unbounded arrays. Use server-generated identifiers and timestamps. Decimal currency amounts are JSON numbers only if clients preserve decimal semantics; otherwise agree on minor units or decimal strings before implementation.


## 9. Error contract

Use RFC 9457-style Problem Details where practical, while preserving ABP's actual error envelope if using native ABP endpoints. **Choose one mobile-visible mapping and verify it.**

```json
{
  "type": "https://api.kepnora.example/problems/concurrency-conflict",
  "title": "The resource changed",
  "status": 409,
  "code": "Kepnora:ConcurrencyConflict",
  "detail": "Refresh the item and try again.",
  "traceId": "00-illustrative-trace-id"
}
```

`type` URL is illustrative; do not claim the domain exists. Do not include SQL statements, stack traces, private record details or S3 URLs in public errors.


## 10. HTTP status semantics

| Status | Meaning |
|---|---|
| 200 | Successful read/update |
| 201 | Created, `Location` when appropriate |
| 202 | Accepted asynchronous provisioning/processing |
| 204 | Successful no-content operation |
| 400 | Malformed request |
| 401 | Missing/invalid session |
| 403 | Authenticated but not permitted |
| 404 | Missing or foreign resource, neutral |
| 409 | State/version conflict, last Owner guard, duplicate lifecycle action |
| 412 | Conditional request failed, if `If-Match` adopted |
| 422 | Valid JSON but domain validation failure |
| 429 | Rate limited |
| 500/503 | Server error / dependency unavailable |

Avoid exposing whether a foreign resource exists through 403 versus 404 differences. Standardize validation errors per field.


## 11. Pagination, filtering and sorting

**Proposed:** `limit` default 20, maximum 100; opaque `cursor`; allowlisted `sort` and `direction`. Cursor must bind to tenant, query filters and sort version. Do not accept cursors from another tenant. Use stable ID tie-breakers.

```text
GET /api/v1/workspaces/{workspaceId}/assets?limit=20&sort=name&cursor=...
```

If using offset pagination for early MVP, document deterministic ordering and upper bounds. Do not expose arbitrary SQL sort column names to clients.


## 12. Optimistic concurrency and idempotency

Use `Version` on entities where defined (`TenantMembers`, `MaintenanceRules`, `MaintenanceOccurrences`, etc.). Proposed write DTOs include `expectedVersion` or HTTP `If-Match`; select **one consistent strategy** per resource and test it.

Proposed `Idempotency-Key` header for create/complete operations with retry ambiguity. The database spec defines `IdempotencyKeys` as an **alternative implementation**, while ABP may provide infrastructure; select one. Request hash mismatch for a reused key must be rejected. Do not promise replay of identical responses until durable idempotency storage exists.


## 13. Workspace endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/api/v1/me/workspaces` | Active memberships and workspace summaries |
| POST | `/api/v1/workspaces` | Provision ABP tenant + Owner membership |
| GET | `/api/v1/workspaces/{workspaceId}` | Current workspace profile |
| PATCH | `/api/v1/workspaces/{workspaceId}` | Rename/timezone settings |
| GET | `/api/v1/workspaces/{workspaceId}/members` | Authorized member list |
| PATCH | `/api/v1/workspaces/{workspaceId}/members/{userId}` | Change role |
| DELETE | `/api/v1/workspaces/{workspaceId}/members/{userId}` | Remove membership |

No `POST /workspaces/{id}/select` is required unless the backend maintains an explicit active-workspace session. The client may select a workspace locally **but every subsequent request must authorize membership**. Avoid creating duplicate tenant management APIs already supplied by ABP.


## 14. Workspace creation DTO

```json
{
  "displayName": "Family Home",
  "defaultTimeZoneId": "America/Chicago"
}
```

**Response:** 201 with ready workspace or 202 with durable provisioning state if ABP tenant creation and Owner membership cannot be committed atomically. Never return “Ready” until the Owner membership exists. Duplicate retries must not create multiple ABP tenants. Global identity is an M0 gate.


## 15. Workspace membership DTOs

```json
{
  "items": [
    {
      "workspaceId": "5a8f2c6b-4e1d-4a6e-90b3-2f18f71a2f30",
      "displayName": "Family Home",
      "role": "Owner",
      "status": "Active"
    }
  ],
  "nextCursor": null
}
```

Role values: `Owner`, `Member`. Status values: `Active`, `Invited`, `Removed`. `Invited` exists in the schema but does **not** imply invitation endpoints are implemented. The last active Owner cannot be removed or demoted, including concurrent attempts.


## 16. Property endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/properties` | List active properties |
| POST | `/workspaces/{workspaceId}/properties` | Create property |
| GET | `/workspaces/{workspaceId}/properties/{propertyId}` | Detail |
| PATCH | `/workspaces/{workspaceId}/properties/{propertyId}` | Update |
| POST | `/workspaces/{workspaceId}/properties/{propertyId}/archive` | Archive |
| GET | `/workspaces/{workspaceId}/properties/{propertyId}/assets` | Related assets |

All routes above are relative to `/api/v1`. The property schema, address fields, optional timezone and archive behavior are governed by the database/property feature spec. **Deleting a property must not silently delete standalone assets or maintenance history.**


## 17. Property DTO example

```json
{
  "name": "Primary Residence",
  "timeZoneId": "America/Chicago",
  "address": {
    "line1": "Example Street",
    "city": "Example City",
    "region": "TX",
    "postalCode": "00000",
    "countryCode": "US"
  }
}
```

This is a **conceptual** create request; the canonical database may store address columns differently. Map exact fields after verifying `Properties` DDL and the dedicated feature contract. Do not create an `Address` JSONB column solely from this example.


## 18. Asset type endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/asset-types` | Global templates and tenant types visible to member |
| GET | `/asset-types/{typeId}` | Type summary |
| GET | `/asset-types/{typeId}/versions/{versionId}` | Immutable schema version |
| POST | `/workspaces/{workspaceId}/asset-types` | Create tenant-owned type |
| POST | `/workspaces/{workspaceId}/asset-types/{typeId}/versions` | Publish next immutable version |
| POST | `/workspaces/{workspaceId}/asset-types/{typeId}/archive` | Archive custom type if allowed |

For global templates, enforce visibility rules without granting access to other tenants' custom types. Define whether type list is tenant-scoped via workspace route instead of global route in final OpenAPI. Never update a published schema version in place.


## 19. Asset type schema DTO

```json
{
  "name": "Vehicle",
  "fields": [
    {
      "key": "vin",
      "label": "VIN",
      "type": "text",
      "required": false,
      "maxLength": 17
    },
    {
      "key": "purchaseYear",
      "label": "Year",
      "type": "integer",
      "required": false
    }
  ]
}
```

Illustrative schema DSL only; field types, key rules, conditional fields and version publishing semantics must match the custom-asset-types spec and validated JSONB schema. Unknown keys must not be blindly accepted.


## 20. Asset endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/assets` | List/filter/paginate |
| POST | `/workspaces/{workspaceId}/assets` | Create |
| GET | `/workspaces/{workspaceId}/assets/{assetId}` | Detail |
| PATCH | `/workspaces/{workspaceId}/assets/{assetId}` | Update |
| POST | `/workspaces/{workspaceId}/assets/{assetId}/archive` | Archive |
| POST | `/workspaces/{workspaceId}/assets/{assetId}/restore` | Restore if approved |
| GET | `/workspaces/{workspaceId}/assets/{assetId}/summary` | Detail card aggregates |

All relative to `/api/v1`. Asset creation must validate selected asset type version and custom values. `propertyId` is optional. Do not use hard delete for assets with maintenance history without an approved retention policy.


## 21. Asset create/update DTO

```json
{
  "name": "Family Car",
  "propertyId": null,
  "assetTypeVersionId": "af7c11a9-9d0c-4f2c-9a11-f79c92f4e710",
  "customFields": {
    "vin": "EXAMPLEVIN1234567",
    "purchaseYear": 2023
  }
}
```

Response should include stable `id`, tenant-scoped type/version references, validated custom fields, archive flag, and concurrency version if implemented. **Do not claim the exact JSON shape is already implemented**; it is a mobile-facing proposal.


## 22. Maintenance rule endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/assets/{assetId}/maintenance-rules` | List |
| POST | `/workspaces/{workspaceId}/assets/{assetId}/maintenance-rules` | Create |
| GET | `/workspaces/{workspaceId}/maintenance-rules/{ruleId}` | Detail |
| PATCH | `/workspaces/{workspaceId}/maintenance-rules/{ruleId}` | Update/version |
| POST | `/workspaces/{workspaceId}/maintenance-rules/{ruleId}/enable` | Enable |
| POST | `/workspaces/{workspaceId}/maintenance-rules/{ruleId}/disable` | Disable |

The canonical rule fields are `ScheduleType` (`Time`/`Mileage`), `AnchorPolicy` (`Scheduled`/`Completion`), `IntervalDays`, `IntervalMonths`, `IntervalMiles`, `StartOn`, `StartMileage`, `TimeZoneId`, `IsEnabled`, and `Version`. **Hybrid time-or-mileage is not in the current canonical schema.**


## 23. Maintenance rule create DTO

```json
{
  "title": "Oil change",
  "scheduleType": "Mileage",
  "anchorPolicy": "Scheduled",
  "intervalDays": null,
  "intervalMonths": null,
  "intervalMiles": 5000,
  "startOn": null,
  "startMileage": 25000,
  "timeZoneId": null
}
```

Validate positive intervals and mutually exclusive schedule fields. The first-occurrence convention (start value itself versus start + interval), completion anchor behavior and edit/revision policy are **open decisions**. Do not silently select them from this example.


## 24. Maintenance occurrence endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/maintenance-occurrences` | Filter by asset, lifecycle, due display |
| GET | `/workspaces/{workspaceId}/maintenance-occurrences/{occurrenceId}` | Detail |
| POST | `/workspaces/{workspaceId}/maintenance-occurrences/{occurrenceId}/complete` | Complete + service record |
| POST | `/workspaces/{workspaceId}/maintenance-occurrences/{occurrenceId}/skip` | Skip |
| POST | `/workspaces/{workspaceId}/maintenance-occurrences/{occurrenceId}/cancel` | Cancel |

Persist only `Open`, `Completed`, `Skipped`, `Cancelled`. Display `Upcoming`, `Due`, `Overdue` or mileage `Unknown` as **derived fields**, never as persisted lifecycle status. `DueOn` and `DueMileage` are nullable according to schedule type.


## 25. Occurrence response example

```json
{
  "id": "28d7b2e3-08b0-4c37-b20d-40cf1aa9ef13",
  "ruleId": "cde94a27-3a8c-4b3a-9175-813a4d284901",
  "assetId": "23c9e68d-6e3f-45ad-a0b7-6294c858ba2a",
  "scheduleKey": "MILE:30000",
  "lifecycleStatus": "Open",
  "displayStatus": "Due",
  "dueOn": null,
  "dueMileage": 30000,
  "latestObservedMileage": 30120,
  "version": 2
}
```

`displayStatus` is calculated from authorized current data. The server must distinguish “no reading” from a reading of zero. No mileage prediction is permitted.


## 26. Completion request and atomic behavior

```json
{
  "performedOn": "2026-11-12",
  "description": "Oil and filter replaced",
  "cost": 89.95,
  "currencyCode": "USD",
  "odometerMileage": 30520,
  "expectedVersion": 2
}
```

In one transaction: authorize, load occurrence/rule/asset in same tenant, require `Open`, check version, insert **one** `MaintenanceRecord`, set `Completed`, update completion anchors, write outbox intent, commit. The unique `(TenantId, OccurrenceId)` partial index prevents duplicate scheduled history. Retried completion must not create a second record.


## 27. Skip and cancel contracts

Proposed request:

```json
{
  "expectedVersion": 2,
  "reason": "No longer required"
}
```

`reason` is **not in canonical `MaintenanceOccurrences` DDL**; persist it only after an approved audit/metadata strategy. Skipping means not performed; cancelling means obligation withdrawn. Neither creates completed history. Whether skip advances a recurrence anchor is an open product decision.


## 28. Service history endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/assets/{assetId}/maintenance-records` | Service history |
| POST | `/workspaces/{workspaceId}/assets/{assetId}/maintenance-records` | Ad hoc record |
| GET | `/workspaces/{workspaceId}/maintenance-records/{recordId}` | Detail |
| PATCH | `/workspaces/{workspaceId}/maintenance-records/{recordId}` | Correction **only if approved** |

`MaintenanceRecords.OccurrenceId` is nullable for ad hoc service history. `AssetId` must match the occurrence's rule asset when linked. Correction requires an audit policy; never silently rewrite service history.


## 29. Odometer endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/assets/{assetId}/odometer-readings` | Observed history |
| POST | `/workspaces/{workspaceId}/assets/{assetId}/odometer-readings` | Add actual reading |
| POST | `/workspaces/{workspaceId}/odometer-readings/{readingId}/correct` | Correction, **deferred pending policy** |

```json
{
  "mileage": 30520,
  "recordedAt": "2026-11-12T16:30:00Z",
  "source": "Manual"
}
```

Canonical storage uses **miles**, not kilometers. No inference from date, no automatic odometer polling. Historical/late readings and odometer resets require explicit correction semantics.


## 30. Document and upload endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| POST | `/workspaces/{workspaceId}/upload-intents` | Authorize upload and presign PUT |
| POST | `/workspaces/{workspaceId}/upload-intents/{intentId}/finalize` | Verify S3 and persist metadata |
| GET | `/workspaces/{workspaceId}/documents` | Filter by owning asset/property |
| GET | `/workspaces/{workspaceId}/documents/{documentId}` | Metadata |
| POST | `/workspaces/{workspaceId}/documents/{documentId}/download-url` | Short-lived presigned GET |
| DELETE | `/workspaces/{workspaceId}/documents/{documentId}` | Logical delete/retention policy |

Private S3 objects are not accessible by permanent public URLs. Upload intent/finalize is a two-phase workflow; S3 and PostgreSQL are not atomically committed together.


## 31. Upload intent request/response

```json
{
  "assetId": "23c9e68d-6e3f-45ad-a0b7-6294c858ba2a",
  "fileName": "receipt.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 125420
}
```

```json
{
  "intentId": "7a39403a-bb43-4d54-b90c-cba27b88ad3c",
  "method": "PUT",
  "uploadUrl": "https://example.invalid/presigned-upload-placeholder",
  "requiredHeaders": {
    "Content-Type": "application/pdf"
  },
  "expiresAt": "2026-11-12T16:40:00Z"
}
```

Example URL is deliberately nonfunctional. Do not log, cache long-term or share presigned URLs. Enforce size/type constraints, resource ownership and object-key opacity.


## 32. Upload finalize semantics

Server checks intent belongs to tenant/user and resource, is not expired/consumed, and S3 object metadata matches expected key/size/type/checksum policy. Then persist `Documents` metadata and finalize the intent in one DB transaction. Repeat finalize must be idempotent under an approved key/state policy. Orphaned objects are removed by a bounded cleanup worker.

A successful PUT alone **must not** show a finalized document in asset detail. Do not return raw bucket keys in mobile DTOs.


## 33. Notification device endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| POST | `/api/v1/me/devices` | Register/rotate global-user Expo token |
| GET | `/api/v1/me/devices` | Authorized device summary |
| DELETE | `/api/v1/me/devices/{deviceId}` | Deactivate own device |

Device registrations are **global-user-owned**, not workspace-owned. Canonical schema stores encrypted token and token hash; do not log raw tokens or allow one user to deactivate another user's registration. Expo acceptance is not final delivery proof.


## 34. Notification preferences endpoints

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/workspaces/{workspaceId}/me/notification-preferences` | Read own per-workspace settings |
| PUT | `/workspaces/{workspaceId}/me/notification-preferences` | Update own settings |

```json
{
  "pushEnabled": true,
  "reminderDaysBefore": 7,
  "quietHoursStart": "22:00:00",
  "quietHoursEnd": "07:00:00"
}
```

Canonical `NotificationPreferences` is keyed `(TenantId, UserId)` with `ReminderDaysBefore` between 0 and 365. Quiet-hours timezone/DST semantics are open decisions. `pushEnabled=true` does not mean OS permission was granted.


## 35. Reminder delivery and worker contracts

Reminder planning, delivery, retry, dead-letter handling and outbox claims are **internal worker operations**, not general mobile CRUD endpoints. A privileged operational diagnostics API may be added later under separate permissions.

Use one selected outbox implementation: ABP distributed-event outbox **or** the custom `OutboxMessages` design. Never both for the same event. At dispatch, recheck membership, preferences, rule/occurrence state and token validity. Logical deduplication cannot guarantee exactly-once device presentation.


## 36. Dashboard endpoint

```text
GET /api/v1/workspaces/{workspaceId}/dashboard
```

```json
{
  "asOfUtc": "2026-10-29T19:00:00Z",
  "counts": {
    "activeAssets": 12,
    "activeProperties": 2,
    "openOccurrences": 9,
    "due": 3,
    "overdue": 2,
    "upcoming": 3,
    "unknown": 1
  },
  "attention": [],
  "recentActivity": []
}
```

Values are illustrative. Compute from actual tenant-scoped records; no separate dashboard truth table. Classifications depend on approved timezone/urgency policy, and buckets may not exhaust all open occurrences when preview windows apply.


## 37. Global workspace search endpoint

```text
GET /api/v1/workspaces/{workspaceId}/search?q=filter&types=asset,property,maintenance&limit=20&cursor=...
```

```json
{
  "query": "filter",
  "items": [
    {
      "kind": "asset",
      "id": "23c9e68d-6e3f-45ad-a0b7-6294c858ba2a",
      "title": "Air filter",
      "subtitle": "Primary Residence"
    }
  ],
  "nextCursor": null
}
```

Search only approved fields in selected tenant. Default excludes archived resources and file contents. Ranking, accent handling and cursor policy are proposed in the dashboard/search feature spec. Destination reauthorizes on navigation.


## 38. Profile and settings endpoints

Global profile editing must use the selected identity provider's supported APIs; do not fabricate tenant-scoped password/email endpoints.

| Method | Proposed route | Purpose |
|---|---|---|
| GET | `/api/v1/me` | Global profile summary |
| GET | `/workspaces/{workspaceId}` | Workspace profile |
| PATCH | `/workspaces/{workspaceId}` | Owner-managed workspace display/timezone |
| GET/PUT | `/workspaces/{workspaceId}/me/notification-preferences` | Own preference |
| POST/DELETE | `/api/v1/me/devices` | Global device management |

Persistent kilometers/unit preference is **not** in the canonical workspace profile schema; do not advertise it as saved without an ADR/migration.


## 39. Response redaction and field visibility

Never return: password hashes, refresh tokens, private signing keys, S3 object keys, raw device tokens, DB connection details, outbox payloads, tenant foreign records, or internal exception traces.

Avoid including exact property address in a general search result unless explicitly approved by privacy policy and permissions. Signed download/upload URLs are transient secrets; return only to the authorized caller and avoid storing in analytics.


## 40. API rate limiting and abuse controls

Set explicit limits per authenticated user, tenant and endpoint category. Sensitive operations include workspace provisioning, membership changes, upload intent creation, global search and device registration. Enforce body-size caps and input validation before costly DB/S3 work.

Return 429 with approved retry guidance. Do not reveal whether a foreign user/email/asset exists through timing or error wording. Final thresholds depend on load testing and product policy.


## 41. API contract testing

Every endpoint must have tests for:
- Valid authenticated request in active workspace.
- Missing/expired token.
- Non-member tenant.
- Member lacking named permission.
- Cross-tenant resource identifier.
- Invalid field/value and malformed JSON.
- Stale version and duplicate request where relevant.
- Pagination/cursor misuse across tenants.
- Archived parent resource.
- Workspace switch / in-flight mobile request.
- No private fields in error/response.

Run integration tests against real PostgreSQL and generated ABP configuration, not only mocks. The source test strategy contains additional race/worker/mobile scenarios.


## 42. Contract test matrix — core

| ID | Contract scenario | Expected |
|---|---|---|
| API-001 | List active workspaces | Only authenticated global subject memberships |
| API-002 | Owner X, Member Y | Tenant-specific permissions |
| API-003 | Non-member requests X | Denied |
| API-004 | Last Owner removal | Rejected, including concurrent attempts |
| API-005 | Duplicate workspace creation retry | No duplicate tenant |
| API-006 | Create property | Tenant-owned row |
| API-007 | Property from tenant X referenced in Y | Rejected |
| API-008 | Create standalone asset | `propertyId=null` accepted |
| API-009 | Asset type schema invalid | Validation error |
| API-010 | Published asset type version edit | Rejected |
| API-011 | Asset archived | Default lists exclude |
| API-012 | Asset list pagination | Stable, bounded |
| API-013 | Time rule invalid intervals | Rejected |
| API-014 | Mileage rule missing baseline | Policy-driven validation |
| API-015 | Hybrid schedule request | Rejected in MVP |
| API-016 | Duplicate occurrence generation | One schedule key |
| API-017 | Two simultaneous completions | One history row |
| API-018 | Completion stale version | Conflict |
| API-019 | Skip occurrence | No completed history |
| API-020 | Ad hoc service record | `occurrenceId=null` |
| API-021 | No mileage reading | Unknown |
| API-022 | Actual mileage at threshold | Due |
| API-023 | Cross-tenant odometer reading | Denied |
| API-024 | Presigned PUT expires | Cannot finalize invalid object |
| API-025 | Upload PUT without finalize | Not listed as document |


## 43. Contract test matrix — notifications and reads

| ID | Contract scenario | Expected |
|---|---|---|
| API-026 | Private S3 anonymous GET | Denied |
| API-027 | Download document after membership removal | Denied for new URL |
| API-028 | Device token logged | Prohibited |
| API-029 | Notification preference in X/Y | Independent |
| API-030 | Push disabled before dispatch | Suppressed |
| API-031 | Maintenance completed before reminder | Suppressed |
| API-032 | Expo accepts push | No false delivered claim |
| API-033 | Dashboard count standalone asset | Included |
| API-034 | Dashboard completed occurrence | Not overdue |
| API-035 | Search foreign tenant | No result |
| API-036 | Search archived resource | Excluded by default |
| API-037 | Cursor reused across tenants | Rejected |
| API-038 | Workspace switch mid-search | Old response discarded |
| API-039 | Problem Details | No internal traces/private IDs |
| API-040 | Retry with same idempotency key | No duplicate effect |
| API-041 | Reuse key with changed request hash | Rejected |
| API-042 | Revoked member with valid JWT | Denied |
| API-043 | Deep link to foreign occurrence | Denied |
| API-044 | Concurrent rule edit/complete | Consistent version policy |
| API-045 | Large tenant search/dashboard | Bounded queries and latency |


## 44. OpenAPI and client generation workflow

1. Pin .NET 10, ABP version, identity mode and serialization packages in M0.
2. Implement ABP AppServices/conventional controllers or reviewed facade controllers.
3. Generate OpenAPI from **running configured backend**, not handwritten guesses.
4. Compare routes, security schemes, nullability, enums, response codes and DTOs to this specification.
5. Generate TypeScript client for React Native from that OpenAPI.
6. Commit generated client and OpenAPI snapshot with controlled update procedure.
7. CI fails on unreviewed breaking changes and missing security requirements.
8. Update examples/docs when implementation changes are approved.

Never maintain divergent handwritten mobile DTOs as a competing source of truth.


## 45. Implementation ownership by milestone

| Milestone | Contracts |
|---|---|
| M0 | ABP identity, tenancy, OpenAPI, auth error strategy |
| M1 | Workspace membership, creation, selection, authorization |
| M2 | Properties and asset type definitions/versions |
| M3 | Assets and dynamic custom field validation |
| M4 | Rules, occurrences, completion, history, odometer |
| M5 | Upload intent, finalize, private document download |
| M6 | Device registration, preferences, reminder integration |
| M7 | Dashboard, global search, settings |
| M8 | Contract compatibility, security, load, mobile E2E |


## 46. Decisions requiring explicit ADRs

1. Host/global ABP Identity versus external OIDC global subject.
2. Native ABP conventional routes versus `/api/v1` facade.
3. Exact named permission policies and Owner/Member grants.
4. Workspace provisioning atomicity/reconciliation.
5. Global template endpoint visibility and tenant context.
6. Canonical custom field schema DSL.
7. Time/mileage first-occurrence convention and edit policy.
8. Completion without odometer anchor.
9. Skip/cancel reason persistence.
10. Idempotency infrastructure: ABP or custom table.
11. Outbox provider: ABP or custom, not both.
12. Upload MIME/checksum/retention policy.
13. Reminder timezone, quiet hours, lead-time rules.
14. Unit preference persistence and kilometer conversion.
15. Pagination/cursor and search collation policy.
16. Problem Details versus ABP error envelope.
17. ETag versus `expectedVersion`.
18. Decimal currency serialization and rounding.
19. Mobile API minimum version/compatibility policy.
20. Invitation workflow scope and contracts.


## 47. Definition of Done

- [ ] M0 identity and ABP route strategy proven.
- [ ] No duplicate ABP platform APIs or tenant registry.
- [ ] Every tenant endpoint verifies active membership + named permission.
- [ ] OpenAPI reflects actual deployed controllers and response types.
- [ ] Mobile TypeScript client generated from OpenAPI.
- [ ] Property is optional for standalone assets.
- [ ] Versioned JSONB custom fields validated.
- [ ] Maintenance lifecycle and derived urgency never conflated.
- [ ] Odometer readings never estimated.
- [ ] Completion and history are transactionally consistent.
- [ ] Upload intent/finalize and private downloads are secure.
- [ ] Notifications respect per-workspace preferences and current eligibility.
- [ ] Search/dashboard are tenant-scoped and bounded.
- [ ] Concurrency, idempotency and errors have documented behavior.
- [ ] API-001 through API-045 have passing tests or tracked exceptions.
- [ ] All open ADR decisions blocking implementation are resolved.


## 48. Final contract principle

**One authoritative domain model, one verified OpenAPI contract, one generated mobile client.** Proposed routes in this document guide implementation but do not supersede ABP-generated behavior until validated. Tenant isolation, factual maintenance state, private files and recoverable side effects are non-negotiable.

