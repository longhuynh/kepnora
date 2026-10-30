# Kepnora — Dashboard & Global Search Feature Specification

**Intended path:** `docs/features/dashboard-search.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Scope:** Phase 1 / Milestone M7 · **Status:** Proposed implementation contract; API routes and policy defaults require validation.

**References:** `docs/features/dashboard.md`, `docs/features/search.md` (earlier Phase 1 feature specifications), `docs/database-overview.md`, `docs/architecture/api-design.md`, `docs/architecture/tenant-isolation.md`, `docs/features/asset-management.md`, `docs/features/maintenance-management.md`, `docs/ui/screen-specifications.md`.

---

**Product promise:** The Home dashboard answers **what do I own, what needs attention, and what should I do next?** Global search helps the user find assets, properties and maintenance information inside **one authorized workspace**. Both are read-oriented features; they do not become independent sources of business truth.

## 1. Goals and non-goals

**Goals:** fast tenant-scoped overview; useful empty states; correct maintenance urgency; simple navigation to assets, properties and maintenance; predictable global search; bounded queries; premium mobile presentation; accessibility; robust offline/refresh behavior.

**Non-goals:** provider discovery (Phase 2), booking/payment (Phase 3), AI semantic search, OCR/document content search, auto-estimated mileage, a second maintenance calculation engine, or a new persistent dashboard aggregate table in MVP.


## 2. Cross-feature invariants

1. Workspace = authoritative ABP tenant; no parallel custom tenant registry.
2. Validate authenticated global identity, active membership and permission on every request.
3. Filter all asset/property/maintenance joins by `(TenantId, Id)`; `ICurrentTenant` alone is insufficient.
4. Standalone assets with `PropertyId=NULL` count as active assets and appear in search.
5. Persisted occurrence lifecycle: `Open`, `Completed`, `Skipped`, `Cancelled` only.
6. `Upcoming`, `Due`, `Overdue` are derived **display** states for `Open` occurrences.
7. Missing valid odometer reading is **Unknown**; do not infer mileage.
8. Exclude archived assets/properties from active summary counts by default; history remains under authorized flows.
9. Dashboard snapshots are not authoritative for completion; target action must revalidate state.
10. Search result links are hints, not authorization grants; destination must reauthorize.
11. All tenant-scoped query keys include workspace ID; switching workspaces cannot flash old data.
12. Never display fabricated counts, prices, dates, images, provider listings or reminder success.


## 3. Information architecture

Suggested bottom tabs for the MVP: **Home, Assets, Maintenance, Search, Settings** (align final tab labels with `docs/ui/screen-specifications.md`). Properties and Documents are contextual destinations.

```text
Home
 ├─ Workspace switcher
 ├─ Summary / attention
 ├─ Due maintenance → occurrence detail
 ├─ Asset preview → asset detail
 ├─ Properties summary → property list
 └─ Recent activity → owning feature
Search
 ├─ Query input / filters
 ├─ Assets → asset detail
 ├─ Properties → property detail
 └─ Maintenance → rule / occurrence detail
```

Do not add a Find Services or Book Service tab to Phase 1.


## 4. Dashboard composition

Recommended vertical layout:

```text
Safe area + workspace switcher + notifications shortcut
Welcome / Home heading
Attention summary (due / overdue)
Next actions: Add Asset, View Maintenance
Maintenance attention cards (bounded)
Assets preview (bounded)
Properties overview (optional)
Recent service activity (bounded)
Safe area + bottom tabs
```

Use premium restrained visual language from `docs/ui/design-system.md`: generous spacing, readable headings, gentle cards, image-first asset previews only when a real authorized image exists. Never hide urgent maintenance behind decorative photography.


## 5. Dashboard content and counts

| Metric | Source | Inclusion |
|---|---|---|
| Active assets | `Assets` | Current tenant, `IsArchived=FALSE`, with or without property |
| Active properties | `Properties` | Current tenant, active only |
| Open occurrences | `MaintenanceOccurrences` | Open, eligible active asset/rule per policy |
| Due | Derived read model | Open occurrences meeting approved due threshold |
| Overdue | Derived read model | Open time occurrences past due per grace policy |
| Upcoming | Derived read model | Open occurrences in approved upcoming window |
| Unknown | Derived read model | Mileage occurrence without valid reading/anchor |
| Recent service activity | `MaintenanceRecords` | Current tenant, bounded and sorted |

**Count reconciliation:** Define whether `OpenOccurrences = Due + Overdue + Upcoming + Unknown + FutureOutsideWindow`. The four displayed buckets may not exhaust all Open occurrences if upcoming uses a bounded preview window. Do not assert equality unless classification is exhaustive.


## 6. Computed maintenance status

For an `Open` time occurrence, compare `DueOn` to the approved local calendar date. `DueOn < today` is generally Overdue, `DueOn = today` Due, and future dates Upcoming or Scheduled according to approved lead-window policy.

For an `Open` mileage occurrence, compare `DueMileage` to the latest **valid observed** mileage reading. Below threshold is not yet due; at/above threshold is Due (possibly past threshold by miles). Without a valid reading, status is Unknown. **Do not invent a calendar due date for mileage.**

Completed, Skipped and Cancelled are lifecycle labels, not overdue items. Due/grace thresholds and timezone precedence remain product/ADR decisions.


## 7. Attention priority and stable ordering

Proposed attention ordering:

1. Overdue time occurrences.
2. Due time and mileage occurrences.
3. Upcoming within preview window.
4. Unknown mileage occurrences with a clear “Add reading” action.

Within groups, sort by relevant due date or mileage gap and stable occurrence ID. Mileage and time cannot always be placed on one meaningful numeric urgency scale; avoid fake precision. For equal priorities, use stable tie-breakers to prevent cards from jumping between refreshes.


## 8. Dashboard response — proposed

```json
{
  "asOfUtc": "2026-10-29T19:00:00Z",
  "workspaceId": "8a8f52c1-9d40-4b2a-bad5-86ed8a76aa10",
  "counts": {
    "activeAssets": 12,
    "activeProperties": 2,
    "openOccurrences": 9,
    "due": 3,
    "overdue": 2,
    "upcoming": 3,
    "unknown": 1
  },
  "attention": [
    {
      "occurrenceId": "3dbb26a4-1f8d-41a2-a5c2-f95d3560e1c7",
      "assetId": "fb5d55ca-29a5-45d8-97b1-32e1f30ab1e7",
      "title": "Change engine oil",
      "displayStatus": "Due",
      "dueOn": "2026-10-29",
      "dueMileage": null
    }
  ],
  "recentActivity": [],
  "nextAttentionCursor": null
}
```

All values are illustrative. `asOfUtc` lets the UI explain snapshot freshness. Do not return unnecessary private document names or S3 URLs. Align final naming with generated OpenAPI.


## 9. Dashboard backend contract

```csharp
public sealed record DashboardQuery(
    Guid WorkspaceId,
    Guid UserId,
    DateOnly TodayLocal);

public interface IDashboardReader
{
    Task<DashboardDto> GetAsync(
        DashboardQuery query,
        CancellationToken cancellationToken);
}
```

`IDashboardReader` is a proposed Kepnora application abstraction, not a built-in ABP API. Membership verification belongs at the application boundary and resource joins remain tenant-scoped. Use a read-only service; do not recalculate or persist new maintenance occurrences as a side effect of opening Home.


## 10. Dashboard SQL/read-model strategy

Prefer bounded aggregate queries and a separate attention query over loading every asset/occurrence into application memory. Query only the fields needed for cards. Avoid N+1 loads of asset names and type labels.

```sql
SELECT COUNT(*) AS "ActiveAssetCount"
FROM "Assets" AS a
WHERE a."TenantId" = @TenantId
  AND a."IsArchived" = FALSE;
```

For occurrence counts, join `MaintenanceOccurrences` → `MaintenanceRules` → `Assets` with same-tenant predicates. Derive status in the read model from actual rule/due/reading data. Review real indexes and `EXPLAIN (ANALYZE, BUFFERS)` before assuming a particular query plan.


## 11. Dashboard consistency and snapshot semantics

Counts and cards may be read in separate queries while maintenance completion occurs concurrently. Options include a consistent read-only transaction snapshot or returning a clearly labeled best-effort `asOfUtc`. Do not claim perfectly atomic dashboard consistency without implementing it.

On tapping Complete, open the current occurrence and reauthorize/refetch; a dashboard card may already be stale. After mutation, invalidate the workspace dashboard and maintenance queries.


## 12. Dashboard loading, empty and offline states

| State | Experience |
|---|---|
| First load | Skeleton sections; no fabricated counts |
| Empty workspace | “Add your first asset” CTA; property creation optional |
| Assets but no rules | Explain maintenance tracking and link to add rule |
| No due items | Calm “Nothing needs attention right now” with other sections |
| Missing mileage | “Mileage not recorded” and link to add actual reading |
| Partial failure | Section-level retry if independent reads |
| Offline | Last authorized cached snapshot + last-sync label; no fake completion |
| Membership revoked | Clear content, show safe access error |
| Workspace switch | Clear prior workspace data before rendering next workspace |


## 13. Recent activity

Use actual `MaintenanceRecords` sorted by performed date and deterministic ID tie-breaker, with a bounded count. An ad hoc service record (`OccurrenceId=NULL`) is still legitimate activity. Display asset and work summary only if authorized. Do not invent events from upload attempts, unfinished maintenance or pending reminders.


## 14. Search scope

Phase 1 searches tenant-owned:

- **Assets:** name, optional manufacturer/model, approved type label.
- **Properties:** name and approved nickname/address summary fields.
- **Maintenance rules:** title and limited approved description fields.
- **Maintenance occurrences:** not independently indexed in v1; navigate via rule/asset if needed.
- **Documents:** metadata-only search is deferred unless explicitly approved.
- **Custom JSONB fields:** not globally searchable by default; explicit schema allowlist and indexes required.

Exclude file bytes, OCR, provider listings, other tenants and archived entities by default.


## 15. Search interaction flow

1. Open Search tab with prominent input and optional recent local queries (only if privacy policy approves).
2. Enter query; trim and normalize input.
3. Debounce input (250 ms proposed, tune after device testing).
4. Cancel obsolete requests using AbortSignal.
5. Render only results for the **current** workspace and query.
6. Apply resource filters (All / Assets / Properties / Maintenance).
7. Paginate with stable ordering.
8. Tap result to open owning feature; destination performs fresh authorization.

Never show prior tenant results while fetching a new tenant's search.


## 16. Search query validation and normalization

Proposed bounds: 2–100 visible characters after trimming. Reject control characters, limit wildcard complexity, cap result count and cursor size. Unicode case folding and accent handling must be documented with PostgreSQL collation choices. Parameterize SQL; never interpolate raw search text or JSONB paths.

Whether one-character queries are allowed is a product decision. For a blank query, show an empty prompt or approved recent local searches, not a full database dump.


## 17. Search request and response — proposed

```text
GET /api/workspaces/{workspaceId}/search?q=filter&types=asset,property,maintenance&limit=20&cursor=...
```

```json
{
  "query": "filter",
  "items": [
    {
      "kind": "asset",
      "id": "c7b6872c-90a2-49b1-a7e3-6d3a1785f8d9",
      "title": "HVAC filter",
      "subtitle": "Home",
      "navigationTarget": {
        "type": "asset",
        "id": "c7b6872c-90a2-49b1-a7e3-6d3a1785f8d9"
      }
    }
  ],
  "nextCursor": null
}
```

These routes and DTOs are **proposed** and must be verified in generated ABP OpenAPI. `navigationTarget` is a UI hint only, not proof of permission.


## 18. Search backend boundary

```csharp
public sealed record SearchRequest(
    string Query,
    IReadOnlyList<string> Types,
    int Limit,
    string? Cursor);

public interface IWorkspaceSearchService
{
    Task<SearchPageDto> SearchAsync(
        Guid workspaceId,
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken);
}
```

Application service verifies membership and named search permission, then invokes tenant-scoped read queries. If search is implemented as multiple queries instead of one endpoint, preserve equivalent security and stable merge/ranking behavior.


## 19. Search ranking and sorting

Proposed deterministic ranking:

| Rank | Match |
|---|---|
| 1 | Exact title/name |
| 2 | Title/name prefix |
| 3 | Title/name substring |
| 4 | Approved secondary metadata |

Within rank sort by normalized title and stable entity ID; if multiple entity types can share an ID, add kind to tie-break. Do not promise fuzzy relevance without an implemented scorer. Search query changes invalidate cursors.


## 20. Search pagination and cursor

Use a bounded page size (20 proposed). A cursor should encode or securely reference: tenant, normalized query, selected types, ranking/sort position and version, plus an expiry or integrity protection if necessary. Do not accept a cursor from workspace X in workspace Y.

Keyset pagination is preferred when stable ranking is feasible; offset pagination may be sufficient for small MVP datasets but must be deterministic and bounded. Test inserts/updates between pages.


## 21. PostgreSQL search plan

An initial implementation can use parameterized `ILIKE` with properly escaped wildcards and tenant predicates. Benchmark `pg_trgm` indexes before deploying substring search at scale.

```sql
SELECT
    a."Id",
    a."Name"
FROM "Assets" AS a
WHERE a."TenantId" = @TenantId
  AND a."IsArchived" = FALSE
  AND a."Name" ILIKE @EscapedPattern ESCAPE '\'
ORDER BY a."Name", a."Id"
LIMIT @Limit;
```

The application must build `@EscapedPattern` safely, escaping `%`, `_` and the escape character. Check actual PostgreSQL syntax and query plans in integration tests. For combined search, every UNION branch must include tenant predicates; never apply tenant filtering only after a global result set is assembled.


## 22. Indexing and performance

Start with existing tenant-scoped indexes from the canonical database spec. Profile actual queries for `Assets.Name`, property names and rule titles. `pg_trgm` may improve `%substring%` queries but adds index/storage/write cost. Do not index arbitrary JSONB custom fields without explicit search requirements.

Use DB-side counts, bounded card lists, stable pagination and cancellation. Measure p50/p95 search and dashboard latency under representative tenants; exact performance budgets are to be approved after a baseline.


## 23. Search and archived records

Default search includes active assets, active properties and enabled rules on active assets. Whether an owner/admin can search archived assets is a separate explicitly permissioned mode. Never expose archived content to a user simply because they guessed a query or deep link. Historic maintenance records belong in authorized history flows, not ordinary global search unless approved.


## 24. Mobile TanStack Query keys

```ts
export const dashboardKeys = {
  summary: (workspaceId: string, timezone: string) =>
    ['workspaces', workspaceId, 'dashboard', timezone] as const,
};

export const searchKeys = {
  page: (
    workspaceId: string,
    query: string,
    types: readonly string[],
    cursor: string | null
  ) => ['workspaces', workspaceId, 'search', query, types, cursor] as const,
};
```

Use stable normalized filters. On workspace switch, cancel outstanding requests and remove prior-tenant visible data. Avoid `keepPreviousData` behavior that displays another workspace's results. Do not persist sensitive search terms to analytics.


## 25. Mobile TypeScript result model

```ts
type SearchKind = 'asset' | 'property' | 'maintenance';

type SearchItem = {
  kind: SearchKind;
  id: string;
  title: string;
  subtitle?: string | null;
};

type SearchPage = {
  query: string;
  items: SearchItem[];
  nextCursor: string | null;
};

type DashboardAttentionItem = {
  occurrenceId: string;
  assetId: string;
  title: string;
  displayStatus: 'Upcoming' | 'Due' | 'Overdue' | 'Unknown';
  dueOn?: string | null;
  dueMileage?: number | null;
};
```

Illustrative only; generated API DTOs are authoritative. Do not conflate `displayStatus` with `MaintenanceLifecycleStatus`.


## 26. Cross-feature invalidation matrix

| Mutation | Refresh |
|---|---|
| Create/edit/archive asset | Asset lists, dashboard, search |
| Create/edit/archive property | Property lists, dashboard, search |
| Create/disable maintenance rule | Maintenance lists, dashboard, search |
| Generate occurrence | Maintenance overview, dashboard |
| Complete/skip/cancel occurrence | Maintenance detail/list, dashboard, recent activity |
| Add/correct odometer reading | Mileage detail, maintenance urgency, dashboard |
| Create ad hoc service record | History, dashboard recent activity |
| Switch workspace | All tenant-owned queries and navigation |
| Revoke membership | Clear affected workspace cache and protected screens |

Prefer targeted invalidation over global refetch loops. Server remains authoritative after mutations.


## 27. Workspace security and IDOR tests

For every dashboard/search request: authenticate, verify active `TenantMembers` relationship, check permission, scope query to workspace and validate referenced entities. A user who belongs to both X and Y must not receive X data when requesting Y. A user removed from X must not continue seeing X cached data or new API responses.

Do not return foreign asset names, maintenance titles, file names or property addresses in 403/404 errors. Search results must never be treated as authorization tokens.


## 28. Offline, freshness and network failures

Cached dashboard/search results are only a convenience if local caching policy allows them. Show `asOfUtc` or “Last updated” on offline content; disable completion and other authoritative mutations. If membership cannot be revalidated after a sensitive session transition, do not continue displaying stale tenant data indefinitely.

For search, offline behavior may be limited to clearly labeled cached results; do not claim full offline global search without an explicit local index design.


## 29. Accessibility and premium UI

Support VoiceOver/TalkBack, scalable text, safe areas, clear focus order and large touch targets. Convey urgency with text and accessible labels, never color alone. Provide empty-state actions with descriptive verbs. Keep summary cards readable at large font sizes and do not truncate the only identifier for an asset. Reduce animation when OS reduced-motion is enabled.


## 30. Error response mapping

| Condition | UI behavior |
|---|---|
| 401 | Supported refresh then sign-in |
| 403 | Permission unavailable; clear protected content |
| 404 | Neutral item unavailable |
| 409 | Refresh authoritative state |
| 422 | Show query/filter validation |
| 429 | Respect server retry hints |
| 5xx | Retry without infinite loop |
| Timeout/offline | Preserve only safe authorized cache |
| Invalid cursor | Reset pagination and requery |
| Workspace switched | Discard obsolete responses |


## 31. Privacy and observability

Metrics: dashboard query duration, search p50/p95, result count, empty searches, cursor failures, index/query-plan regressions, membership denials, stale cache incidents and error rates. Use tenant/correlation identifiers in controlled logs but avoid raw private search terms, asset serial numbers, document names, exact location data and S3 presigned URLs. Log permission failures without exposing foreign record details.


## 32. Dashboard QA scenarios

| ID | Scenario | Expected |
|---|---|---|
| DS-001 | Empty workspace | Add first asset CTA |
| DS-002 | Standalone asset | Counted and navigable |
| DS-003 | Archived asset | Excluded from active count |
| DS-004 | Open time occurrence due today | Due display |
| DS-005 | Open time occurrence past due | Overdue display |
| DS-006 | Completed past-due occurrence | Not counted overdue |
| DS-007 | Mileage reading absent | Unknown |
| DS-008 | Mileage reading at threshold | Due |
| DS-009 | New completion while Home open | Refresh/revalidation |
| DS-010 | Recent ad hoc history | Included if authorized |
| DS-011 | Large tenant | Bounded queries and cards |
| DS-012 | Partial backend error | Honest partial/retry state |
| DS-013 | Offline cached Home | Staleness labeled |
| DS-014 | X→Y workspace switch | No X data flashes |
| DS-015 | Membership revoked | Protected content cleared |
| DS-016 | Timezone boundary | Approved local-day calculation |
| DS-017 | Unknown vs zero mileage | Never conflated |
| DS-018 | No maintenance rules | Useful onboarding CTA |
| DS-019 | Accessibility large text | No hidden critical actions |
| DS-020 | Duplicate refresh requests | No inconsistent UI corruption |


## 33. Search QA scenarios

| ID | Scenario | Expected |
|---|---|---|
| DS-021 | Exact asset name | Highest matching rank |
| DS-022 | Prefix match | Ranked after exact |
| DS-023 | Substring match | Correct escaped query |
| DS-024 | Property name | Tenant-owned result |
| DS-025 | Maintenance rule title | Authorized result |
| DS-026 | Archived asset | Excluded by default |
| DS-027 | Standalone asset | Searchable |
| DS-028 | Cross-tenant same name | Only selected tenant |
| DS-029 | Non-member search | Denied |
| DS-030 | Guess foreign ID from result link | Destination denied |
| DS-031 | SQL wildcard characters | Treated per documented policy |
| DS-032 | Control characters | Validation |
| DS-033 | Query over length limit | Validation |
| DS-034 | Empty query | Prompt, no unbounded dump |
| DS-035 | Rapid typing | Only latest query rendered |
| DS-036 | Switch workspace mid-request | Old response discarded |
| DS-037 | Cursor reused with new query | Rejected/reset |
| DS-038 | Cursor reused across tenants | Rejected |
| DS-039 | Pagination ties | Stable ordering |
| DS-040 | Search result deleted before tap | Safe not found |
| DS-041 | No matches | Clear filters CTA |
| DS-042 | 429 rate limit | Controlled retry |
| DS-043 | Accent/case matching | Approved collation policy |
| DS-044 | Screen reader result | Kind/title announced |
| DS-045 | Search analytics | No raw sensitive terms logged |


## 34. Build order — M7

1. Verify M0 global identity and membership model.
2. Reuse completed M2–M4 property, asset and maintenance APIs.
3. Define a shared server-side urgency classifier and test time/mileage edge cases.
4. Implement dashboard read model using bounded PostgreSQL queries.
5. Implement tenant-safe global search over approved entity fields.
6. Add deterministic ranking, pagination and validated cursors.
7. Generate TypeScript client from verified OpenAPI.
8. Build Home cards, attention list and Search UI using design tokens.
9. Add tenant-safe query caching, invalidation, refresh and offline labels.
10. Run integration tests against PostgreSQL and multi-workspace fixtures.
11. Profile query plans, index only demonstrated bottlenecks.
12. Complete accessibility, low-end Android/iOS and beta security QA.


## 35. Open decisions and ADR updates

1. Final Home tab order and optional Properties shortcut.
2. Exact due/grace/upcoming windows and timezone precedence.
3. Classification of future outside-window Open occurrences.
4. Mileage “past due by X miles” display wording.
5. Whether recent activity includes document uploads or only service records.
6. Searchable property address/nickname fields and privacy limits.
7. Whether document metadata search enters MVP.
8. Query min/max length, debounce, page-size and rate limits.
9. Search collation/accent handling and relevance ranking.
10. Cursor format/integrity and expiry policy.
11. Cached dashboard/search retention and offline visibility.
12. Dashboard snapshot consistency level and refresh cadence.
13. Whether archived entities have a separate admin search mode.
14. Actual ABP routes, permissions and DTO names after OpenAPI generation.

Document decisions before implementing dependent code. Do not treat examples as approved defaults.


## 36. Definition of Done

- [ ] Home shows correct active counts for current workspace.
- [ ] Standalone assets are included without requiring a property.
- [ ] Maintenance urgency is derived and lifecycle stays canonical.
- [ ] Missing mileage remains Unknown.
- [ ] Dashboard cards navigate to reauthorized resources.
- [ ] Search covers approved asset/property/maintenance fields only.
- [ ] Every search branch and dashboard join is tenant-scoped.
- [ ] Query input is normalized, bounded and parameterized.
- [ ] Ranking and pagination are deterministic.
- [ ] Workspace switch/revocation cannot expose stale tenant data.
- [ ] Loading, empty, offline and error states are truthful.
- [ ] PostgreSQL performance is measured under realistic data.
- [ ] Accessibility and premium design tokens are implemented.
- [ ] DS-001 through DS-045 are tested or tracked with evidence.
- [ ] API routes and permissions are verified against actual OpenAPI.


## 37. Final implementation principle

**Dashboard summarizes; Search locates; domain services decide.** Neither read feature creates new business truth, grants permission or guesses missing maintenance data. Correct tenant isolation and understandable next actions matter more than visual complexity.

