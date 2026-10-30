# Kepnora — Dashboard Feature: Implementation Specification

**Status:** Implementation specification draft · **Client:** Expo / React Native + TypeScript · **Backend:** ASP.NET Core .NET 10 / PostgreSQL

> Routes, limits, and policy defaults marked proposed require OpenAPI/ADR approval. This is a design artifact, not a claim of implemented or tested code.

**Milestone:** M7. Read-only tenant dashboard for inventory and maintenance attention, not a second domain engine.

## 1. Purpose, outcomes, and exclusions

The dashboard answers: what do I own, what needs attention, and what should I do next? It is the first useful screen after onboarding and a navigation surface rather than an alternative source of domain truth. Phase 1 excludes provider quotes, booking, payments, AI recommendations and automatic mileage inference. Standalone assets (null `property_id`) appear alongside property-linked assets.

## 2. Canonical domain rules

- Tenant membership is validated on every request; changing active tenant invalidates the old dashboard cache.
- Persisted occurrence lifecycle is **Open, Completed, Skipped, Cancelled** only.
- **Upcoming, Due, Overdue** are computed display states for open occurrences; do not write them to `lifecycle_status`.
- Mileage-only occurrences with no trustworthy current reading show **Unknown** rather than inventing a date or distance.
- Archived assets/properties are excluded from active counts by default; historic records remain accessible under explicit history flows.
- A dashboard aggregate is a snapshot and must not be treated as the authoritative state when completing maintenance.

## 3. Personas and journeys

| Journey | Entry | Outcome |
|---|---|---|
| New user | Empty tenant | Clear CTA to add first asset; property optional |
| Homeowner | Dashboard | Review due maintenance across multiple properties |
| Vehicle owner | Dashboard | See mileage-triggered item with last reading freshness |
| Multi-tenant member | Tenant switcher | See only selected tenant totals and cards |
| Returning user | Push deep link | Open occurrence after membership check, then return dashboard |
| Offline user | Cached screen | Show last-sync timestamp and disable authoritative actions |

## 4. Navigation and layout

Proposed Expo Router paths: `/(app)/dashboard`, `/(app)/maintenance/[occurrenceId]`, `/(app)/assets/[assetId]`, `/(app)/properties/[propertyId]`. Tabs: Home, Assets, Maintenance, Settings. Home has tenant switcher, greeting, alert summary, due cards, asset/property counts and recent activity. Cards must have accessible labels and distinct overdue/due/unknown text, not color-only meaning.

## 5. Dashboard response contract — proposed

`GET /api/v1/tenants/{tenantId}/dashboard?timezone=America%2FChicago`
```json
{
  "asOfUtc": "2026-10-29T19:00:00Z",
  "tenantId": "8a8f52c1-9d40-4b2a-bad5-86ed8a76aa10",
  "counts": {"activeAssets": 12, "activeProperties": 2, "openOccurrences": 9, "due": 3, "overdue": 2, "upcoming": 3, "unknown": 1},
  "attention": [{"occurrenceId": "3dbb26a4-1f8d-41a2-a5c2-f95d3560e1c7", "assetId": "fb5d55ca-29a5-45d8-97b1-32e1f30ab1e7", "title": "Change engine oil", "displayStatus": "Due", "dueDateLocal": "2026-10-29", "dueMileage": null}],
  "recentActivity": [], "nextCursor": null
}
```
The response is a **read model**, not a new dashboard table. `displayStatus` is derived server-side from rule, occurrence, timezone and readings. API must document nullability and maximum items.

## 6. Derived status and priority ordering

For an open time-based occurrence, compare its local due date with the tenant/property policy timezone. Overdue is past due; Due is due now or within the approved due window; Upcoming is inside the approved lead window. Exact grace and lead windows remain an ADR/product decision. Mileage status compares an explicitly recorded current reading to the threshold, with unknown if no valid reading exists. Hybrid status is not enabled until policy is approved. Proposed sort: Overdue first, Due second, Upcoming third, Unknown fourth; within group sort by earliest due date or mileage gap and stable UUID tie-breaker.

## 7. Database read model and query plan

Sources: `assets`, `properties`, `maintenance_rules`, `maintenance_occurrences`, `asset_odometer_readings`, `maintenance_records`. Scope each join by `(tenant_id, id)` and filter archived entities as documented. Avoid N+1 queries and avoid loading entire history. Prefer one bounded summary query plus one keyset-paginated attention query. Use database-side counts, not fetching thousands of rows into memory. `maintenance_occurrences(tenant_id, lifecycle_status, due_date)` indexing must be checked against actual schema and query plans; do not assume a column exists without reviewing database-overview.

## 8. C# query service boundary

```csharp
public sealed record DashboardQuery(Guid TenantId, Guid UserId, DateOnly TodayLocal);
public interface IDashboardReader
{
    Task<DashboardDto> GetAsync(DashboardQuery query, CancellationToken ct);
}

public async Task<DashboardDto> HandleAsync(
    DashboardQuery query, CancellationToken ct)
{
    await tenantAccess.RequireMembershipAsync(query.UserId, query.TenantId, ct);
    return await dashboardReader.GetAsync(query, ct);
}
```
The reader MUST enforce tenant predicates independently; a controller authorization check alone is insufficient for worker or internal reuse. Do not cache authorization decisions beyond membership/session invalidation guarantees.

## 9. React Native query and tenant switching

```tsx
const dashboardKey = (tenantId: string) => ['tenant', tenantId, 'dashboard'] as const;

export function useDashboard(tenantId: string) {
  return useQuery({
    queryKey: dashboardKey(tenantId),
    queryFn: ({signal}) => api.dashboard.get(tenantId, {signal}),
    enabled: Boolean(tenantId),
    staleTime: 30_000,
  });
}
```
On tenant change: cancel active queries, clear previous tenant-sensitive state, update active tenant, fetch new membership and only then enable dashboard queries. No cross-tenant placeholder data. Proposed 30-second stale time is tunable.

## 10. Loading, empty, offline, and error states

| State | UI behavior | Recovery |
|---|---|---|
| First load | Skeleton cards with accessible busy label | Automatic fetch |
| Empty assets | Explain standalone assets and property option | Add asset CTA |
| No maintenance rules | No false all-clear claim | Add maintenance CTA |
| No due items | “Nothing due based on recorded data” | View all maintenance |
| Unknown mileage | Show “Add current mileage” | Open reading form |
| 401 | Clear session / refresh via approved auth flow | Sign in |
| 403/404 membership | Do not show cached tenant content | Switch tenant |
| 5xx | Retry without duplicating actions | Pull to refresh |
| Offline | Read-only stale data with timestamp | Retry when online |

## 11. Performance and accessibility

Target response payload under a proposed 100 KB for first page; attention list capped (proposed 10), recent activity capped (proposed 5). Measure p50/p95 server time and client render; set SLO only after baseline. Support Dynamic Type, VoiceOver/TalkBack, minimum 44x44 pt touch targets, reduced motion, screen reader ordering, and status text. Test slow Android devices and network throttling.

## 12. Observability and privacy

Log tenant ID, request ID, counts and duration, never raw document URLs, push tokens, private asset notes or street addresses. Metrics: dashboard read latency, query count, cache hit ratio, unknown-mileage count, API error rate. Ensure tenant IDs are not treated as proof of authorization. Trace API -> query service -> PostgreSQL; avoid user PII in spans.

## 13. Acceptance scenarios

| ID | Scenario | Expected |
|---|---|---|
| DASH-01 | Empty tenant | Zero counts and add-asset CTA |
| DASH-02 | Asset without property | Counted and navigable |
| DASH-03 | Cross-tenant dashboard request | No data disclosed |
| DASH-04 | Tenant switch during in-flight request | Old response not rendered |
| DASH-05 | Completed occurrence | Excluded from open due cards |
| DASH-06 | Skipped occurrence | Excluded from open due cards |
| DASH-07 | Missing odometer | Unknown, never guessed |
| DASH-08 | Two properties in different timezones | Approved policy applied consistently |
| DASH-09 | Offline stale cache | Clearly labeled read-only |
| DASH-10 | Archived asset | Excluded from active counts |
| DASH-11 | Duplicate sort priority | Stable tie-breaker |
| DASH-12 | Large tenant | Bounded pagination, no N+1 |
| DASH-13 | Expired membership | No cached tenant leak |
| DASH-14 | DST boundary | Deterministic local date |
| DASH-15 | Screen reader | Status text announced |

## 14. Implementation tasks and definition of done

1. Approve dashboard endpoint and timezone policy in OpenAPI/ADR.
2. Implement authorized tenant-scoped read service with bounded projections.
3. Add EXPLAIN ANALYZE performance fixture with representative asset/occurrence volume.
4. Generate TypeScript API client; implement dashboard hooks and skeleton/empty/error states.
5. Wire cards to asset/maintenance routes and enforce authorization on destination.
6. Add API integration tests for tenant isolation and derived statuses; RN component tests for tenant switch and offline.
7. Document telemetry and accessibility checks. Done only when tests pass and ADR gaps are resolved.
