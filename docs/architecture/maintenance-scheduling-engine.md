# Kepnora — Maintenance Scheduling Engine

**Target:** `docs/architecture/maintenance-scheduling-engine.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Scope:** Phase 1 · .NET 10 / ABP / EF Core / PostgreSQL  
**Status:** Technical specification; open decisions are explicitly flagged.

**References:** `docs/architecture/database-overview.md`, `docs/features/maintenance-management.md`, `docs/architecture/background-jobs-and-outbox.md`, `docs/architecture/tenant-isolation.md`, `docs/api/api-contracts-master.md`, `docs/testing/test-strategy.md`.

**Source-of-truth rules:** `MaintenanceRules.ScheduleType` is `Time` or `Mileage`; `AnchorPolicy` is `Scheduled` or `Completion`. Persisted occurrence lifecycle is only `Open`, `Completed`, `Skipped`, `Cancelled`. `Upcoming`, `Due`, `Overdue` and `Unknown` are calculated for display. Mileage is observed, never estimated. Every workspace is an ABP tenant.

---
## 1. Goals and boundaries

Generate deterministic maintenance obligations, expose due/overdue states from current facts, and keep service history correct under concurrent edits, retries, and downtime. The engine must be independently unit-testable with a controllable clock.

Not Phase 1: hybrid time-or-mileage rules, predictive mileage, automatic vehicle telemetry, cross-workspace shared schedules, booking or payments. Do not silently add these to the schema.


## 2. Canonical persistence mapping

| Table | Key fields | Critical constraint |
|---|---|---|
| `MaintenanceRules` | `TenantId`, `AssetId`, `ScheduleType`, `AnchorPolicy`, `IntervalDays`, `IntervalMonths`, `IntervalMiles`, `StartOn`, `StartMileage`, `TimeZoneId`, `LastCompletedOn`, `LastCompletedMileage`, `Version` | Time has exactly one positive day/month interval; Mileage has positive miles interval |
| `MaintenanceOccurrences` | `TenantId`, `RuleId`, `ScheduleKey`, `DueOn`, `DueMileage`, `LifecycleStatus`, `CompletedAt`, `Version` | Unique `(TenantId, RuleId, ScheduleKey)` |
| `MaintenanceRecords` | `TenantId`, `AssetId`, nullable `OccurrenceId`, `PerformedOn`, `OdometerMileage` | Unique `(TenantId, OccurrenceId)` when non-null |
| `AssetOdometerReadings` | `TenantId`, `AssetId`, `Mileage`, `RecordedAt`, `Source` | Nonnegative observed mileage |

`Asset.PropertyId` is nullable; maintenance is linked to the asset, not a required property. SQL identifiers are quoted PascalCase PostgreSQL identifiers.


## 3. Rule validation

Time rules require exactly one of `IntervalDays` or `IntervalMonths`, positive; `IntervalMiles` must not be used as a competing active interval. Mileage rules require `IntervalMiles > 0`. Define and validate the initial anchor: `StartOn` for time, `StartMileage` for mileage, or an explicitly approved fallback. The current DB CHECK constraints do not require those anchors; enforce chosen policy in domain/application validation or add reviewed constraints.

Time zone must be a valid IANA zone when provided. Do not let invalid values fall back silently. All rule writes require membership and named permission.


## 4. Engine input and output

```csharp
public sealed record ScheduleInput(
    Guid TenantId, Guid RuleId, ScheduleType Type,
    AnchorPolicy Anchor, int? IntervalDays, int? IntervalMonths,
    int? IntervalMiles, DateOnly? StartOn, int? StartMileage,
    DateOnly? LastCompletedOn, int? LastCompletedMileage,
    string? TimeZoneId, long Version);

public sealed record OccurrenceCandidate(
    string ScheduleKey, DateOnly? DueOn, int? DueMileage);

public interface IMaintenanceScheduleCalculator
{
    IReadOnlyList<OccurrenceCandidate> Calculate(
        ScheduleInput rule, ScheduleWindow window);
}
```
Illustrative C# contracts, not compiled application code. `ScheduleWindow` must define a bounded time horizon or mileage count. The calculator should not query a database or call Expo.


## 5. Scheduled-anchored time recurrence

For an approved first-due date `A`, day-based recurrence is `A + n × IntervalDays`. Month-based recurrence must calculate each due date **from the original anchor** (`A.AddMonths(n × intervalMonths)`), never from the previous clamped due date.

Example with anchor 2026-01-31 and monthly interval: 2026-01-31, 2026-02-28, 2026-03-31, 2026-04-30. Repeatedly adding a month to February 28 would drift to March 28 and is prohibited. Whether `n` starts at 0 or 1 is an **open first-occurrence policy**; do not hardcode a decision without an ADR.


## 6. Day recurrence examples

| Anchor | Interval | n | Due date |
|---|---:|---:|---|
| 2026-10-01 | 7 days | 0 | 2026-10-01 |
| 2026-10-01 | 7 days | 1 | 2026-10-08 |
| 2026-10-01 | 7 days | 2 | 2026-10-15 |
| 2026-12-30 | 7 days | 1 | 2027-01-06 |

These demonstrate arithmetic only. The start index depends on the approved interpretation of `StartOn`. Use `DateOnly` for due dates rather than converting through local midnight UTC.


## 7. Month recurrence examples

| Original anchor | Interval | n | Due date |
|---|---:|---:|---|
| 2026-01-31 | 1 month | 1 | 2026-02-28 |
| 2026-01-31 | 1 month | 2 | 2026-03-31 |
| 2024-02-29 | 12 months | 1 | 2025-02-28 |
| 2024-02-29 | 12 months | 4 | 2028-02-29 |

Check leap years, year boundaries, and invalid overflow. Never use a fixed 30-day approximation for a calendar month.


## 8. Completion-anchored time recurrence

After a scheduled occurrence is completed, the next due date is based on the **actual `PerformedOn`** or another explicitly approved completion date source. The database also stores `LastCompletedOn`; update it in the same transaction as completion.

Example: interval 3 months, performed 2026-10-12 → next due 2027-01-12. The anchor changes on each actual completion, unlike scheduled-anchored rules. If multiple Open occurrences exist, decide whether completion cancels/supersedes them; do not create conflicting future obligations automatically.


## 9. Scheduled-anchored mileage recurrence

With initial baseline `M` and positive interval `I`, candidate threshold `M + n × I`. Example: baseline 20,000, interval 5,000 → thresholds 20,000, 25,000, 30,000 (or start at 25,000 if ADR selects first due after baseline).

Use checked integer arithmetic; reject overflow. The threshold does not depend on elapsed time. `ScheduleKey` may be `MILE:25000`, stable under retries. Do not assume a current odometer reading exists merely because a threshold can be generated.


## 10. Completion-anchored mileage recurrence

Use the **actual recorded mileage at completion**, if supplied and validated, as the next anchor: e.g., completed at 30,520 with 5,000-mile interval → next due 35,520. Update `LastCompletedMileage` atomically with the history record and occurrence transition.

**Open decision:** what to do when a mileage-based completion has no mileage reading. Recommended gate: require an observed mileage for completion-anchored mileage rules, or define an explicit deferred/unknown policy. Never substitute the due threshold as the actual reading.


## 11. Mileage observation semantics

Only `AssetOdometerReadings` or explicitly recorded actual `MaintenanceRecords.OdometerMileage` may be considered observed mileage under an approved precedence rule. Readings include timestamp and source. A reading of `0` is real and distinct from no reading.

**Open policy:** late-arriving observations, correction events, odometer rollovers/resets, and non-monotonic readings. Do not silently take `MAX(Mileage)` if a newer lower value represents an odometer replacement. Use a defined authoritative latest-valid observation strategy.


## 12. Derived display status

Persisted `LifecycleStatus != Open` remains Completed/Skipped/Cancelled. For Open time occurrences, compare `DueOn` to the **current local calendar date in the effective rule timezone**, with approved due-soon window/grace policy. For Open mileage occurrences, compare `DueMileage` with the latest **valid observed** mileage. If none exists, display `Unknown`.

`Upcoming`, `Due`, `Overdue` are presentation labels; they are never written into `MaintenanceOccurrences.LifecycleStatus`. Avoid an impossible exact overdue distinction for mileage unless a mileage-specific grace/threshold policy is approved.


## 13. Time zone precedence and DST

Proposed precedence requiring ADR approval: rule `TimeZoneId` → property timezone when property exists → workspace default timezone → configured fallback. Do not infer the timezone from server locale or phone locale.

Due dates are `DateOnly` and do not become UTC instants automatically. For reminder dispatch, resolve local reminder time through the IANA timezone and define behavior for DST gaps/overlaps. Tests must cover midnight boundaries, timezone edits and leap years.


## 14. Deterministic schedule keys

The canonical database gives examples `TIME:2026-11-30` and `MILE:25000`. Generate keys from the logical scheduled obligation, not a random GUID or job execution timestamp. Enforce max 120 characters.

Unique `(TenantId, RuleId, ScheduleKey)` makes repeat generation safe. For completion-anchored rules, a future occurrence may need a generation sequence/revision key to avoid collisions when two completions produce the same due date/mileage; this is an **open ADR** and must be resolved before enabling that path.


## 15. Generation horizon and bounded batches

Time rules should generate only a bounded preview horizon (for example, next N days and a small per-rule cap; actual values must be configured). Mileage rules should generate a bounded number of future thresholds; do not invent an expected date. Never materialize decades of monthly occurrences.

Workers page tenants/rules with stable cursors, honor cancellation, and checkpoint progress. Downtime catch-up is bounded and idempotent. Expose backlog and age metrics.


## 16. Idempotent insert pattern

```sql
INSERT INTO "MaintenanceOccurrences"
    ("Id", "TenantId", "RuleId", "ScheduleKey",
     "DueOn", "DueMileage", "LifecycleStatus", "Version")
VALUES
    (@id, @tenantId, @ruleId, @scheduleKey,
     @dueOn, @dueMileage, 'Open', 1)
ON CONFLICT ("TenantId", "RuleId", "ScheduleKey") DO NOTHING;
```

`ON CONFLICT` is PostgreSQL syntax. Do not update existing Completed/Skipped/Cancelled rows to Open during regeneration. Insert under validated tenant context and parent FK. Use EF Core or carefully parameterized SQL with a single authoritative repository boundary.


## 17. Worker coordination

Select **one** durable scheduling/job provider and one outbox implementation per event stream, as described in the background jobs ADR. Multiple workers may process distinct claimed rules but must not generate conflicting next occurrences. Use database leases, provider locks, or safe row claims with unique constraints.

A worker must establish `ICurrentTenant` for each job and dispose the scope afterward. `ICurrentTenant` is not user authorization: user-triggered requests require membership; trusted system jobs require validated tenant IDs and narrow system permissions.


## 18. Claim and lease strategy

A robust claim loop can use a job provider's native lease/visibility semantics. If custom PostgreSQL claiming is selected, use bounded transactions and `FOR UPDATE SKIP LOCKED` on a dedicated claimable job/outbox table. Do **not** lock the entire `MaintenanceRules` table for a long-running generation batch.

Lease expiry must permit safe replay. Generation correctness depends on unique keys and deterministic calculations, not on a belief that jobs execute exactly once.


## 19. Completion transaction

1. Authenticate actor and validate active tenant membership + named maintenance permission.
2. Load occurrence, rule and asset under matching `TenantId`; verify `Open` and expected version.
3. Validate performed date, cost, currency and optional actual odometer.
4. Insert `MaintenanceRecord` with matching `AssetId` and `OccurrenceId`.
5. Transition occurrence to `Completed`; set `CompletedAt`, increment `Version`.
6. Update `LastCompletedOn` / `LastCompletedMileage` only per anchor policy.
7. Write durable outbox event in same DB transaction.
8. Commit; schedule next completion-anchored occurrence safely.

`UX_MaintenanceRecords_Occurrence` enforces one linked history row. Do not call Expo within the transaction.


## 20. Concurrency scenarios

| Race | Required outcome |
|---|---|
| Two completion requests | Exactly one scheduled history row |
| Completion versus skip | One valid terminal state, other conflict |
| Rule edit versus generation | No orphan/contradictory obligations |
| Rule disable versus dispatch | Revalidate and suppress stale reminder |
| Two workers generate same key | One row, no duplicate effect |
| Two owners update same rule version | Optimistic conflict or approved serialized update |
| Member revoked before action | Action denied despite valid token |

Use real PostgreSQL integration tests; EF InMemory cannot prove these guarantees.


## 21. Rule editing policy

The schema has `MaintenanceRules.Version` but no dedicated `MaintenanceRuleRevisions` table. Edits to intervals/anchor/timezone may change future due obligations. Choose one explicit policy: preserve existing Open rows, cancel and regenerate future Open rows, or add versioned revision storage through an ADR/migration.

Never rewrite Completed history or reuse a `ScheduleKey` to mean a different obligation. Pending reminder outbox events must revalidate current rule state at send time.


## 22. Disable and re-enable behavior

`IsEnabled = false` stops future generation and new reminders. Whether already Open occurrences remain actionable is an explicit product decision. Re-enabling must not duplicate existing occurrences. Archived assets and disabled rules should not create new reminders without policy approval.

Do not interpret disable as automatic cancellation of historical records.


## 23. Skip and cancel behavior

`Skipped` means work was not performed; `Cancelled` means obligation withdrawn. Neither creates a completed maintenance record. Whether skip advances a scheduled or completion anchor is an open decision; do not silently treat it as completion.

The canonical occurrence table has no `Reason` column. If UI collects a reason, persist only via approved audit/metadata strategy.


## 24. Ad hoc maintenance history

`MaintenanceRecords.OccurrenceId` may be null for unplanned service. An ad hoc record is a historical fact, not automatically a scheduled completion. If ad hoc service should reset a completion anchor, that is a separate explicit user action/policy with transactional effects and audit.


## 25. Outbox event design

Illustrative events: `MaintenanceOccurrenceGenerated`, `MaintenanceOccurrenceCompleted`, `MaintenanceRuleChanged`, `MaintenanceReminderEligible`. Payloads should include stable tenant/entity IDs, event version, timestamp and correlation ID—not private document contents.

Use the approved ABP or custom outbox, **not both for the same stream**. At-least-once handling requires deduplication and current-state checks. Do not promise exactly-once Expo push delivery.


## 26. Notification eligibility

At dispatch, verify tenant and user active membership, device validity, per-workspace notification preference, enabled rule, Open occurrence and due/reminder policy. A completion after enqueue but before send must suppress the reminder. Quiet hours and DST policy must be defined by notification engine ADR.

No permanent notification delivery state should be inferred from provider acceptance alone.


## 27. API boundary

Proposed endpoints (verify against actual ABP-generated OpenAPI):

```text
GET   /api/v1/workspaces/{workspaceId}/assets/{assetId}/maintenance-rules
POST  /api/v1/workspaces/{workspaceId}/assets/{assetId}/maintenance-rules
PATCH /api/v1/workspaces/{workspaceId}/maintenance-rules/{ruleId}
GET   /api/v1/workspaces/{workspaceId}/maintenance-occurrences
POST  /api/v1/workspaces/{workspaceId}/maintenance-occurrences/{id}/complete
POST  /api/v1/workspaces/{workspaceId}/maintenance-occurrences/{id}/skip
POST  /api/v1/workspaces/{workspaceId}/maintenance-occurrences/{id}/cancel
POST  /api/v1/workspaces/{workspaceId}/assets/{assetId}/odometer-readings
```

Return lifecycle and derived display state separately. Validate membership, permission and tenant-bound resource IDs on every endpoint.


## 28. Mobile presentation

Show next due date or mileage threshold, current observed mileage if available, and a clear Unknown state when not available. Do not display invented “estimated due in 3 weeks” for mileage.

Completion form collects actual performed date, optional cost/notes and odometer where relevant. Handle loading, offline, 409 conflicts, duplicate taps and workspace switches. React Query cache keys must include workspace ID.


## 29. Algorithm pseudocode — time

```text
calculateTime(rule, horizon):
  validate(rule)
  anchor = approvedStartOrCompletionAnchor(rule)
  for n in approvedStartIndex..maxCandidateCount:
    due = anchor + n * intervalDays
       OR originalAnchor.AddMonths(n * intervalMonths)
    if due > horizon.end: break
    if due < horizon.start: continue
    yield ("TIME:" + ISODate(due), dueOn=due, dueMileage=null)
```

For completion anchor, normally generate only the next obligation until completion. Do not use repeated month addition from a previously clamped date.


## 30. Algorithm pseudocode — mileage

```text
calculateMileage(rule, countLimit):
  validate(rule)
  anchor = approvedStartOrCompletionMileage(rule)
  for n in approvedStartIndex..countLimit:
    threshold = checked(anchor + n * intervalMiles)
    yield ("MILE:" + threshold, dueOn=null,
           dueMileage=threshold)

classifyMileage(occurrence, observed):
  if lifecycle != Open: return lifecycle
  if observed is null: return Unknown
  if observed.mileage < dueMileage: return Upcoming
  return DueOrOverdueByApprovedMileagePolicy
```

No time-based mileage estimation. If observed mileage goes backward, follow explicit correction/reset policy.


## 31. Scheduling service boundaries

```text
Domain:
  IMaintenanceScheduleCalculator
  TimeScheduleCalculator
  MileageScheduleCalculator
  OccurrenceStatusClassifier
Application:
  MaintenanceRuleAppService
  MaintenanceCompletionAppService
  MaintenanceGenerationCoordinator
Infrastructure:
  EF Core repositories and constraints
Worker:
  GenerateMaintenanceOccurrencesJob
  DispatchReminderJob
```

Keep pure recurrence math out of EF Core and HTTP. Use a testable `TimeProvider` or clock abstraction for current date/time.


## 32. Query and indexing strategy

Canonical indexes include `IX_MaintenanceRules_TenantId_AssetId` for enabled rules and `IX_MaintenanceOccurrences_Tenant_Status_Due` for Open time occurrences. Additional mileage/status indexes require measured query plans.

Example tenant-scoped time due query:

```sql
SELECT "Id", "RuleId", "DueOn"
FROM "MaintenanceOccurrences"
WHERE "TenantId" = @tenantId
  AND "LifecycleStatus" = 'Open'
  AND "DueOn" <= @throughDate
ORDER BY "DueOn", "Id"
LIMIT @limit;
```

Mileage classification requires an actual valid observation per asset; optimize via bounded queries and indexes rather than N+1 requests.


## 33. Performance safeguards

Limit candidate generation per rule and per batch, configure worker concurrency, and use cursor pagination. Do not scan all tenants into memory. Benchmark 1K/10K/100K rules with representative schedules and multiple tenants; set concrete SLOs from measurement.

Monitor DB lock wait, conflict rate, job duration, generation lag, outbox age and API p95 latency.


## 34. Failure and retry matrix

| Failure | Recovery |
|---|---|
| Worker crashes before insert | Retry same deterministic keys |
| Worker crashes after commit | Retry harmless unique conflict |
| DB connection loss during completion | Refresh status; idempotency/unique constraint |
| Two jobs claim same rule | Unique key + coordination |
| Invalid timezone | Quarantine rule and alert; no silent fallback |
| Bad odometer correction | Explicit review, no fabricated value |
| Expo unavailable | Durable outbox retry; occurrence remains authoritative |
| Long downtime | Bounded catch-up and backlog alerts |
| Rule edited during generation | Version check/retry under policy |


## 35. Security and audit

Record actor, tenant, rule/occurrence ID, operation, safe before/after state, timestamp and correlation ID. Do not log private notes, device tokens, presigned URLs or raw personal documents. Enforce tenant-aware FKs, explicit membership and permissions.

Background jobs are privileged internal actions and must use validated tenant IDs; never allow arbitrary user-supplied tenant context to bypass membership checks.


## 36. Unit test matrix — calendar

| ID | Case | Expectation |
|---|---|---|
| SCH-U01 | Daily interval 1 | Consecutive dates |
| SCH-U02 | Weekly interval 7 | Seven-day spacing |
| SCH-U03 | Jan 31 monthly | Feb clamp, Mar 31 recovery |
| SCH-U04 | Leap day yearly | Feb 28, leap restoration |
| SCH-U05 | DST spring | Local date stable |
| SCH-U06 | DST fall | Local date stable |
| SCH-U07 | Year boundary | Correct next year |
| SCH-U08 | Zero interval | Rejected |
| SCH-U09 | Both days and months | Rejected |
| SCH-U10 | Time and mileage hybrid | Rejected |
| SCH-U11 | Missing start anchor | Approved validation |
| SCH-U12 | Completion late | Next due from actual completion |
| SCH-U13 | Scheduled late completion | Original cadence unchanged |
| SCH-U14 | Generation horizon | Bounded |
| SCH-U15 | Same inputs twice | Same schedule keys |


## 37. Unit test matrix — mileage

| ID | Case | Expectation |
|---|---|---|
| SCH-U16 | 20K baseline + 5K | Deterministic thresholds |
| SCH-U17 | No reading | Unknown |
| SCH-U18 | Reading zero | Not null |
| SCH-U19 | Reading below threshold | Upcoming |
| SCH-U20 | Reading at threshold | Due |
| SCH-U21 | Reading above threshold | Approved due/overdue policy |
| SCH-U22 | Actual completion mileage | New completion anchor |
| SCH-U23 | No completion mileage | Approved validation/unknown |
| SCH-U24 | Overflow | Rejected |
| SCH-U25 | Late reading | Approved precedence |
| SCH-U26 | Odometer reset | Explicit correction policy |
| SCH-U27 | Unit conversion | No silent km/mile mix |
| SCH-U28 | Repeated calculation | Same mileage keys |


## 38. PostgreSQL integration test matrix

| ID | Case | Expectation |
|---|---|---|
| SCH-I01 | Two workers same key | One occurrence |
| SCH-I02 | Same rule two tenants | Separate tenant rows |
| SCH-I03 | Foreign rule/asset FK | Rejected |
| SCH-I04 | Concurrent complete | One record |
| SCH-I05 | Complete vs skip | One terminal state |
| SCH-I06 | Completed regeneration | Never reopened |
| SCH-I07 | Rule disabled | No new generation |
| SCH-I08 | Last completed anchor | Transactionally updated |
| SCH-I09 | Outbox rollback | No phantom event |
| SCH-I10 | Worker tenant A then B | No context leak |
| SCH-I11 | Rule edit race | Version policy enforced |
| SCH-I12 | Crash/retry | No duplicates |
| SCH-I13 | Member revoked | Action denied |
| SCH-I14 | No reading | No invented mileage |
| SCH-I15 | Service history asset mismatch | Rejected by domain |
| SCH-I16 | PostgreSQL CHECK | Reject invalid interval |
| SCH-I17 | Key length >120 | Rejected |
| SCH-I18 | Idempotency key replay | One logical completion |
| SCH-I19 | Cross-tenant read | No foreign data |
| SCH-I20 | Reminder after completion | Suppressed |


## 39. Observability and operations

Emit counters for generated, duplicate-conflict, skipped-disabled, failed-invalid-rule and completion-conflict. Track oldest unprocessed rule/outbox age, worker heartbeat, per-batch latency and retry/dead-letter counts. Use bounded, low-cardinality labels; never label metrics with individual user or asset IDs.

Operator runbook: identify failing tenant/rule through secured logs, verify current rule version and recent completion, inspect outbox and uniqueness conflicts, replay a bounded batch, and verify no duplicate occurrence/history.


## 40. M0–M8 implementation gates

| Milestone | Scheduling deliverable |
|---|---|
| M0 | Pinned ABP/.NET 10, tenant identity POC, outbox ADR |
| M1 | Membership + permission enforcement |
| M2–M3 | Asset and type references, optional property |
| M4 | Pure calculators, DB constraints, generation worker, atomic completion |
| M5 | Documents linked to history only under approved ownership model |
| M6 | Reminder eligibility + outbox + Expo dispatcher |
| M7 | Due/overdue dashboard and search projections |
| M8 | Race tests, recovery drills, monitoring, beta gates |


## 41. Open decisions before implementation

1. Does first due occur at `StartOn`/`StartMileage`, or one interval later?
2. Exact timezone precedence and DST reminder instant policy.
3. Due-soon window and grace/overdue classification for time.
4. Mileage-specific due versus overdue policy.
5. Missing start anchor validation and existing-data migration.
6. Completion-anchored mileage without observed completion reading.
7. Rule edit policy for already Open occurrences.
8. Completion-anchored schedule key collision/revision design.
9. Skip/cancel impact on recurrence anchors.
10. Disable/re-enable impact on existing Open occurrences.
11. Handling odometer resets, late readings and corrections.
12. Whether ad hoc service can reset completion anchors.
13. Exact outbox/job provider (ABP or custom, not both).
14. Idempotency key store and replay response semantics.
15. Generation horizon, cap and worker lease settings.
16. What happens if a completed occurrence's `PerformedOn` is corrected.

Do not treat examples in this document as a substitute for these ADRs.


## 42. Definition of Done

- [ ] Deterministic time/mileage calculator with injected clock.
- [ ] Month-end and leap-year behavior verified.
- [ ] First-occurrence and edit policies approved.
- [ ] Only canonical lifecycle statuses persisted.
- [ ] Unknown mileage is preserved without prediction.
- [ ] Tenant-aware FK and membership/permission checks pass.
- [ ] Unique schedule keys prevent duplicate generation.
- [ ] Completion writes exactly one history record transactionally.
- [ ] Completion anchor update and outbox are atomic.
- [ ] Worker retries and tenant switching are safe.
- [ ] Rule disable/edit and pending reminders are revalidated.
- [ ] All unit and PostgreSQL integration tests pass.
- [ ] OpenAPI matches implementation and mobile client.
- [ ] Monitoring, replay and recovery procedures documented.


## 43. Final principle

**A maintenance schedule is a deterministic obligation derived from explicit rule anchors and real observations—not a prediction.** Historical completion is a durable fact; notification delivery is best-effort. Make every operation safe to retry and safe across tenant boundaries.

