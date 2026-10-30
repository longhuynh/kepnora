# Kepnora — M1 Authentication, Workspace Provisioning & Authorization

**Version:** 1.0 · **Status:** Implementation handoff, pending verification against the completed M0 repository · **Dependency:** M0/G0 passed · **Exit gate:** G1

## 1. Source of truth

Read and reconcile these repository files before editing code:

1. `docs/mvp-build-order.md` — M1-01 through M1-08 and G1 acceptance criteria.
2. `docs/architecture/authentication-authorization.md` — global identity, membership, permissions, revocation, session security.
3. `docs/architecture/tenant-isolation.md` — ABP tenant boundaries and membership enforcement.
4. `docs/features/authentication-onboarding.md` — mobile authentication, first-run and optional onboarding.
5. `docs/database-overview.md` (or the actual approved path) — membership schema and constraints.
6. `docs/api/api-contracts-master.md` — approved API/DTO conventions.
7. `docs/implementation/mobile-project-structure.md` and `docs/implementation/backend-project-structure.md` — repository structure.

**Preflight:** Confirm these files exist and identify the actual M0-selected ABP/Identity/OIDC architecture. The files may contain draft proposals; the tested M0 implementation and approved ADRs take precedence where explicitly superseded. Report discrepancies before changing the design.

## 2. Goal and boundaries

Deliver a secure, working end-to-end path: **sign up / sign in → verified global identity → personal workspace provisioning → workspace selection → authorized workspace requests → sign out**. A returning user must resume the correct session/workspace. A global user can belong to multiple workspaces, with a different role in each.

**In scope:** authentication callback and session lifecycle; workspace listing, creation, selection and switching; workspace membership and Owner/Member permissions; tenant-aware API authorization; audit; first-run onboarding entry and ability to skip property creation; automated security tests; Expo mobile screens and states.

**Out of scope:** full Property CRUD (M2), My Items (M3), Maintenance, Documents, Billing/Subscription, Find Services, enterprise SSO, complex custom role editors. Optional property/item prompts may route to future modules or be deferred behind feature availability; do not fake persisted data.

## 3. Hard invariants

- One workspace maps to one authoritative **ABP Tenant**. Never create a parallel custom tenant registry.
- One **global user identity** may be Owner in workspace X and Member in workspace Y.
- Every protected workspace request checks **active membership plus operation permission**, independently of `ICurrentTenant`.
- Workspace IDs from route/header/body/client are untrusted until membership is verified.
- A user's Owner role in X never grants Owner rights in Y.
- No access to another tenant's data through route guessing, spoofed headers, stale tokens or cached mobile state.
- Only **Owner** and **Member** are MVP workspace roles. Last active Owner cannot be removed or demoted.
- Provisioning and invitation/membership operations must be retry-safe; partial failures must be recoverable.
- Do not store tokens in AsyncStorage or logs; use the M0-approved secure session mechanism (Expo SecureStore where applicable).
- Property creation is optional; onboarding must finish without a Property or Item.

## 4. Implementation tasks

### M1-01 — Mobile authentication and session lifecycle

Implement the M0-approved OIDC Authorization Code + PKCE / identity integration in the Expo app: sign-up/sign-in entry, callback, email-verification handling, session restore, access-token refresh, sign-out and expired/revoked session recovery. Store secrets only using approved secure storage. Handle loading, offline, cancellation, verification-pending and failure states. Avoid duplicate callback processing.

### M1-02 — Workspace list, select and switch

Provide the authorized workspace list for the current global subject, including role and membership status. Support zero/one/multiple-workspace routing and explicit switching. On switch, clear or partition TanStack Query caches and any persisted state by workspace; cancel old in-flight requests and prevent stale responses from painting another workspace's UI. Restore a previous selection only after revalidating membership.

### M1-03 — Idempotent workspace provisioning

After a verified first login, create exactly one personal workspace using the **approved ABP Tenant Management integration**. Do not accept a client-chosen tenant ID. Use a server-side idempotency strategy or uniqueness invariant and handle concurrent retries. Do not treat tenant creation alone as completed provisioning.

### M1-04 — Membership and profile persistence

Persist `(TenantId, GlobalUserId)` membership with approved role/status and workspace profile metadata. Add reviewed EF Core migration(s) and enforce uniqueness and valid references according to the M0-approved identity model. Handle tenant-created/member-not-created partial failures with an explicit reconciliation/retry path; document transaction boundaries.

### M1-05 — Owner / Member authorization

Implement a documented permission matrix. At minimum: Owner can manage workspace membership; Member cannot. Both can access allowed regular workspace functions, subject to module-specific permissions. Protect against removing/demoting the final active Owner using a concurrency-safe server-side operation. Do not implement unapproved role types.

### M1-06 — ABP API authorization boundary

Implement reusable membership-aware authorization for Application Services, validated tenant context, and tenant-scoped resource lookups. Distinguish `401` (not authenticated) from `403` (not authorized); conceal resource existence where the approved API policy requires `404`. Ensure revoked/inactive memberships are denied within the approved revocation bound. Never rely solely on client hiding buttons.

### M1-07 — Audit, contracts and tests

Record security-relevant events without tokens/secrets: sign-in outcome where available, workspace provisioning, switch where server-relevant, membership changes and authorization denials. Update OpenAPI and generated TypeScript clients from actual endpoints; do not invent routes. Add PostgreSQL-backed integration tests and mobile session/cache tests.

### M1-08 — First-run onboarding

Implement Welcome / Sign In / Verify Email / Workspace Setup / Workspace Picker / introductory onboarding and Dashboard empty-state routing as applicable to the approved mobile screen scope. Make **Add Property** and **Add Item** optional, with visible Skip actions; do not build M2/M3 CRUD as part of M1. Returning verified users with an existing workspace must not repeat first-run setup.

## 5. Required test scenarios

| Scenario | Expected result |
|---|---|
| New verified user retries provisioning or two requests race | Exactly one personal workspace and one valid Owner membership |
| User A is Owner of X and Member of Y | Can access both; Owner-only actions allowed only in X |
| User C has no membership in X | All X protected endpoints deny access |
| User sends a spoofed tenant ID/header | Membership check rejects unauthorized tenant |
| Member tries to invite/remove another member | Forbidden |
| Owner tries to remove/demote last active Owner | Rejected transactionally, including concurrent attempts |
| Membership revoked while app remains signed in | Next protected request denied according to approved revocation policy |
| Switch X → Y while X API request is in flight | No X data appears in Y; cache is partitioned/cleared |
| Token expires / refresh fails / sign-out | Secure session cleared; protected screen inaccessible |
| App killed during provisioning then reopened | Safe retry/recovery; no duplicate tenant or orphan membership |
| User skips property and item prompts | Onboarding completes with zero properties and zero items |
| Unverified user attempts provisioning | Blocked until verification, per approved policy |

Use seeded fixtures: A (Owner X, Member Y), B (Owner Y, no X membership), C (no X/Y membership). Test real PostgreSQL and actual ABP filters, not only mocked repositories.

## 6. G1 acceptance / definition of done

- **AC-M1-01:** New registered user gets exactly one personal workspace after retry.
- **AC-M1-02:** User switches between two authorized workspaces without token or cache bleed.
- **AC-M1-03:** Member cannot manage workspace members; Owner can.
- **AC-M1-04:** Removing the last active Owner is rejected transactionally.
- **AC-M1-05:** Inactive member cannot read tenant assets (use a protected test resource or existing M0 fixture; do not build M3 just to satisfy this).
- **AC-M1-06:** No property required to complete onboarding.

Evidence required: successful builds; reviewed migration IDs and DB constraints; passing integration/security tests; OpenAPI snapshot and generated mobile client; screenshots/demo for auth and workspace flow; logs/correlation IDs without secrets; exact commit SHA; known issues and ADR updates. **Do not mark G1 complete solely because UI works.**

## 7. Execution instructions for coding agent

1. Inspect repository and M0 completion evidence; identify the actual implemented identity model, ABP version, tenant management APIs and security decisions.
2. Read all source-of-truth documents. Produce a gap/conflict list against the M0 code.
3. Present a task-by-task M1 plan with file paths, migration changes, security tests and demo steps.
4. **Wait for approval before changing code.**
5. Implement M1-01 through M1-08 incrementally, building and testing after each task.
6. Report measured evidence and unresolved risks; request approval before declaring G1 passed or starting M2.

**Important:** This handoff does not assert that M0 is actually validated. Verify its test evidence first. All routes, schemas and auth-provider specifics must be derived from the approved specs and the actual repository.
