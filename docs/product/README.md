# Kepnora — Product Documentation

**Canonical path:** `docs/product/README.md`  
**Version:** 1.0-draft · **Updated:** 2026-10-29  
**Audience:** Product owner, engineering, design, QA, AI coding agents  
**Scope:** Product strategy, Phase 1 boundaries and navigation across product specs.

> This README is a product index, not a replacement for the canonical database, architecture, API or feature specifications. Open decisions are not implementation commitments.

---

## 1. Product mission

Kepnora helps households and small asset-owning teams keep an accurate inventory of what they own, understand what needs maintenance, preserve relevant documents, and receive actionable reminders. It combines **properties, standalone assets, maintenance, receipts/manuals and reminders** in one mobile-first workspace. It is not a property marketplace or an automated repair booking service in Phase 1.

## 2. Audience and primary jobs

**Primary:** individuals and households managing homes, vehicles, appliances and electronics. **Secondary:** small teams managing a limited collection of assets across multiple properties. Jobs-to-be-done: record an asset once; find its model/serial details; know what service is due; enter actual odometer readings; attach receipts; find the last completed service; share access with trusted members. Do not assume enterprise facilities management or professional service providers are MVP users.

## 3. Product principles

1. Asset-first, property-optional: a vehicle or laptop needs no property.
2. Workspace isolation is a security invariant, not just a UI filter.
3. Reminders must reflect real records, not guessed mileage.
4. Historical maintenance records must remain understandable after edits.
5. Mobile workflows should be quick enough for occasional household use.
6. Protect private addresses, files and device tokens.
7. Avoid premature marketplace, payment or IoT dependencies.
8. Make all meaningful failure states visible.

## 4. MVP feature overview

| Capability | Phase 1 behavior |
|---|---|
| Account | Global login, secure mobile session |
| Workspaces | Create/select workspace; Owner/Member with server permissions |
| Properties | Optional locations, CRUD and archive |
| Asset types | Global and workspace-specific versioned schemas |
| Assets | CRUD, optional property, custom fields |
| Maintenance | Rules, generated occurrences, complete/skip/cancel, history |
| Odometer | User-entered actual mileage, no projections |
| Documents | Private S3-backed photos, manuals and receipts |
| Reminders | Due/upcoming/overdue eligibility and Expo push |
| Dashboard/search | Tenant-scoped overview and asset/maintenance search |
| Settings | Workspace membership and notification preferences |

## 5. Phased roadmap boundaries

**Phase 1 — Own & Maintain:** inventory, maintenance, documents, reminders, settings and search.

**Phase 2 — Find Services:** provider discovery within Kepnora, potentially behind an `IProviderDiscoveryService` abstraction. Discovery is not booking.

**Phase 3 — Book & Pay:** provider availability, scheduling, booking, payment and associated workflows.

Dates, budget, pricing and monetization are *not approved* by this README; see the dedicated product files for proposals and decisions.

## 6. Key domain language

| Term | Definition |
|---|---|
| User | Globally authenticated account |
| Workspace | Collaboration and data-isolation boundary, mapped 1:1 to ABP Tenant |
| Tenant member | Membership/role record connecting global user and workspace |
| Property | Optional location for grouping assets |
| Asset | Owned or managed item such as a vehicle, appliance or electronics |
| Asset type definition | Shared or workspace-specific kind of asset |
| Asset type version | Immutable published custom-field schema |
| Maintenance rule | Repeatable service policy |
| Maintenance occurrence | Concrete scheduled maintenance instance |
| Odometer reading | Manually entered actual mileage |
| Document | Private stored file and metadata |
| Reminder | Notification intent generated from eligible maintenance state |

## 7. Lifecycle vocabulary

Persisted maintenance occurrence statuses are exactly **`Open`, `Completed`, `Skipped`, `Cancelled`**. **`Upcoming`, `Due`, `Overdue`** are computed presentation states and must not be added to persisted status enums. Scheduling uses deterministic keys and concurrency-safe generation. Mileage never increases automatically or through projection.

## 8. Architecture snapshot

```text
React Native + Expo (iOS / Android)
   ├─ Expo Router / TanStack Query / Zustand
   ├─ SecureStore / typed OpenAPI client
   └─ Expo Push registration
                 │ HTTPS / OIDC + PKCE
                 ▼
.NET 10 + ASP.NET Core + ABP modular monolith
   ├─ Global identity / workspace membership authorization
   ├─ Properties / Asset Types / Assets
   ├─ Maintenance / Odometer / Documents
   └─ Reminders / Dashboard / Search
          │                │
          ▼                ▼
 PostgreSQL / EF Core   Private AWS S3
          │
          ▼
 Durable outbox / background worker → Expo Push
```

**Blocker:** ABP/.NET 10 compatibility and global-user/multi-tenant identity must be demonstrated in M0 proof of concept. No document should imply the proof is already complete.

## 9. Workspace and authorization invariants

One workspace is one ABP Tenant. Use ABP Tenant Management as the authoritative registry; do not add a parallel custom `Tenants` table. One global user can belong to multiple workspaces, with membership recorded and validated for every tenant operation. `ICurrentTenant` and `IMultiTenant` filter data but do **not** authorize access. Client workspace selection is never trusted as proof of membership. Query caches and background jobs must be tenant-safe.

## 10. Experience and navigation

The app should offer a clear first-run flow: sign in → create or select workspace → optionally add property → add first asset → optionally add maintenance rule/document. Properties are never a mandatory step. Suggested main navigation: Home, Assets, Maintenance, Search, Settings. Deep links must revalidate authentication, workspace membership and resource access before rendering.

## 11. Reliability and privacy expectations

- S3 objects private; issue short-lived upload/download access only after authorization.
- Upload flow: intent → direct upload → server finalize; unfinalized uploads cleaned up.
- Push is best-effort and may be duplicated or delayed; no claim of guaranteed device delivery.
- Recheck occurrence eligibility and membership before dispatching reminders.
- Use durable outbox, idempotent workers and safe retry policy.
- Never log auth tokens, presigned URLs, full addresses or private file contents.
- Handle revoked membership and workspace switching without data leaks.

## 12. Product documentation map

```text
docs/product/
  README.md              # this guide and product boundaries
  mvp-scope.md           # in/out, acceptance and non-goals
  roadmap.md             # milestone and phased delivery plan
  pricing.md             # pricing hypotheses; not approved rates
  feature-matrix.md      # cross-phase capabilities and dependencies
```

Product documents define **why/what**. Feature documents define user workflows and acceptance. Architecture documents define invariants and engineering decisions. API contracts define proposed interfaces, verified against generated OpenAPI. Implementation documents define source organization. UI docs define screens and handoff.

## 13. Reference map: foundational documents

- `docs/README.md`: documentation entrypoint.
- `docs/kepnora-project-context.md`: project identity and context.
- `docs/architecture-overview.md`: stack and component boundaries.
- `docs/mvp-build-order.md`: M0–M8 dependency sequence and definition of done.
- `docs/database-overview.md` (or relocated `docs/architecture/database-overview.md`): canonical PostgreSQL schema.
- `docs/future-features.md`: post-MVP ideas.
- `docs/architecture/architecture-decision-records.md`: architectural decisions.

When a document is moved, update links instead of creating competing copies.

## 14. Reference map: features

- `docs/features/authentication-onboarding.md`
- `docs/features/property-management.md`
- `docs/features/asset-management.md`
- `docs/features/custom-asset-types.md`
- `docs/features/maintenance-management.md`
- `docs/features/documents-warranties.md`
- `docs/features/notifications-reminders.md`
- `docs/features/dashboard.md` or `dashboard-search.md`
- `docs/features/settings-members.md`

If both old and new names exist, designate one canonical path and leave a redirect note, not contradictory specs.

## 15. Reference map: engineering and UI

- `docs/api/api-contracts-master.md`
- `docs/implementation/backend-project-structure.md`
- `docs/implementation/mobile-project-structure.md`
- `docs/implementation/admin-project-structure.md` (admin portal is deferred)
- `docs/architecture/maintenance-scheduling-engine.md`
- `docs/architecture/notification-engine.md`
- `docs/architecture/tenant-isolation.md`
- `docs/architecture/background-jobs-and-outbox.md`
- `docs/architecture/file-storage.md`
- `docs/ui/README.md`
- `docs/ui/mockups/README.md`
- `docs/ui/design-system/tokens.md`
- `docs/testing/test-strategy.md`
- `docs/deployment/deployment-guide.md`

## 16. Milestones and release gates

| Gate | Product deliverable |
|---|---|
| M0 | ABP compatibility, global identity, workspace POC |
| M1 | Sign-in, workspace onboarding and permissions |
| M2 | Properties and versioned asset types |
| M3 | Asset inventory, optional property association |
| M4 | Maintenance rules, occurrences, history, actual mileage |
| M5 | Private document upload/download |
| M6 | Notification preferences, outbox and Expo Push |
| M7 | Dashboard, search and workspace settings |
| M8 | Security, load, observability, mobile beta and release |

Each gate needs verified implementation, tests, error handling and demo evidence; completion of documentation alone does not pass a gate.

## 17. Product success measures

**Candidate metrics (not yet approved targets):** activation rate (first asset created), time to first asset, percent of assets with a maintenance rule, maintenance completion rate, document attachment adoption, reminder opt-in rate, seven-/30-day retention, failed upload rate, support incidents and tenant-isolation defects. Instrument only after privacy review. Never use a reminder-open metric as proof that a task was completed.

## 18. Non-goals and explicit exclusions

Phase 1 does **not** require service-provider search, appointment booking, payments, automated mileage telemetry, IoT integrations, property valuation, GPS tracking, accounting, enterprise facility hierarchies or a bespoke admin portal. Warranty expiration alerts require approved structured warranty-expiry fields; a stored warranty PDF alone does not imply the system can calculate expiry.

## 19. Decision ownership and change control

Product owner approves user-facing scope, roles and pricing. Engineering owner approves compatibility and implementation tradeoffs through ADRs. Security review is required for identity, cross-tenant access, document sharing and push content. Any scope change updates `mvp-scope.md`, `feature-matrix.md`, relevant feature/API specs and tests together. Avoid implementing features from brainstorming notes alone.

## 20. Open decisions to track

1. Which global identity implementation passes the ABP M0 POC?
2. Owner/Member permission matrix details.
3. Schema dialect and asset-type version upgrade UX.
4. Maintenance anchor and recurrence edge-case policy.
5. Warranty expiration metadata and reminders: MVP or later?
6. Archive/restore behavior for properties and assets.
7. Push notification default preferences and quiet hours.
8. Search result privacy and indexing.
9. Subscription model, entitlements and billing timing.
10. Initial geography, language and launch channel.

No open decision should be silently presented as shipped functionality.

## 21. AI coding agent working agreement

Before coding a feature, read this README, `mvp-build-order.md`, the relevant feature spec, the canonical database schema, tenant-isolation rules and API master. Identify unresolved decisions; do not invent columns, roles or routes. Implement in dependency order; regenerate OpenAPI TypeScript clients; add real PostgreSQL integration tests and mobile error/offline states. Record schema/API deviations in ADRs and update docs with the merged behavior.

## 22. Product acceptance checklist

- [ ] Phase 1 features and exclusions are consistent across roadmap and feature matrix.
- [ ] Property optionality appears in onboarding and asset flows.
- [ ] One workspace = one ABP tenant; no parallel tenant registry.
- [ ] Multi-workspace membership is checked server-side.
- [ ] Mileage uses manually recorded readings only.
- [ ] `Due`/`Overdue` are derived, not persisted occurrence statuses.
- [ ] Private files use authorized S3 intent/finalize.
- [ ] Notifications are best-effort with durable intent and rechecks.
- [ ] No service marketplace, booking, payment or custom admin portal is mistakenly required for MVP.
- [ ] M0 identity/compatibility POC remains a hard prerequisite.
- [ ] Product metrics are hypotheses until approved.
- [ ] Pricing is not treated as decided without signoff.

## 23. Next documents to complete

Create and review in order: `docs/product/mvp-scope.md`, `docs/product/roadmap.md`, `docs/product/pricing.md`, `docs/product/feature-matrix.md`. Then audit all Markdown files, normalize canonical paths and cross-links, and package a complete `docs/` ZIP for GitHub. Verify the archive against the source manifest; do not rely only on an estimated document count.
