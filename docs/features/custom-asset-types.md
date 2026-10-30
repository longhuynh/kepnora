# Kepnora — Custom Asset Types: Global Templates, JSONB and Versioned Schemas

**Target path:** `docs/features/custom-asset-types.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Scope:** Phase 1 / M2–M3 · **Status:** Implementation specification with explicitly identified open decisions

**Canonical references:** `docs/architecture/database-overview.md` §§11–12; `docs/features/asset-management.md`; `docs/api/api-contracts-master.md`; `docs/architecture/tenant-isolation.md`; `docs/mvp-build-order.md`. The exact schema dialect, endpoints, and code samples below are **proposals**, not claims of implemented APIs.

---
## 1. Purpose

Allow Kepnora to support different kinds of assets—vehicles, appliances, HVAC systems, electronics and custom equipment—without creating a database column for every type-specific attribute. Each asset points to a **published, immutable asset type version** and stores its validated type-specific values in PostgreSQL JSONB. An asset may exist without a property.


## 2. Phase 1 scope

**Included:** read-only global templates, workspace-owned custom types, schema version publication, type selection, server-side validation, version-pinned asset creation and editing, safe schema evolution, archived type behavior, and mobile schema-driven forms.

**Excluded unless separately approved:** third-party template marketplace, user-executable validation scripts, arbitrary nested schemas, file contents inside JSONB, automatic data migration of every asset, schema-driven workflow automation, and provider discovery/booking.


## 3. Invariants

1. `AssetTypeDefinitions.TenantId IS NULL` means global; non-null means tenant-owned.
2. Global templates are readable by eligible tenants but editable only through a separate platform-admin boundary.
3. Workspace members may never read or modify another tenant’s custom type.
4. Every asset stores both `AssetTypeDefinitionId` and `AssetTypeVersionId`.
5. `AssetTypeVersions.SchemaJson` is an object and a **published version is immutable**.
6. `Assets.CustomFields` is an object, validated against its **pinned** version.
7. The FK from assets to type versions proves definition/version consistency, **not** tenant eligibility.
8. `Assets.PropertyId` remains nullable.
9. A type's new version does not automatically change existing assets.
10. No client-generated schema or UI validation substitutes for server validation.


## 4. Source-of-truth database model

The canonical proposed PostgreSQL tables are:

| Table | Relevant columns |
|---|---|
| `AssetTypeDefinitions` | `Id UUID`, `TenantId UUID NULL`, `Code VARCHAR(100)`, `DisplayName VARCHAR(160)`, `IsActive BOOLEAN`, `CreatedAt TIMESTAMPTZ` |
| `AssetTypeVersions` | `Id UUID`, `AssetTypeDefinitionId UUID`, `VersionNumber INTEGER`, `SchemaJson JSONB`, `PublishedAt TIMESTAMPTZ` |
| `Assets` | `TenantId`, `AssetTypeDefinitionId`, `AssetTypeVersionId`, `CustomFields JSONB`, `PropertyId UUID NULL`, `Version BIGINT` |

The canonical definition table does **not** currently have `Description`, `Icon`, `Draft`, `IsSystem`, `UpdatedAt` or `LatestVersionId` columns. Do not invent them in EF entities or migrations without an ADR. Publishing a new version is modeled by inserting a row; drafts are client/editor state or a separately approved extension.


## 5. Canonical PostgreSQL constraints

```sql
-- Excerpts from the canonical design, not a new migration.
CREATE UNIQUE INDEX "UX_AssetTypeDefinitions_GlobalCode"
  ON "AssetTypeDefinitions" ("Code") WHERE "TenantId" IS NULL;

CREATE UNIQUE INDEX "UX_AssetTypeDefinitions_TenantCode"
  ON "AssetTypeDefinitions" ("TenantId", "Code") WHERE "TenantId" IS NOT NULL;

-- Existing version uniqueness and definition/version FK are required:
-- UNIQUE ("AssetTypeDefinitionId", "VersionNumber")
-- UNIQUE ("AssetTypeDefinitionId", "Id")
-- "Assets" ("AssetTypeDefinitionId", "AssetTypeVersionId")
--   REFERENCES "AssetTypeVersions" ("AssetTypeDefinitionId", "Id")
-- CHECK (jsonb_typeof("SchemaJson") = 'object')
-- CHECK (jsonb_typeof("CustomFields") = 'object')
```

A DB-level JSON object check does **not** validate field names, enum membership, requiredness or range. Those belong to the application/domain validation pipeline.


## 6. Global versus tenant-owned catalog

| Operation | Global definition | Current-tenant definition | Other tenant definition |
|---|---|---|---|
| List visible types | Yes | Yes | No |
| Read published version | Yes | Yes | No |
| Create definition | Platform admin only | Authorized member | No |
| Publish version | Platform admin only | Authorized member | No |
| Deactivate | Platform admin only | Authorized member, policy dependent | No |
| Create asset with type | Yes | Yes | No |

Do not interpret ABP `ICurrentTenant` as membership. Resolve global user → active `TenantMembers` → named permission, then evaluate type eligibility.


## 7. Catalog query and visibility predicate

```csharp
// Illustrative; require active membership before this query.
var visible = db.AssetTypeDefinitions.Where(t =>
    t.IsActive && (t.TenantId == null || t.TenantId == workspaceId));
```

For a requested version, join `AssetTypeVersions` to the definition and verify `version.AssetTypeDefinitionId == definition.Id`. If the definition is foreign, return a neutral 404/denial. Do not leak another tenant’s custom code, label or version metadata.


## 8. Definition identity and code policy

`Code` is stable, unique within its catalog scope, and suitable for internal references. `DisplayName` is user-facing and can be renamed if the product allows. Do not use `DisplayName` as a unique identifier.

**Proposed validation:** code is normalized lowercase ASCII with digits and hyphens, 2–100 chars; display name 1–160 chars trimmed. **Open decision:** whether a tenant may use the same `Code` as a global template. Current DB indexes permit it; API must disambiguate by `Id` and scope.


## 9. Schema dialect decision

The asset-management spec proposes a **Kepnora-specific constrained schema dialect**, but explicitly allows choosing an established JSON Schema subset instead. This file documents the proposed constrained dialect for M2 planning; it is **not yet an approved standard**.

Do not implement both partially. Before coding, decide supported dialect, `schemaVersion` semantics, error messages and shared test fixtures; record the choice in an ADR. The schema's `schemaVersion` describes the **dialect**, while `AssetTypeVersions.VersionNumber` describes the **published definition revision**.


## 10. Example published schema

```json
{
  "schemaVersion": 1,
  "fields": [
    {
      "key": "vin",
      "label": "VIN",
      "type": "string",
      "required": false,
      "maxLength": 17
    },
    {
      "key": "modelYear",
      "label": "Model year",
      "type": "integer",
      "required": false,
      "min": 1886,
      "max": 2100
    },
    {
      "key": "fuelType",
      "label": "Fuel type",
      "type": "select",
      "required": false,
      "options": ["Gasoline", "Diesel", "Hybrid", "Electric"]
    }
  ]
}
```

This is a proposed contract example copied in spirit from the canonical asset-management specification. It is not full JSON Schema and must not be treated as such by generic validators.


## 11. Supported MVP field types

| `type` | JSON value | UI widget | Server validation |
|---|---|---|---|
| `string` | String | Text | Max length, optional pattern/normalization |
| `integer` | Integer number | Numeric input | Exact integer, min/max |
| `decimal` | Decimal number/approved string | Decimal input | Precision, scale, min/max |
| `boolean` | Boolean | Switch | Strict true/false |
| `date` | `YYYY-MM-DD` string | Date picker | Real calendar date, optional range |
| `select` | String | Picker | Value in published options |
| `multiSelect` | Array of strings | Multi-picker | Allowed options, unique, bounded |

Nested object, arbitrary array, geographic field, rich text, computed field and binary file fields are outside MVP unless approved. Documents are separate entities, not base64 JSONB values.


## 12. Requiredness and null semantics

Decide and document whether `required=true` means a key must exist and be non-null/non-empty; recommended behavior is reject absent/null/empty values for required fields. For optional fields, distinguish omitted key from explicit `null` and normalize consistently. Do not coerce `"false"` to boolean false or `"42"` to integer 42 without an explicit transport policy. Avoid hidden client defaults that change business meaning.


## 13. Schema publication validation

On publish, validate: root object, recognized dialect version, `fields` array, field count cap, unique normalized keys, required `key`/`label`/`type`, allowed type-specific attributes, bounded lengths, sensible numeric ranges, unique select options, safe patterns, and total serialized schema size. Reject unsupported properties rather than silently ignoring them. Use structured field-path errors such as `fields[2].options` without returning internal exceptions.


## 14. Reserved field keys

Prohibit collisions with standard fields and metadata, including: `id`, `tenantId`, `propertyId`, `name`, `manufacturer`, `model`, `serialNumber`, `assetTypeDefinitionId`, `assetTypeVersionId`, `isArchived`, `version`, `createdAt`, `updatedAt`. Compare under an approved normalization/case policy. Also prohibit prototype-pollution keys such as `__proto__`, `constructor`, `prototype` in JS-facing schema dialects even though JSONB itself is not a JavaScript object prototype.


## 15. Field key stability

Published field keys identify stored values. A new version may add a new key; changing the meaning of an existing key is a breaking change. Labels may evolve in a new version but old assets continue rendering from their pinned version. Prefer new keys for semantic changes and explicit migration for old values. Never rename a key silently during an asset read.


## 16. Schema version publishing algorithm

1. Authorize global catalog admin or active workspace member with type-management permission.
2. Load active definition and current latest published version.
3. Validate proposed schema and enforce publication policy.
4. Acquire concurrency protection (row lock/advisory lock or equivalent).
5. Compute next `VersionNumber` deterministically.
6. Insert **new** `AssetTypeVersions` row with new `Id` and `PublishedAt`.
7. Commit and return published version metadata.
8. Invalidate tenant-scoped catalog caches.

Concurrent publish attempts must not create duplicate version numbers. The unique DB constraint is the final safety net. Decide whether conflict returns 409 or safely retries.


## 17. Immutability enforcement

Application code must expose no update/delete operation for published `AssetTypeVersions`. EF mappings should make `SchemaJson` read-only after insertion in domain workflows. For stronger guarantees, consider DB permissions/triggers, but record migration and admin implications. Fixing a bad published schema means publishing a corrected version, not editing the existing row in place.


## 18. Asset create validation

1. Authenticate and resolve global subject.
2. Verify active membership and asset-create permission for workspace.
3. Load selected definition and exact version by ID.
4. Verify definition active and global-or-current-tenant eligible.
5. Verify version belongs to definition.
6. Parse `customFields` as JSON object.
7. Validate keys, values and constraints against **that version**.
8. Verify optional property belongs to the same tenant.
9. Persist `Assets` with `TenantId`, definition ID, version ID and validated JSONB in one UoW.
10. Return the actual pinned version in DTO.

Never accept a client-provided `TenantId` as authority.


## 19. Asset update validation

An asset update normally uses its current pinned schema. The server loads the asset and pinned version before validating edited `CustomFields`. Use optimistic `Assets.Version` and reject stale writes. If the user chooses a different type/version, treat it as a **distinct migration operation** with explicit field mapping, data loss preview and product approval. The canonical API has not yet approved automatic type switching.


## 20. Unknown keys and malicious inputs

Reject unknown custom field keys by default. Reject duplicate JSON keys after parsing if the JSON library exposes them, or adopt a canonical parsing policy and test it. Cap object size, array length, field count and nesting depth. Avoid regex patterns vulnerable to catastrophic backtracking; prefer safe constrained patterns with timeout. Never execute scripts embedded in schema descriptions.


## 21. Decimal precision and transport

`decimal` is for numeric metadata, not automatic currency accounting. JSON numeric literals may lose precision in JavaScript; choose decimal-string transport or explicit precision/scale policy before implementation. PostgreSQL JSONB stores numeric values, but mobile JavaScript parsing can round. Do not round a user value silently; report a field validation error.


## 22. Date and timezone handling

Custom `date` values are calendar dates (`YYYY-MM-DD`) with no timezone shift. Use `DateOnly` or equivalent server representation. Do not convert a purchase date to UTC midnight and back in mobile. Timezone-aware maintenance scheduling is a separate domain concern; a custom date field must not create reminders by itself.


## 23. Select and multiSelect semantics

Options are stored as stable values in the published schema. Labels/translations may be a future extension; do not treat changing an option's display label as changing its stored value without a versioned schema. Reject duplicate `multiSelect` values, invalid options and arrays exceeding configured cap. Define whether option comparison is case-sensitive before launch.


## 24. Global template seed strategy

Seed global definitions through a controlled, idempotent initializer/migration with stable codes and safe identity strategy. Seed first published versions once. On later deployments, create a **new version** for schema changes. Do not overwrite a published `SchemaJson` row. Global catalog changes must not accidentally write a tenant ID or create duplicate definitions.


## 25. Recommended initial global templates

**Candidate templates for product review**, not an approved final catalog:

| Code | Display name | Example custom fields |
|---|---|---|
| `vehicle` | Vehicle | VIN, model year, fuel type |
| `appliance` | Appliance | Appliance category, capacity |
| `hvac` | HVAC system | System type, filter size |
| `electronics` | Electronics | Device category, purchase model |
| `home-equipment` | Home equipment | Equipment category, installation date |

Keep common attributes (`Manufacturer`, `Model`, `SerialNumber`, `PurchasedOn`, `PurchasePrice`) in standard `Assets` columns rather than duplicating them as custom fields. Final template definitions require a product ADR.


## 26. Custom type creation workflow

Mobile user opens asset type catalog → selects “Create custom type” → enters display name/code → configures fields → reviews validation → publishes version 1. Server creates tenant-owned definition and version under controlled transaction. If creation succeeds but publication fails, reconcile or roll back; do not leave unusable type visible without defined state. `Draft` is not a canonical DB state, so drafts should remain client-local until an ADR adds persistence.


## 27. Editing an existing custom type

An authorized member edits a **new draft copied from latest published schema**, then publishes version N+1. Existing assets stay pinned to their old versions. Show a warning when removing keys, changing types or restricting options because old assets may contain values not valid under the new version. A schema editor must never retroactively invalidate an old asset on read.


## 28. Example version evolution

**Version 1:** `vin: string`, `modelYear: integer`.  
**Version 2:** adds optional `fuelType: select`.  
**Version 3:** changes `fuelType` options or makes a field required.

An asset created with v1 remains readable and editable using v1. A newly created asset may use v3. Upgrading an old asset requires an explicit operation that validates its values against v3 and addresses newly required fields. Publishing v3 alone never migrates assets.


## 29. Archive/deactivate behavior

`AssetTypeDefinitions.IsActive=false` prevents **new asset creation and new version publication** by default. Existing assets retain pinned versions and must remain readable, searchable and editable according to an approved policy. If the backend rejects all reads of inactive definitions, it will break historical assets; therefore distinguish catalog selection eligibility from historical rendering eligibility. Define whether an inactive type may be reactivated.


## 30. Deletion and retention

Hard delete a definition/version only if it is unreferenced and an approved retention policy allows it; otherwise deactivate. PostgreSQL FKs should prevent accidental deletion of referenced versions. Maintenance, documents and service history remain attached to the asset, not the mutable type catalog. Do not cascade-delete assets because a custom type is archived.


## 31. Schema-driven mobile forms

1. Fetch eligible type catalog for active workspace.
2. Fetch the exact published schema version.
3. Render standard asset fields plus custom widgets in schema order.
4. Apply Zod/client-side validation for immediate feedback.
5. Submit the selected definition/version IDs and values.
6. Handle server validation errors by field path.
7. On conflict or schema change, refresh and show an explicit choice.

The client is not authoritative; it must not bypass server validation. Render old assets from their pinned schema even when the latest catalog version differs.


## 32. Mobile UI states

**Loading:** skeleton fields; no submission until schema loaded. **Empty:** explain no custom fields. **Invalid schema:** safe error, do not crash renderer. **Offline:** read cached pinned version only within the authorized workspace, but defer writes or show explicit queued state if offline mutation support is approved. **Version conflict:** preserve unsaved values and explain that type definition changed. **Archived type:** historical detail read-only or editable per policy.


## 33. React Native field registry example

```ts
type FieldType =
  | 'string' | 'integer' | 'decimal' | 'boolean'
  | 'date' | 'select' | 'multiSelect';

type CustomFieldDefinition = {
  key: string;
  label: string;
  type: FieldType;
  required: boolean;
  options?: string[];
  min?: number;
  max?: number;
  maxLength?: number;
};

const widgetByType: Record<FieldType, string> = {
  string: 'TextInput',
  integer: 'IntegerInput',
  decimal: 'DecimalInput',
  boolean: 'Switch',
  date: 'DatePicker',
  select: 'Select',
  multiSelect: 'MultiSelect',
};
```

Illustrative renderer metadata; real components should be typed and validated against generated API models. Never execute arbitrary components named by server JSON.


## 34. Application service boundaries

```csharp
public interface IAssetTypeAppService
{
    Task<PagedResultDto<AssetTypeDto>> GetEligibleAsync(
        Guid workspaceId, CancellationToken ct);
    Task<AssetTypeVersionDto> GetVersionAsync(
        Guid workspaceId, Guid definitionId, Guid versionId,
        CancellationToken ct);
    Task<AssetTypeDto> CreateCustomAsync(
        Guid workspaceId, CreateCustomAssetTypeDto input,
        CancellationToken ct);
    Task<AssetTypeVersionDto> PublishVersionAsync(
        Guid workspaceId, Guid definitionId,
        PublishAssetTypeVersionDto input, CancellationToken ct);
}
```

This is **illustrative C#**, not a claim of an existing ABP interface or guaranteed compile-ready signature. The actual service must use ABP authorization and generated OpenAPI contracts.


## 35. Proposed API routes

| Method | Proposed route | Notes |
|---|---|---|
| GET | `/api/v1/workspaces/{workspaceId}/asset-types` | Eligible global + own types |
| GET | `/api/v1/workspaces/{workspaceId}/asset-types/{definitionId}` | Definition metadata |
| GET | `/api/v1/workspaces/{workspaceId}/asset-types/{definitionId}/versions/{versionId}` | Immutable schema |
| POST | `/api/v1/workspaces/{workspaceId}/asset-types` | Create tenant type |
| POST | `/api/v1/workspaces/{workspaceId}/asset-types/{definitionId}/versions` | Publish new version |
| POST | `/api/v1/workspaces/{workspaceId}/asset-types/{definitionId}/archive` | Deactivate, policy dependent |

`api-contracts-master.md` previously used partly unscoped `/asset-types` routes. **This file recommends explicit workspace-scoped routes** for tenant-safe catalog reads; reconcile both documents and verify actual ABP-generated OpenAPI before implementation.


## 36. Create type request example

```json
{
  "code": "garden-tool",
  "displayName": "Garden tool",
  "schema": {
    "schemaVersion": 1,
    "fields": [
      {"key": "powerSource", "label": "Power source", "type": "select", "required": false,
       "options": ["Electric", "Gas", "Manual"]}
    ]
  }
}
```

A create-and-publish request is one proposed UX shortcut. If API separates definition creation and version publication, make the transaction/recovery semantics explicit. Do not claim the canonical database has a persistent draft state.


## 37. Published version response example

```json
{
  "definitionId": "ad9d05cf-91bb-4d9d-92d7-bdf22183f920",
  "versionId": "7e0f4e9b-41bc-4875-9512-20c60d1d9a3c",
  "versionNumber": 2,
  "publishedAt": "2026-10-29T16:00:00Z",
  "schema": {
    "schemaVersion": 1,
    "fields": []
  }
}
```

Do not include private tenant metadata from a foreign definition. Cache immutable version payloads using definition/version IDs plus workspace eligibility; a cached schema is not proof of current authorization.


## 38. Authorization and permission matrix

| Action | Member | Owner | Platform catalog admin |
|---|---|---|---|
| Read eligible global/own schema | If active + read permission | Yes | Admin scope |
| Create tenant type | If type-manage granted | Yes | Not tenant override by default |
| Publish tenant version | If type-manage granted | Yes | Not tenant override by default |
| Deactivate tenant type | If type-manage granted | Yes | Audited support only |
| Modify global type | No | No | Yes |
| Read foreign tenant type | No | No | Audited privileged process only |

Exact ABP permission names/grants are M0/M1 decisions. Never authorize based on role string alone.


## 39. Tenant-safe EF Core query

```csharp
var definition = await db.AssetTypeDefinitions
    .SingleOrDefaultAsync(x => x.Id == definitionId &&
        (x.TenantId == null || x.TenantId == workspaceId), ct);

if (definition is null) throw new EntityNotFoundException(...);
var version = await db.AssetTypeVersions.SingleOrDefaultAsync(
    x => x.Id == versionId && x.AssetTypeDefinitionId == definition.Id, ct);
```

Pseudocode. Membership check must occur **before** this lookup. The composite FK in `Assets` does not enforce the tenant eligibility predicate; validate at write time and consider safe database guard mechanisms if approved.


## 40. Server validator interface

```csharp
public interface ICustomAssetFieldValidator
{
    ValidationResult ValidateSchema(JsonElement schema);
    ValidationResult ValidateValues(
        JsonElement schema, JsonElement customFields);
}
```

Use the same validator for create, edit, publish compatibility checks and migrations. Domain validation should return deterministic field-path errors. Keep the dialect parser independent of ASP.NET controllers, EF Core and React Native.


## 41. Error model

| Error | Suggested HTTP | Stable category |
|---|---|---|
| Unauthorized workspace | 403/404 per concealment policy | `WorkspaceAccessDenied` |
| Foreign tenant definition | 404 | `AssetTypeNotFound` |
| Unsupported schema dialect | 422 | `UnsupportedSchemaVersion` |
| Duplicate normalized field key | 422 | `DuplicateCustomFieldKey` |
| Reserved field key | 422 | `ReservedCustomFieldKey` |
| Unknown asset custom key | 422 | `UnknownCustomField` |
| Invalid field value | 422 | `InvalidCustomFieldValue` |
| Duplicate type code | 409 | `AssetTypeCodeConflict` |
| Concurrent publish | 409 | `AssetTypePublishConflict` |
| Stale asset version | 409 | `AssetConcurrencyConflict` |

Codes are **proposed** and must align with ABP error mapping and the master API contract.


## 42. Validation and security limits

Before beta, set and test limits for: maximum fields per type, schema bytes, custom JSON bytes, string length, select options, multiSelect count, regex complexity, and publish frequency. Avoid allowing arbitrary JSON nesting. Rate-limit type creation/publication and record audit events without exposing user private values in logs. Do not store passwords, access tokens, or sensitive freeform secrets as custom asset fields.


## 43. Indexing and performance

Use relational indexes for common filters (`TenantId`, `PropertyId`, type/version IDs, `IsArchived`). JSONB GIN indexes may help **only** if supported queries justify them; do not create one on every field blindly. Analyze query plans with `EXPLAIN (ANALYZE, BUFFERS)`. A large catalog list should return metadata without all schemas; fetch a specific immutable version on demand.


## 44. Cache partitioning

Cache global immutable versions by version ID only when access policy permits; tenant-owned versions by `(TenantId, DefinitionId, VersionId)`. Always recheck current membership for protected requests, even if data is cached. On mobile workspace switch, cancel stale requests and invalidate/partition tenant-owned cache. A global schema cache must not accidentally include another tenant's type.


## 45. Migration and backward compatibility

Publishing a new schema version is **not** a PostgreSQL migration. Existing JSONB rows remain unchanged. If a future migration changes schema dialect, write a separately reviewed, idempotent migration utility with dry-run, compatibility tests, backups and per-asset error reporting. Preserve old published versions as long as referenced by assets.


## 46. Audit and observability

Audit type definition creation, version publication, deactivation and failed cross-tenant attempts. Record safe metadata: tenant, definition ID, version number, actor, outcome, correlation ID. Avoid logging entire `CustomFields` JSON, VINs, serial numbers, private addresses or presigned URLs. Monitor validation failure rates and publish conflicts without exposing values.


## 47. Testing matrix — catalog and schema

| ID | Scenario | Expected |
|---|---|---|
| CAT-001 | List global types as active member | Included |
| CAT-002 | List own tenant types | Included |
| CAT-003 | List foreign tenant types | Excluded |
| CAT-004 | Global type edited by tenant Owner | Denied |
| CAT-005 | Create duplicate code in same tenant | Conflict |
| CAT-006 | Same code in different tenants | Allowed by canonical indexes |
| CAT-007 | Same code global and tenant | Allowed by DB; API policy decided |
| CAT-008 | Publish invalid root array | Rejected |
| CAT-009 | Publish duplicate field key | Rejected |
| CAT-010 | Publish reserved key | Rejected |
| CAT-011 | Unsupported field type | Rejected |
| CAT-012 | Invalid select options | Rejected |
| CAT-013 | Concurrent publish same next version | One winner or safe retry |
| CAT-014 | Update published schema | Rejected |
| CAT-015 | Read archived type for old asset | Historical read preserved |


## 48. Testing matrix — asset values and UI

| ID | Scenario | Expected |
|---|---|---|
| CAT-016 | Create asset with global type | Allowed |
| CAT-017 | Create asset with own custom type | Allowed |
| CAT-018 | Create asset with foreign custom type | Denied |
| CAT-019 | Version belongs to different definition | Rejected |
| CAT-020 | `CustomFields` is array | Rejected |
| CAT-021 | Unknown field key | Rejected |
| CAT-022 | Missing required field | Rejected |
| CAT-023 | Integer field receives decimal | Rejected |
| CAT-024 | Boolean field receives string | Rejected |
| CAT-025 | Invalid date | Rejected |
| CAT-026 | Select option not published | Rejected |
| CAT-027 | Duplicate multiSelect value | Rejected |
| CAT-028 | Old asset pinned to v1 after v2 publish | Still valid/readable |
| CAT-029 | Archive type with existing assets | Existing assets remain |
| CAT-030 | Asset has null `PropertyId` | Allowed |
| CAT-031 | Cross-tenant property assigned | Rejected |
| CAT-032 | Stale asset update | Conflict |
| CAT-033 | Mobile switches tenant during schema fetch | No stale form leak |
| CAT-034 | Malicious huge schema | Bounded rejection |
| CAT-035 | Cache hit after membership revocation | Denied |
| CAT-036 | Global seed rerun | No duplicate version |
| CAT-037 | Different definition same version ID input | Rejected |
| CAT-038 | Attempt hard-delete referenced version | FK prevents |
| CAT-039 | Decimal precision edge | No silent rounding |
| CAT-040 | Schema editor crashes on unsupported type | Safe error UI |


## 49. Implementation plan M0–M3

**M0:** Decide dialect and ABP global identity, define permission boundary, create validation fixtures and contract errors.  
**M1:** Prove global membership/tenant isolation; test tenant-aware type reads.  
**M2:** Implement canonical definitions/versions, global seeds, tenant custom type create/publish, immutable schemas, mobile editor.  
**M3:** Integrate asset create/edit with pinned schema validation, optional property, optimistic concurrency and mobile schema-driven form.  
**M8:** Security, performance, schema evolution and recovery regression suite.


## 50. Open decisions

1. Constrained Kepnora dialect versus JSON Schema subset.
2. Field count, schema size and value size limits.
3. Key normalization, Unicode and case sensitivity.
4. Decimal serialization/precision policy.
5. Select option comparison and localization.
6. Whether inactive definitions permit edits of existing assets.
7. Whether assets can change type or upgrade schema version.
8. Whether type drafts are persisted or client-local.
9. Whether global and tenant codes may overlap in UI.
10. Global template seed catalog and stable IDs.
11. Exact API route strategy; reconcile with master API contract.
12. Permission names and role grants.
13. Regex/pattern support and timeout policy.
14. Audit retention and custom field privacy classification.
15. DB-level guard beyond application type eligibility.
16. Version publication conflict/retry policy.


## 51. Definition of Done

- [ ] Uses canonical `AssetTypeDefinitions` and `AssetTypeVersions`; no parallel catalog.
- [ ] Global templates are read-only to tenant members.
- [ ] Foreign tenant custom types are inaccessible.
- [ ] Published schemas are immutable and version numbers unique.
- [ ] Asset definition/version association and eligibility are checked.
- [ ] Server validates every custom field against pinned version.
- [ ] Reserved, unknown, malformed and oversized fields are rejected.
- [ ] Old assets remain valid after a new version is published.
- [ ] `Asset.PropertyId` remains optional.
- [ ] Archived types preserve historical asset reads.
- [ ] Schema-driven mobile UI handles errors, loading and workspace switch.
- [ ] PostgreSQL and API integration tests pass CAT-001 through CAT-040.
- [ ] Open decisions are closed in ADRs before implementation.


## 52. Final principle

**The schema is part of the asset’s historical contract.** Kepnora must preserve the exact published version that validated each asset, while allowing the catalog to evolve through new immutable versions. Flexibility must not weaken tenant isolation, validation or historical integrity.

