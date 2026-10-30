# Kepnora — Documents & Warranties Feature Specification

**Target:** `docs/features/documents-warranties.md`  
**Version:** 1.0-draft · **Phase:** 1 MVP · **Language:** English  
**Status:** Implementation specification, with explicit decisions pending.

**References:** `docs/database-overview.md` §17, `docs/architecture/file-storage.md`, `docs/architecture/tenant-isolation.md`, `docs/features/asset-management.md`, `docs/api/api-contracts-master.md`, `docs/implementation/mobile-project-structure.md`.

> **Core invariant:** Documents belong to assets and tenants; files live in private S3, metadata in PostgreSQL. A warranty is a document **category** in the current schema, not a separate warranty record with an expiry date.

---

## 1. Purpose and user outcomes

Kepnora lets workspace members keep asset photos, purchase receipts, service invoices, manuals and warranty files in one private location. Authorized users can attach, browse, open and archive documents without losing asset or maintenance history. Uploads must survive ambiguous mobile network failures safely.

## 2. MVP scope and exclusions

| Included in Phase 1 | Deferred / requires explicit approval |
|---|---|
| Asset-level document attachment | Direct property documents |
| Photo, Receipt, Manual, Warranty, Other categories | Standalone warranty entities |
| Private S3 direct upload | Public sharing links |
| Metadata list, detail and download | OCR / AI extraction |
| Document archive | Permanent delete in normal UX |
| Intent expiration and orphan cleanup | Automatic file sync |
| Retry, idempotent finalize, auditing | Service provider file access |
| Mobile preview via authorized URL | Automatic warranty expiration reminders |

**Important:** The current `Documents` schema has no `PurchaseDate`, `WarrantyStartDate`, `WarrantyEndDate`, `WarrantyProvider`, `Notes`, `ServiceRecordId` or `PropertyId`. Do not silently add these to DTOs or pretend the app can store them.

## 3. Canonical persistence model

`UploadIntents`: `Id`, `TenantId`, `AssetId`, `RequestedByUserId`, `ObjectKey`, `ExpectedContentType`, `ExpectedSizeBytes`, `Status`, `ExpiresAt`, `CreatedAt`.

`Documents`: `Id`, `TenantId`, `AssetId`, `UploadIntentId`, `Category`, `FileName`, `ContentType`, `SizeBytes`, `ObjectKey`, `CreatedAt`, `IsArchived`.

**Database invariants:** composite foreign keys `(TenantId, AssetId)` to `Assets`; `(TenantId, UploadIntentId)` to `UploadIntents`; unique `Documents.UploadIntentId`, unique object key; valid upload status values `Pending`, `Finalized`, `Expired`, `Rejected`; expected size positive. Keep PostgreSQL quoted PascalCase identifiers in raw SQL.

**No schema migration should be generated from this document alone.** Use canonical database overview and approved ADRs.

## 4. Document categories

| Value | Intended content | Notes |
|---|---|---|
| `Photo` | Asset appearance, model/serial label | May need image-specific preview |
| `Receipt` | Purchase or service invoice | No OCR in MVP |
| `Manual` | Product guide / user manual | PDF commonly used |
| `Warranty` | Warranty certificate / terms | File category only |
| `Other` | Supporting asset file | Still subject to allowed MIME policy |

The category enum casing and values must be pinned in backend Application.Contracts and OpenAPI; database currently stores `VARCHAR(30)` without a category CHECK constraint.

## 5. Document and warranty user stories

- As an authorized member, I can upload a receipt to a standalone car.
- As an authorized member, I can attach a manual to an appliance linked to a property.
- As a reader, I can see document category, filename, file size and creation date.
- As an authorized reader, I can open a file through a short-lived download URL.
- As an authorized editor, I can archive an obsolete document.
- As a user in Workspace A, I cannot see Workspace B files or metadata.
- As a user, I can attach a warranty PDF, but I am not misled into thinking its expiry is tracked unless a separate approved feature is implemented.
- As a user with intermittent connectivity, I can safely retry upload finalization.

## 6. Ownership and authorization

Every file is owned by one asset and one ABP tenant. The actor must have active global identity, active `TenantMembers` membership and explicit permission. `ICurrentTenant`/`IMultiTenant` filter data but do **not** authorize access. A document may never be attached to an asset from another workspace. Standalone assets (`PropertyId = NULL`) are valid targets.

Suggested permissions: `Kepnora.Documents.Read`, `Kepnora.Documents.Manage`; validate actual ABP permission names during M0. Device login alone does not imply workspace access.

## 7. Secure S3 architecture

```mermaid
sequenceDiagram
    participant M as Expo Mobile
    participant A as ABP API
    participant D as PostgreSQL
    participant S as Private S3
    M->>A: Request upload intent
    A->>D: Check membership, asset, policy; create Pending
    A-->>M: Short-lived presigned PUT + intentId
    M->>S: PUT bytes with signed headers
    S-->>M: Upload response
    M->>A: Finalize intent
    A->>S: HEAD object / verify metadata
    A->>D: Transaction: Document + Finalized intent
    A-->>M: Document metadata
```

The API never assumes client-reported upload success. S3 object keys are generated server-side and never accepted as an authorization credential. Bucket Public Access Block and private IAM policies are mandatory.

## 8. Upload intent lifecycle

```text
Pending --valid PUT and verified finalize--> Finalized
Pending --TTL elapsed--------------------> Expired
Pending --policy/object mismatch----------> Rejected
```

Status values belong to `UploadIntents`, not `Documents`. Finalized intent must create at most one document. Cleanup must never delete finalized objects. A crash between S3 PUT and DB finalize is reconciled by retry or cleanup worker.

## 9. Upload flow — client steps

1. Select asset and category.
2. Pick file from OS document picker or camera/photo library, with permissions.
3. Display local filename, size and file type.
4. Request an intent from backend; backend validates asset and category.
5. PUT bytes to presigned URL using exactly the signed content type and headers.
6. Finalize using `intentId`; do not create a local fake document as authoritative.
7. Show server-returned metadata and invalidate asset document list.
8. On ambiguous finalize failure, retry finalize or refresh the list rather than creating a second intent.

Do not persist presigned URLs, include them in telemetry or log them in debugging output.

## 10. Proposed API contracts

All routes are **proposals**; the running backend's OpenAPI output is authoritative.

| Method | Proposed route | Behavior |
|---|---|---|
| POST | `/api/v1/workspaces/{workspaceId}/upload-intents` | Authorize and presign PUT |
| POST | `/api/v1/workspaces/{workspaceId}/upload-intents/{intentId}/finalize` | Verify and create document |
| GET | `/api/v1/workspaces/{workspaceId}/assets/{assetId}/documents` | Tenant-scoped metadata list |
| GET | `/api/v1/workspaces/{workspaceId}/documents/{documentId}` | Document metadata |
| POST | `/api/v1/workspaces/{workspaceId}/documents/{documentId}/download-url` | Fresh authorization and short-lived GET |
| POST | `/api/v1/workspaces/{workspaceId}/documents/{documentId}/archive` | Soft archive |

Do not add an unreviewed file-bytes API proxy or public S3 URL.

## 11. Intent request and response example

```json
{
  "assetId": "ea8b0b3c-679b-4e56-bce6-44f6db12a6f1",
  "category": "Warranty",
  "fileName": "warranty.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 238144
}
```

Illustrative response:

```json
{
  "intentId": "91a0ec16-72e5-4d1f-962f-7b641c56ef5c",
  "uploadUrl": "<short-lived-presigned-url>",
  "method": "PUT",
  "requiredHeaders": { "Content-Type": "application/pdf" },
  "expiresAt": "2026-10-29T19:15:00Z"
}
```

Response shape, header signing and TTL must be fixed in API contracts; placeholder URL is not a real credential.

## 12. Finalization semantics

Server verifies intent is pending and belongs to current tenant, user is still authorized, S3 object exists, object key is expected, content type/length match policy and intent has not expired. In a PostgreSQL transaction, insert one `Documents` row and set intent to `Finalized`.

Unique `Documents.UploadIntentId` prevents duplicates under concurrent finalize calls. A repeat finalize should return the same document if authorized, not a second record. If object is missing or metadata mismatches, return a stable error without claiming upload success.

## 13. Object key and URL security

Proposed key: `tenants/{tenantId}/assets/{assetId}/documents/{documentId}/{opaqueName}`. Use unpredictable server-generated names; never concatenate user-provided paths. `ObjectKey` stays internal to backend. Presigned GET/PUT URLs are bearer capabilities valid until expiry; permission revocation cannot instantly invalidate an already issued URL, so keep TTL short and document residual risk.

## 14. File validation

Validate allowed MIME types, extensions where appropriate, size, quotas, user permissions and content policy server-side. Do not trust the mobile filename or `Content-Type` header as proof of file content. MIME allowlist and per-category byte limits remain policy decisions; record them before beta. Reject zero-byte and oversized files. Malware scanning policy must be decided before production exposure.

## 15. Document metadata response

```json
{
  "id": "b481d5a0-2b0b-4d21-bf64-2fda457b1d23",
  "assetId": "ea8b0b3c-679b-4e56-bce6-44f6db12a6f1",
  "category": "Warranty",
  "fileName": "warranty.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 238144,
  "createdAt": "2026-10-29T23:30:00Z",
  "isArchived": false
}
```

`ObjectKey` and presigned URLs should not appear in ordinary metadata list responses. Date and size display are localized in the client; raw UTC timestamps remain API contract.

## 16. List, filter and sorting

Default list shows non-archived documents for a single asset. Filters: category and optionally includeArchived (permission-gated). Sort by `CreatedAt DESC, Id DESC`; paginate with stable cursor or consistent API-wide pagination. Do not load entire file metadata collection for every asset. An asset detail may show a small preview and link to all documents.

## 17. Download and preview

1. User opens document.
2. Mobile requests a fresh authorized download URL.
3. API rechecks active membership, document and asset tenant, permissions and archive policy.
4. API issues short-lived presigned GET.
5. Mobile opens in a secure supported viewer and handles expiry/retry.

Avoid caching private files in globally accessible storage; OS share/export behavior needs explicit UX/privacy policy. Do not persist presigned URLs in AsyncStorage or Zustand.

## 18. Archive behavior

`Documents.IsArchived = TRUE` hides it from normal lists. Do not delete S3 bytes immediately on archive; retention/deletion policies are not finalized. Archive should not delete an asset, maintenance occurrence or historical service record. Archived document download policy must be explicit (recommended read restricted to authorized roles or restore flow). Hard deletion is deferred.

## 19. Warranty representation — current MVP

Warranty is a `Documents.Category` value. Users may upload and view warranty files, but the current schema **cannot** query warranty expiry dates or automatically create warranty-expiration reminders. Do not infer expiration by reading PDF content or file name. Do not add a warranty expiry input field that is silently discarded.

The UI may label a category `Warranty Documents` and show the uploaded file, upload date and associated asset. Any expiry tracking is a separate, explicitly approved schema/API/product decision.

## 20. Future warranty metadata proposal (not implemented)

If approved later, an ADR may introduce a separate `AssetWarranties` entity or typed `WarrantyMetadata` with fields such as `AssetId`, `DocumentId?`, `StartDate`, `EndDate`, `Provider`, `CoverageNotes`, `CreatedBy`, `Version`. Decide whether multiple warranties per asset, date-only timezone semantics and reminder lifecycle are supported. Never encode warranty end dates in the document filename as an ersatz database.

## 21. Warranty reminders — deferred integration

Expiry reminders would require approved warranty metadata, eligibility rules, notification preferences, deduplication keys and outbox events. The Phase 1 notification engine is built for maintenance occurrences, not arbitrary inferred warranty dates. Avoid reusing `MaintenanceOccurrence` for warranties without a domain design decision.

## 22. Mobile folder structure

```text
src/features/documents/
  api/
    documentQueries.ts
    documentMutations.ts
    uploadIntentApi.ts
  components/
    DocumentCard.tsx
    CategoryPicker.tsx
    UploadProgress.tsx
    DocumentPreview.tsx
  hooks/
    useAssetDocuments.ts
    useDocumentUpload.ts
  screens/
    AssetDocumentsScreen.tsx
    UploadDocumentScreen.tsx
  validation/
    documentSchema.ts
  utils/
    fileMetadata.ts
```

Expo Router route files remain thin wrappers. Use generated TypeScript OpenAPI client for backend DTOs and TanStack Query for server state.

## 23. Mobile upload state machine

```text
Idle -> Selecting -> Validating -> CreatingIntent -> Uploading
     -> Finalizing -> Succeeded
     -> Failed (recoverable or terminal)
```

A network error during upload/finalize is ambiguous. Refresh intent/document state before retry. Distinguish cancellation before PUT, during PUT and after finalize. Upload progress reflects bytes sent, not guaranteed finalization. App background suspension is not a guarantee of continued upload.

## 24. Tenant-safe query keys

```ts
const documentKeys = {
  list: (workspaceId: string, assetId: string) =>
    ['workspace', workspaceId, 'assets', assetId, 'documents'] as const,
  detail: (workspaceId: string, documentId: string) =>
    ['workspace', workspaceId, 'documents', documentId] as const,
};
```

Cancel active requests and clear/partition cached data when workspace changes. Do not reuse file preview or pending upload from another tenant. Token and workspace changes must invalidate old authorization assumptions.

## 25. Expo device-specific handling

Use a supported document/image picker compatible with the pinned Expo SDK. Treat `content://` URIs on Android and iOS security-scoped file access carefully; files may need a temporary app-private copy before upload. Preserve extension only for display; server controls object key. Test iOS HEIC, PDF, Android content URIs, app suspension and OS permission denial. Do not promise background upload persistence without implementing it.

## 26. Loading, empty, error and offline UX

| State | UX |
|---|---|
| No files | `No documents yet` and authorized Add action |
| Loading | Skeleton/progress indicator |
| Uploading | Progress with cancel where safe |
| Finalizing | Separate verifying indicator |
| URL expired | Re-request authorized URL |
| Unauthorized | Remove inaccessible file from view |
| Offline | Cached metadata only if allowed; no unqueued write |
| Too large/type denied | Actionable validation error |
| S3 transient error | Safe retry without duplicate document |
| Workspace switch | Cancel/clear pending tenant-specific state |

## 27. Privacy and logging

Never log file bytes, presigned URLs, bearer tokens, raw document contents or unnecessary filenames. Audit document create/finalize/download authorization/archive with tenant, asset, actor, document and correlation IDs. Consider filename as potentially sensitive user data. Encrypt S3 objects at rest and require TLS in transit.

## 28. Quotas, retention and cleanup

Per-file max, per-tenant quota, intent TTL, orphan retention, archived-file retention and malware scanning are open policy decisions. Cleanup worker must use safe leases, recheck intent status before delete, tolerate S3 eventual/operational failures and never delete an object referenced by a finalized document. Measure orphan count and cleanup age. Do not claim S3 and PostgreSQL form one atomic transaction.

## 29. Backend organization

```text
Domain/Documents/
  Document.cs
  UploadIntent.cs
Application.Contracts/Documents/
  DocumentDto.cs
  CreateUploadIntentDto.cs
  FinalizeUploadIntentDto.cs
Application/Documents/
  DocumentAppService.cs
  UploadIntentAppService.cs
EntityFrameworkCore/EntityConfigurations/
  DocumentConfiguration.cs
  UploadIntentConfiguration.cs
Infrastructure/Storage/
  S3DocumentStorage.cs
Worker/Jobs/
  ExpiredUploadIntentCleanupJob.cs
```

Names and folders are illustrative; adapt to pinned ABP module boundaries. Storage adapter handles S3, application layer handles authorization and domain policy.

## 30. Error contract

| Condition | Expected behavior |
|---|---|
| Unauthenticated | 401 |
| Membership revoked | 403 or neutral 404 |
| Cross-tenant asset/document | Neutral 404 |
| Unsupported category/MIME | 422 |
| Oversized file | 413 or mapped validation per API contract |
| Expired intent | 409/410 per documented contract |
| Missing S3 object on finalize | Stable 409/422, no document |
| Duplicate finalize | Return existing document idempotently |
| S3 unavailable | Retryable 503, safe logging |
| Concurrency conflict | Stable 409 |

Match API master error envelope; do not leak bucket/object keys or internal stack traces.

## 31. Concurrency and idempotency

Concurrent finalize requests for one intent must produce one document. Use unique `UploadIntentId` and transactional status transitions. A failed client response after DB commit should be recoverable via repeated finalize returning the existing record. Cleanup races must recheck finalized status and document references before deleting S3 objects.

## 32. Security threat scenarios

- Caller supplies a foreign asset ID in upload intent: deny.
- Caller guesses document ID from another tenant: deny metadata and presign.
- Former member retains JWT: active membership check denies new URLs.
- User changes workspace during upload: pending request must not finalize in wrong workspace.
- User changes `ObjectKey` in request: reject; keys server-generated.
- Public bucket policy accidentally enabled: deployment check fails.
- Presigned URL in logs: privacy test fails.
- Upload size/content mismatch: finalize rejects.
- Duplicate finalize: only one document.
- Cleanup worker with stale state: must not delete finalized object.
- Archived asset still has document history: preserve according to asset archive policy.

## 33. Unit tests

| ID | Scenario | Expected |
|---|---|---|
| DOC-U01 | Valid category | Accepted |
| DOC-U02 | Unknown category | Rejected |
| DOC-U03 | Zero byte size | Rejected |
| DOC-U04 | Negative size | Rejected |
| DOC-U05 | Oversize | Rejected |
| DOC-U06 | Empty filename | Rejected |
| DOC-U07 | Unsafe filename/path | Sanitized/rejected |
| DOC-U08 | Missing MIME | Rejected |
| DOC-U09 | Invalid intent transition | Rejected |
| DOC-U10 | Finalized repeat | Idempotent |
| DOC-U11 | Archived list default | Hidden |
| DOC-U12 | Warranty category | File only, no expiry field |
| DOC-U13 | No property asset | Allowed |
| DOC-U14 | Expired URL | Refresh required |
| DOC-U15 | Retry ambiguous finalize | No duplicate |

## 34. Integration tests

| ID | Scenario | Expected |
|---|---|---|
| DOC-I01 | Intent under tenant A | Tenant A only |
| DOC-I02 | Intent with B asset | Denied |
| DOC-I03 | PUT and finalize | One document |
| DOC-I04 | Finalize missing object | No document |
| DOC-I05 | Content length mismatch | Rejected |
| DOC-I06 | MIME mismatch | Rejected |
| DOC-I07 | Two finalize calls | One document |
| DOC-I08 | Commit then response loss | Repeat returns same |
| DOC-I09 | Revoked member GET | Denied |
| DOC-I10 | Tenant B document GET | Denied |
| DOC-I11 | Archived document listing | Excluded |
| DOC-I12 | Standalone asset upload | Success |
| DOC-I13 | S3 outage | Recoverable error |
| DOC-I14 | Expired intent cleanup | Orphan removed safely |
| DOC-I15 | Cleanup vs finalize race | Finalized object survives |
| DOC-I16 | Workspace switch during PUT | No data leakage |
| DOC-I17 | Android content URI | Successful upload |
| DOC-I18 | iOS PDF/HEIC | Supported or clear rejection |
| DOC-I19 | Presigned URL redaction | No secrets logged |
| DOC-I20 | Public bucket access test | Denied |
| DOC-I21 | Pagination/category filter | Tenant scoped |
| DOC-I22 | Asset archive | Document retained |
| DOC-I23 | Versioned API generated client | Contract matches |
| DOC-I24 | Retry after app suspension | Safe recovery |
| DOC-I25 | Warranty PDF | Metadata only; no fabricated expiration |

## 35. Performance and cost

Upload bytes directly from mobile to S3 rather than through ABP API. Page metadata lists and avoid eager image downloads. Use thumbnail generation only after a documented pipeline and schema/storage strategy. Measure upload completion time, finalize latency, S3 request errors, orphan count, document storage usage and download presign latency.

## 36. Accessibility and internationalization

Give picker, upload progress, cancel and error actions accessible labels. Show filename and category as text, not only icons. Announce upload completion or failure to screen readers. Localize size/date display without altering stored UTC timestamps or canonical category enum strings.

## 37. Milestone integration

| Milestone | Work |
|---|---|
| M0 | Validate ABP tenant/global identity assumptions |
| M1 | Permission/membership boundary |
| M3 | Asset-level attachment entry points |
| M5 | S3, intents, finalize, list, GET, archive, cleanup |
| M6 | No warranty-expiry push without separate approved domain |
| M7 | Document counts/search only if approved in contracts |
| M8 | Security, load, recovery, mobile device and privacy tests |

## 38. Open decisions

1. Exact MIME allowlist and per-category size limits.
2. Presigned PUT/GET TTL and required signed headers.
3. S3 encryption choice (SSE-S3 vs KMS).
4. Malware scanning policy before beta.
5. Per-tenant quota and enforcement.
6. Archived document download and restore behavior.
7. Retention/deletion policy and legal hold considerations.
8. Original photo preservation, HEIC support, thumbnail strategy.
9. Whether `Document.Category` gets a SQL CHECK constraint.
10. Whether service-record-specific attachments are needed (current schema asset-only).
11. Whether warranty expiry metadata is in Phase 1; current schema does **not** support it.
12. If expiry tracking approved, new entity vs JSONB vs explicit columns and reminder policy.
13. Whether document search indexes metadata only or content (MVP recommended metadata only).
14. Whether OS file export/share is allowed.
15. Which API-wide concurrency/idempotency envelope is authoritative.

## 39. Definition of Done

- [ ] Documents and upload intents match canonical schema.
- [ ] Private S3 with blocked public access and least privilege.
- [ ] Every operation checks active tenant membership and permission.
- [ ] Cross-tenant composite FK tests pass.
- [ ] Upload intent, PUT and verified finalize work on iOS and Android.
- [ ] Repeat finalize is idempotent; duplicate docs impossible.
- [ ] Download URL is short-lived and freshly authorized.
- [ ] List, preview, archive, empty/error/offline states implemented.
- [ ] Cleanup never deletes finalized objects.
- [ ] Standalone asset attachment works.
- [ ] Warranty is correctly represented as a document category only.
- [ ] No invented expiry field or reminder without approved schema.
- [ ] OpenAPI-generated mobile DTOs match running backend.
- [ ] Unit, integration, tenant and recovery tests pass.
- [ ] Secrets and presigned URLs are absent from logs.
- [ ] Pending policy decisions are recorded and closed before beta.

## 40. Final implementation principle

Keep **document storage**, **asset ownership**, and **warranty business metadata** separate concepts. The first two are supported by the current Phase 1 schema. Warranty dates and expiration notifications are not. Preserve this distinction to avoid data loss, misleading UX and accidental scope expansion.

