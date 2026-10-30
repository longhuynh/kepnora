# Kepnora — Admin Project Structure & Operations Boundary

**Target path:** `docs/implementation/admin-project-structure.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Status:** Proposed design, **not an approved Phase 1 custom admin build**  
**Platform:** .NET 10 + ASP.NET Core + ABP Framework; React Native + Expo customer application

**Related documents:** `docs/implementation/backend-project-structure.md`, `docs/implementation/mobile-project-structure.md`, `docs/architecture/tenant-isolation.md`, `docs/architecture/authentication-authorization.md`, `docs/api/api-contracts-master.md`, `docs/deployment/deployment-guide.md`, `docs/testing/test-strategy.md`, `docs/mvp-build-order.md`.

> **Core decision:** Kepnora Phase 1 does **not** require a custom admin dashboard. Use verified ABP administrative capabilities and infrastructure observability first. The optional internal operations portal described below is a future extension and must not become a blocker for M0–M8.

---
## 1. Purpose

Define what “admin” means in Kepnora, which operations belong to ABP, which are internal support operations, and how an optional admin application would be structured without weakening tenant isolation or duplicating platform features.


## 2. Three distinct authority levels

| Actor | Scope | Example |
|---|---|---|
| Workspace Owner | One ABP tenant | Manage workspace membership/settings |
| Workspace Member | One ABP tenant | Manage permitted assets/maintenance |
| Platform Operator | Restricted host-side operations | Diagnose provisioning/outbox health |

A workspace Owner is **not** a platform administrator. A platform operator does not automatically have permission to browse customer assets. Never merge these identities into one global `Admin` boolean.


## 3. Phase 1 scope decision

**Required:** secure platform configuration, ABP tenant registry, operator audit, worker/health monitoring, production deployment controls and incident runbooks.

**Optional and deferred:** a branded web admin frontend, customer impersonation, manual tenant repair UI, global asset search, support ticket tooling, subscription controls and analytics dashboards.

The absence of a custom admin UI must not prevent normal workspace creation, tenant membership, asset maintenance or mobile settings.


## 4. ABP responsibilities versus Kepnora responsibilities

| Capability | Preferred owner | Note |
|---|---|---|
| Authoritative tenant registry | ABP Tenant Management | Never duplicate Tenants table |
| Platform permissions | ABP Permission Management | Confirm host/tenant semantics in M0 |
| Identity / user lifecycle | Pinned global identity integration | M0 proof required |
| Audit logging | ABP auditing + configured sink | Review privacy/retention |
| Product asset and maintenance logic | Kepnora domain/application | Never admin-only shortcuts |
| Worker/outbox diagnostics | Observability + restricted operations | No duplicate outbox |
| Support actions | Future restricted Ops API | Explicit authorization/audit |
| Workspace Owner settings | Mobile app + tenant API | Not platform admin UI |


## 5. M0 ABP identity compatibility gate

Verify .NET 10 + selected ABP version, identity configuration, host-side operator principal and one global user belonging to multiple tenants. ABP’s default tenant-local user behavior must **not** be assumed to satisfy Kepnora.

Test host context versus tenant context, role grants, session renewal, membership revocation and audit attribution. If host permissions require a separate operator identity provider or client, record the decision before implementation.


## 6. Recommended repository placement

```text
kepnora/
├── backend/
│   └── src/
│       ├── Kepnora.Domain.Shared/
│       ├── Kepnora.Domain/
│       ├── Kepnora.Application.Contracts/
│       ├── Kepnora.Application/
│       ├── Kepnora.EntityFrameworkCore/
│       ├── Kepnora.HttpApi/
│       ├── Kepnora.HttpApi.Host/
│       └── Kepnora.Worker/
├── apps/
│   ├── mobile/                       # Phase 1 customer app
│   └── admin/                        # OPTIONAL / NOT IN MVP
│       ├── src/
│       │   ├── app/
│       │   ├── features/
│       │   ├── components/
│       │   ├── auth/
│       │   ├── api/
│       │   └── design/
│       └── tests/
└── docs/
    └── implementation/
        └── admin-project-structure.md
```

This is a **logical future layout**; no custom admin package should be scaffolded in M0 unless there is a validated operational need.


## 7. Optional admin frontend technology

If a custom portal becomes necessary, choose the frontend technology by ADR. A TypeScript/React web application could share API schema types and design tokens, but do not assume it must use Expo Router or share mobile navigation code. Prefer accessible semantic HTML, a reviewed component library and minimal client-side secrets.

ABP’s available administrative UI (if included in the pinned edition/template) may make a separate frontend unnecessary. Confirm licensing, modules and actual routes rather than asserting they exist.


## 8. Optional frontend feature folders

```text
apps/admin/src/
  app/                 # routing, authenticated shell
  auth/                # operator OIDC, session boundary
  api/                 # generated OpenAPI client + transport
  features/
    system-health/
    tenant-directory/
    provisioning/
    jobs/
    audit-events/
  components/          # shared tables, dialogs, error states
  design/              # tokens and accessibility primitives
  config/              # validated public runtime config
  tests/               # unit, integration, E2E
```

Folders are examples, not an authorization to build all features in Phase 1.


## 9. Backend operations API boundary

Do **not** add unrestricted `admin` actions to ordinary mobile application services. If needed, create explicit internal operations contracts (e.g., `Kepnora.Operations.*`) in Application.Contracts and implement in Application; expose via separately protected HTTP controllers or ABP capabilities.

An internal API must require operator-specific identity, named permission, host-safe access and audit. Merely prefixing a route with `/admin` is not security.


## 10. Host context and tenant data access

ABP host context is useful for tenant registry and infrastructure operations, **not** permission to enumerate private tenant domain tables. A support action that genuinely requires tenant-scoped data must have a narrow purpose, documented authority, explicit tenant selection, scoped lifetime and audit.

Never globally disable `IMultiTenant` filters to make an admin grid work. Do not run unrestricted `SELECT * FROM Assets` across tenants.


## 11. Operator permission design

| Permission (proposed) | Typical capability |
|---|---|
| `Kepnora.Operations.Health.Read` | Read aggregate service health |
| `Kepnora.Operations.Tenants.Read` | View minimal tenant metadata |
| `Kepnora.Operations.Provisioning.Read` | Read provisioning state |
| `Kepnora.Operations.Provisioning.Retry` | Retry idempotent reconciliation |
| `Kepnora.Operations.Jobs.Read` | View safe job summaries |
| `Kepnora.Operations.Jobs.Retry` | Retry approved failed job |
| `Kepnora.Operations.Audit.Read` | View redacted audit metadata |

These are **proposed** names, not existing ABP permission definitions. Avoid generic `Admin.All` grants and require separate approval for destructive actions.


## 12. Operator roles and separation of duties

Suggested future role split: `SupportReadOnly`, `OperationsMaintainer`, `SecurityAuditor`, and `DeploymentOperator`. These are **platform operator roles**, separate from tenant Owner/Member and not part of Phase 1 workspace role schema.

Read-only support must not retry jobs, mutate memberships or view private files. Elevated operations should use just-in-time access and additional approval where practical.


## 13. Authentication for internal operators

Use a separate OIDC client or equivalent reviewed operator authentication flow, MFA, short sessions and audience-restricted tokens. Do not reuse mobile refresh tokens or expose operator scopes to the mobile app.

For a browser-based portal, choose an appropriate secure token/session pattern (often a backend-for-frontend with HttpOnly cookies) after threat modeling; never put long-lived privileged tokens in localStorage.


## 14. Tenant directory — safe metadata

A future directory may show ABP TenantId, display name, creation time, provisioning status, enabled/disabled state and aggregate operational indicators if approved. It must **not** show private asset names, property addresses, receipts or member email lists by default.

Use ABP Tenant Management as the registry; join a chosen `WorkspaceProfiles` table or tenant extension only after that ADR is closed.


## 15. Workspace provisioning diagnostics

Track create request correlation, tenant creation, Owner membership insertion, workspace profile creation and final Ready state. A recoverable partial failure may be retried through a durable reconciler.

Operator action must not create a second tenant on retry. Present the exact reconciliation stage and safe outcome; never claim “fixed” before verifying active Owner membership.


## 16. Member support boundaries

Workspace Owners manage members in the mobile product. Platform operators should not routinely add themselves to customer workspaces or bypass last-owner protections.

If an exceptional recovery workflow is approved, require documented customer authorization, reason code, elevated permission, full audit and the same last-active-Owner invariant. Avoid generic member impersonation.


## 17. Background job operations

Useful future read-only diagnostics: queue/outbox backlog age, retry count, dead-letter count, job type, safe tenant identifier, correlation ID, last error category and next retry time.

Retry must use the **same chosen outbox provider** and handler semantics; do not create a parallel retry queue. Operator actions must be idempotent and bounded.


## 18. Maintenance scheduler support

Support visibility into generated occurrence counts, schedule-key conflicts, scheduler lag and failed rule batches without exposing private maintenance descriptions. A retry should regenerate deterministically with unique `(TenantId, RuleId, ScheduleKey)` protection.

Never manually flip `Overdue` in the database; it is a derived display classification. Persisted lifecycle statuses remain `Open`, `Completed`, `Skipped`, `Cancelled`.


## 19. S3 upload support

Expose aggregate upload-intent failure rates, orphan object counts, expired intents and finalize errors. Do not expose raw private object keys or presigned URLs to broad support roles.

Any cleanup/retry uses existing document authorization, retention rules and idempotent workers. An S3 PUT without successful finalize is not a completed document.


## 20. Expo Push support

Operational metrics may include accepted/rejected dispatch counts, invalid-token counts, receipt error categories and backlog age. Expo acceptance is not final device delivery. Push tokens remain encrypted and must never appear in the portal or logs.

A retry must recheck membership, per-workspace preferences and maintenance occurrence state before dispatch.


## 21. Audit and forensic trail

Record actor global ID, operator principal, action, target tenant, target entity category, correlation ID, timestamp, outcome and justification for elevated actions. Protect logs from casual edits and apply a documented retention policy.

Never log bearer tokens, refresh tokens, S3 signed URLs, raw push tokens, property addresses or receipt contents.


## 22. Privacy and support access policy

Default to **metadata-first support**. A customer-data read requires a specific product/security policy, narrow field-level permissions, recorded reason and ideally time-bound access. Never add unrestricted “view all assets” or “download any document” features just for convenience.

Account deletion/export requests are separate privacy workflows and require legal/product policy; this document does not invent them.


## 23. Admin UI information architecture

Proposed navigation **only if** a portal is approved:

```text
Overview / System Health
Tenant Directory
Provisioning Exceptions
Jobs & Outbox
Audit Events
Access & Security
```

Keep destructive actions outside overview cards. Use confirmations and clear impact descriptions for retries/reconciliation. Never expose unimplemented buttons as if functional.


## 24. Operational UX states

Every screen supports loading, empty, permission-denied, dependency-unavailable and partial-data states. Show UTC timestamps with optional local display, correlation IDs for support and safe redacted errors.

Do not display success before the server confirms persistence. For long-running reconciliation show `Queued`, `Running`, `Succeeded`, `Failed` based on durable state.


## 25. Pagination and data minimization

Tenant directory, job failures and audit events require bounded cursor pagination and allowlisted filters. No unbounded export of global user or tenant data. Aggregate counters should use efficient database queries/metrics rather than loading full domain entities.

Limit search to approved metadata and avoid full-text search over private documents or properties.


## 26. Proposed internal API inventory

| Operation | Scope | Permission |
|---|---|---|
| `GetSystemHealth` | Host | Health.Read |
| `ListTenantSummaries` | Host | Tenants.Read |
| `GetProvisioningStatus` | Host | Provisioning.Read |
| `RetryProvisioning` | Host/target tenant | Provisioning.Retry |
| `ListJobFailures` | Host safe metadata | Jobs.Read |
| `RetryFailedJob` | Explicit job | Jobs.Retry |
| `ListAuditEvents` | Redacted metadata | Audit.Read |

No concrete routes are promised. Derive actual endpoints from the pinned ABP module or generated OpenAPI. Avoid adding APIs that duplicate existing secure ABP administration features.


## 27. Example safe tenant summary DTO

```csharp
public sealed record TenantOperationsSummaryDto(
    Guid TenantId,
    string DisplayName,
    string ProvisioningStatus,
    DateTimeOffset CreatedAt,
    bool IsActive);
```

`ProvisioningStatus` and `IsActive` are conceptual: map them to actual approved workflow/ABP fields. Do not add columns just because a DTO example includes them.


## 28. Example job retry request

```json
{
  "jobId": "8b0df8e3-995a-4e0d-a0b2-651e8355b2a7",
  "expectedState": "DeadLetter",
  "reason": "Reviewed transient provider outage"
}
```

Before retry: authorize operator, verify state/version, validate bounded retry policy, write audit, enqueue via the **existing** job provider. Return accepted/queued status only after durable write. Do not replay side effects outside idempotent handlers.


## 29. Security threat model

| Threat | Required control |
|---|---|
| Operator token theft | MFA, audience restrictions, short sessions |
| Host privilege confused with tenant membership | Explicit operator vs tenant authorization |
| Cross-tenant data leakage | Minimal metadata, scoped tenant operations |
| CSRF on browser mutations | BFF/cookie CSRF protection or approved token design |
| Mass data export | No bulk private export by default |
| Job retry duplication | Idempotent handlers, state/version guard |
| Audit tampering | Protected append-oriented logs and access review |
| Sensitive log fields | Structured redaction and tests |
| Deployment credential misuse | Separate CI/CD IAM role, least privilege |


## 30. Infrastructure and network placement

If a portal is deployed, protect it with operator authentication, HTTPS, WAF/rate limiting as appropriate, and ideally a restricted access path. A private-only portal or existing ABP UI may be preferable to a public admin subdomain.

Use separate runtime IAM and DB roles for admin operations. Do not grant browser clients AWS credentials or direct RDS connectivity.


## 31. Monitoring without a custom portal

Phase 1 operators can use AWS CloudWatch, RDS monitoring, ECS service status, structured application logs, alerts and approved ABP administration surfaces. This meets many operational needs without adding a new frontend attack surface.

Provide runbooks and role-limited CLI/automation for rare interventions instead of building an always-on UI prematurely.


## 32. Deployment and environment separation

Development/staging/production operator roles, secrets and datasets must be isolated. Staging must not expose production tenant metadata or push tokens. Production access is audited and time-bounded.

Deploy any future portal as its own artifact, not bundled into the mobile app. Pin its OpenAPI client and release compatibility with backend.


## 33. Testing strategy

Test host authorization with real ABP configuration and PostgreSQL. Verify operator role isolation from tenant Owner/Member, negative access tests, redaction, pagination, audit and safe retry.

Use synthetic tenants in staging. Do not use production customer documents or raw push tokens as fixtures.


## 34. Test catalog — permissions

| ID | Scenario | Expected |
|---|---|---|
| ADM-001 | Workspace Owner calls host ops | Denied |
| ADM-002 | Workspace Member calls host ops | Denied |
| ADM-003 | Read-only operator views health | Allowed |
| ADM-004 | Read-only operator retries job | Denied |
| ADM-005 | Operator views raw S3 key | Denied |
| ADM-006 | Operator views raw push token | Denied |
| ADM-007 | Operator requests foreign asset | No generic bypass |
| ADM-008 | Host identity missing MFA | Denied by policy |
| ADM-009 | Expired operator token | Denied |
| ADM-010 | Tenant Owner X / Member Y | No platform escalation |
| ADM-011 | Unauthorized bulk export | Denied |
| ADM-012 | Audit access outside role | Denied |
| ADM-013 | Permission change | Next request re-evaluated |
| ADM-014 | Direct API bypassing UI | Denied |
| ADM-015 | Operator tries tenant filter disable | No unrestricted domain access |


## 35. Test catalog — workflows

| ID | Scenario | Expected |
|---|---|---|
| ADM-016 | Provisioning retry after partial failure | No duplicate tenant |
| ADM-017 | Owner membership missing | Not marked Ready |
| ADM-018 | Two operators retry same job | One logical effect |
| ADM-019 | Retry dead-letter with stale state | Conflict |
| ADM-020 | Scheduler replays occurrence | Unique schedule key |
| ADM-021 | Completed occurrence receives reminder retry | Suppressed |
| ADM-022 | Removed member receives queued reminder | Suppressed |
| ADM-023 | S3 orphan cleanup | Retention policy followed |
| ADM-024 | Audit record for privileged retry | Actor/reason/target captured |
| ADM-025 | Job error includes private content | Redacted |
| ADM-026 | Large tenant list | Bounded pagination |
| ADM-027 | Support operation interrupted | Recoverable durable state |
| ADM-028 | Staging operator queries prod | Denied |
| ADM-029 | Backend API version changes | Client contract gate |
| ADM-030 | Admin UI not deployed | Phase 1 product still works |


## 36. Implementation milestone mapping

| Milestone | Admin/operations responsibility |
|---|---|
| M0 | ABP edition, host identity, global-user POC, permission model |
| M1 | Tenant registry, provisioning observability, last Owner guard |
| M2–M3 | Asset/property logs and cross-tenant regression |
| M4 | Scheduler/outbox metrics and safe retry runbooks |
| M5 | S3 upload/finalize diagnostics |
| M6 | Push failure/receipt metrics and token redaction |
| M7 | Product Settings in mobile, not custom admin UI |
| M8 | Incident runbooks, security audit, restore drill |

**Optional future milestone:** authorize a dedicated admin UI only after demonstrated operator workload and approved requirements.


## 37. Open decisions

1. Which ABP edition/modules and administrative UI are available after M0?
2. Is a dedicated operator identity/client required?
3. What host permissions exist and how are they assigned?
4. Are support actions permitted to access customer domain data at all?
5. What audit retention and justification policy applies?
6. Which provisioning state store/reconciler is authoritative?
7. Which outbox provider is selected, and how is retry exposed?
8. What incident procedures require an operator UI versus runbooks?
9. What is the approved internal network access model?
10. Which frontend framework is justified if a portal is approved?
11. What are privacy/export/deletion obligations and data retention?
12. What security review is required before privileged mutations?


## 38. Definition of Done — Phase 1

- [ ] No custom admin portal is required to launch Phase 1.
- [ ] ABP Tenant Management remains the authoritative tenant registry.
- [ ] Host operator authority is distinct from workspace Owner/Member.
- [ ] M0 proves global identity and host/tenant permission boundaries.
- [ ] Operational metrics and alerts cover API, RDS, outbox, S3 and push.
- [ ] Privileged interventions have least-privilege runbooks and audit.
- [ ] No raw tokens, signed URLs or private document contents appear in logs.
- [ ] Worker retries are bounded, durable and idempotent.
- [ ] Customer data is never broadly accessible merely through host context.
- [ ] ADM-001 through ADM-030 have test evidence or documented deferral.
- [ ] Any future portal has an explicit scope/ADR before implementation.


## 39. Definition of Done — optional portal

- [ ] Approved use cases demonstrate value over ABP UI/CloudWatch.
- [ ] Operator identity, MFA and named permissions are implemented.
- [ ] Internal API has generated OpenAPI and negative security tests.
- [ ] UI supports redacted metadata, pagination and accessible workflows.
- [ ] Privileged actions require reason, confirmation, concurrency checks and audit.
- [ ] Staging/production isolation and CI security gates pass.
- [ ] No direct browser-to-database or browser-to-S3 privileged access.
- [ ] No unrestricted cross-tenant private data search.


## 40. Final architecture principle

**Keep product administration close to the workspace, and platform operations close to the platform.** Use ABP capabilities and infrastructure tooling before building a separate portal. Any future admin UI must make cross-tenant privilege escalation harder—not easier.

