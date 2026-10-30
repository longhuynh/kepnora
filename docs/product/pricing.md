# Kepnora — Pricing, Packaging & Subscription Specification

**Canonical path:** `docs/product/pricing.md`  
**Version:** 1.2 — proposed product decision  
**Updated:** 2026-10-29  
**Status:** Founder direction captured; capacity numbers and billing launch require final approval  
**Audience:** Product, backend, mobile, QA, design, operations

> **Decision summary:** Target two paid monthly tiers, **Plus $4.99/month or $49.90/year** and **Family $9.99/month or $99.90/year**, differentiated primarily by maximum Properties and My Items **per Workspace**. Preserve a useful Free tier. Phase 1 remains an unpaid Own & Maintain MVP; target subscription implementation for a later commercial release, tentatively Phase 2. Phase 2's existing product scope is Find Services; billing is a separate workstream and not automatically included.

---

## 1. Purpose and authority

This document supersedes the earlier exploratory pricing table and $2.99/$4.99/$7.99/$9.99 research list. It records the selected price points and a concrete **proposed** entitlement design. It is not a statement that billing is implemented or approved for public sale. Where limits remain proposals, they are marked accordingly.

Related specifications: `docs/product/mvp-scope.md`, `docs/product/roadmap.md`, `docs/mvp-build-order.md`, `docs/architecture/tenant-isolation.md`, `docs/api/api-contracts-master.md`, `docs/features/settings-members.md`.

## 2. Confirmed direction versus open decisions

| Topic | Direction | Status |
|---|---|---|
| Plus monthly price | USD $4.99 / month | Selected product target; not live |
| Family monthly price | USD $9.99 / month | Selected product target; not live |
| Plus annual price | USD $49.90 / year | Selected product target; not live |
| Family annual price | USD $99.90 / year | Selected product target; not live |
| Annual discount | Two months free compared with 12 monthly payments (approximately 16.7%) | Selected marketing direction |
| Free tier | Retain a useful free option | Recommended |
| Primary capacity metric | Properties and My Items per Workspace | Selected design direction |
| Allocation | One shared item allowance across all Properties and standalone Items | Selected design direction |
| Phase 1 | Free beta, no checkout or paid entitlement requirement | Existing MVP scope |
| Paid launch | Consider alongside Phase 2, after billing readiness | Target only; not approved milestone |
| Free/Plus/Family caps | 1/3/10 Properties and 10/50/200 Items | Proposed values requiring approval |
| Annual plans | Plus $49.90/year; Family $99.90/year, paid upfront | Selected targets |
| Trials, introductory offers, taxes, regions | Not yet decided | Open |
| Member caps and storage caps | Not yet decided | Open |
| Store payment implementation | Must evaluate platform rules and provider | Open |

## 3. Consumer-facing plan matrix — draft

| Feature | Free | Plus | Family |
|---|---:|---:|---:|
| Monthly price (USD) | $0 | **$4.99/month** | **$9.99/month** |
| Annual price (USD, prepaid) | $0 | **$49.90/year** | **$99.90/year** |
| Savings versus 12 monthly payments | — | **$9.98/year (~16.7%)** | **$19.98/year (~16.7%)** |
| Maximum Properties per Workspace | **1** proposed | **3** proposed | **10** proposed |
| Maximum My Items per Workspace | **10** proposed | **50** proposed | **200** proposed |
| Standalone Items (no Property) | Yes; count toward Item limit | Yes | Yes |
| Maintenance rules and records | Included | Included | Included |
| Manual odometer/mileage readings | Included | Included | Included |
| Basic maintenance reminders | Included | Included | Included |
| Documents and photos | Included; fair-use controls TBD | Included; capacity TBD | Included; capacity TBD |
| Workspace collaboration | Core role model | Core role model | Core role model |
| Find Services | Subject to Phase 2 availability | Same | Same |
| Book & Pay | Not included in Phase 1 or 2 | Same | Same |

**Important:** Numeric capacity caps above are recommended defaults, **not finalized product commitments**. Do not publish or enforce them in production until approved. Do not promise unlimited storage, members, documents or reminders.

### 3.1 Why shared Item capacity

A Plus Workspace with three Properties and 50 My Items can have 25 Items at House A, 15 at House B, and 10 at House C. A car with `PropertyId = NULL` also consumes one of those 50 slots. The total is **50**, not 50 per Property. A Workspace with zero Properties can still contain standalone Items up to its Item allowance.

### 3.2 Naming conventions

- Mobile/UI: **My Items**, **Item**, **Item Category**.
- Backend, API, persistence: `Asset`, `AssetId`, `AssetType` remain unchanged.
- A `Property` is a physical location/entity and is not itself counted as an Item.
- A `Workspace` maps to one ABP tenant; one Workspace may own multiple Properties.

## 4. Billing ownership and tenancy

1. Subscription and capacity belong to a **Workspace**, not an individual member or Property.
2. A global user can belong to multiple Workspaces, each with its own effective plan.
3. Only an authorized Workspace Owner may initiate or manage paid billing, subject to final permission design.
4. Workspace membership authorization is required on every request; `ICurrentTenant` alone is not authorization.
5. Workspace switching must refresh effective entitlements and clear/partition client caches.
6. Never share a paid plan or its limits across unrelated tenants merely because the same user owns both.
7. The existing ABP Tenant Management registry remains authoritative; do not introduce a competing Tenants table.

## 5. Entitlement definitions

### 5.1 Property count

Count active, non-deleted Properties belonging to the Workspace. Whether archived Properties consume capacity must be finalized before launch. Recommendation: archived Properties **do not** consume creation capacity only if archive makes them read-only and does not permit unlimited active usage through quick unarchive.

### 5.2 Item count

Count active, non-deleted Assets in the Workspace, including Assets without a Property. Recommendation: archived Items do not count toward active capacity; restoring an archived Item must pass the same capacity check as creating a new Item.

### 5.3 No per-house cap

Do not introduce `maxItemsPerProperty` for the initial subscription design. Use `maxItemsPerWorkspace` and `maxPropertiesPerWorkspace` to avoid ambiguity, especially for cars and standalone devices.

### 5.4 Historical records

Maintenance history, mileage history and documents are not erased or rewritten because of a plan change. Completed, Skipped and Cancelled maintenance occurrences remain auditable. Existing file permissions and tenant isolation continue to apply.

## 6. Capacity enforcement behavior

- Backend is the authority; mobile labels and disabled buttons are UX only.
- For create or restore operations, verify current effective entitlement and count inside a concurrency-safe workflow.
- Ensure simultaneous requests cannot both pass a last-slot check and exceed the cap. Choose a transactional locking/admission approach and test it with real PostgreSQL.
- Reject over-limit requests with a stable machine-readable business error such as `PLAN_LIMIT_REACHED`, plus `resource`, `limit`, `currentCount`, `planCode` and an upgrade action hint where appropriate.
- Do not return a misleading HTTP 401/403 for capacity errors; use the agreed API business-error contract (e.g. 409 or 422, to be finalized in the API spec).
- Do not count failed uploads or incomplete Item creation as successful Items.
- Deleting an Item should not silently delete maintenance history; follow the archive/retention specification.
- Rate-limit and audit billing-sensitive mutations.

### Example entitlement snapshot (illustrative, not a committed API)

```json
{
  "workspaceId": "workspace-id",
  "planCode": "plus",
  "billingStatus": "active",
  "limits": {
    "maxProperties": 3,
    "maxItems": 50
  },
  "usage": {
    "properties": 2,
    "items": 34
  }
}
```

Actual DTOs and routes must be approved in `docs/api/api-contracts-master.md` and generated from running backend OpenAPI.

## 7. Upgrades and downgrades

### Upgrade

- Entitlements change only after verified store/provider billing state or a documented administrative grant.
- Never trust a mobile-provided plan code or success redirect as proof of payment.
- Refresh entitlement caches, show new limits, and allow creation within the updated cap.

### Downgrade

- **Never delete existing Properties, Items, documents, maintenance records or mileage readings automatically.**
- If current usage exceeds the new cap, retain access to existing data and allow view, edit, export (if supported), archive and delete.
- Block creating/restoring additional counted resources until usage is below the new cap or the Workspace upgrades.
- Define whether the new plan applies immediately or at period end based on the selected billing provider's verified state.
- Display clear explanation: “You have 34 of 10 Items on Free. Your existing Items are safe, but you can't add more until you reduce usage or upgrade.”

### Failed payment, refund, cancellation

Model billing state independently from entitlements. Grace period, retry window, refund handling, cancellation effective date, and account recovery are open decisions. Never revoke authentication or erase customer data merely because a payment failed.

## 8. User experience and screen requirements

### Settings

When paid plans are available, add **Settings → Plans & Billing** (not an MVP requirement). Show current plan, Property/Item usage, billing management, and upgrade options. Avoid cluttering Home Dashboard with paywalls.

### Plans & Billing

- Compare Free, Plus and Family with monthly prices and approved caps.
- Clearly label whether prices include taxes and whether subscription auto-renews, based on store requirements.
- Show active plan and next billing date only when verified.
- Use “Upgrade to Plus”, “Upgrade to Family” and “Manage Subscription” actions where supported.
- Present restore purchases and account/workspace association behavior if the selected platform requires it.

### Add Property / Add Item

- Display remaining capacity unobtrusively near the creation action when useful.
- If limit reached, explain which Workspace is limited and offer plan comparison.
- Do not interrupt maintenance completion or mileage logging merely because the Item count is at its limit.

### Multi-workspace

A user with Free Workspace A and Family Workspace B must see each Workspace's correct plan, limits and billing owner. Switching Workspaces must not show stale usage from another tenant.

## 9. Billing architecture — future workstream

**Not in Phase 1.** Introduce billing only after explicit architectural review.

Recommended concepts:

- `PlanCatalog`: versioned plan definitions and effective entitlements.
- `WorkspaceSubscription`: provider identifiers, current state, period boundaries, plan reference and audit metadata; scoped to Workspace.
- `BillingEventInbox`: provider event id, signature verification result, processing status and deduplication.
- `EntitlementService`: effective plan resolution, counts and capacity admission checks.
- `BillingProviderAdapter`: isolates App Store / Google Play / other approved providers.
- Admin grants/overrides: time-bounded, audited, explicitly authorized, never a silent client override.

These are conceptual modules, **not approved tables, API routes or ABP modules**. Database naming, tenant isolation, provider reconciliation and transactional guarantees require separate design and migrations.

### Billing event invariants

1. Verify webhook or server-notification authenticity and account association.
2. Handle duplicate, delayed and out-of-order provider events idempotently.
3. Never assume app foreground state or client callback determines payment success.
4. Keep plan history and effective entitlement transitions auditable.
5. Protect payment/customer identifiers in logs; do not store card details in Kepnora.
6. Reconcile provider state after outages and missed events.
7. Document subscription transfer, restore, cancellation and refunds before release.

## 10. App Store / Google Play policy gate

Digital subscriptions or capacity unlocks inside iOS/Android apps may be subject to platform in-app purchase rules. Choose a compliant purchase and account-management flow after reviewing the applicable current rules for target regions. Do not assume an external checkout link or “Buy Me a Coffee” button is permitted in every context. Donation/tipping UX is separate from subscriptions and also needs policy review.

No live payment, external checkout or donation action should be shipped without compliance review.

## 11. Phase alignment

| Phase | Product focus | Pricing delivery |
|---|---|---|
| Phase 1 — Own & Maintain | Build and validate core product, M0–M8 | Free beta; measure usage; no paid checkout dependency |
| Phase 2 — Find Services | Discover providers inside Kepnora | **Target window** to evaluate launching Free/Plus/Family subscriptions as a separate workstream |
| Phase 3 — Book & Pay | Booking, scheduling, transaction payment | Potential transaction economics distinct from Workspace subscription |

Subscription readiness may occur before or after Find Services depending on retention, engineering effort and store compliance. Phase 2 is **not** a guarantee that billing will ship.

## 12. Measurement and validation

Track aggregated, privacy-conscious metrics: active Workspaces, Properties/Items per Workspace, number of users near limits, maintenance retention, documents stored, reminder activity, storage costs, paid conversion, churn, refunds and support contacts. Do not use personal household data or document contents for pricing analytics.

Validate proposed 1/3/10 Property and 10/50/200 Item caps against real beta distributions before public launch. Check whether the Free tier is useful for a user with **one car and zero Properties**.

## 13. Acceptance criteria for paid launch

- [ ] Founder approves public plan names, exact caps, currencies and effective date.
- [ ] Product/roadmap/MVP docs distinguish paid workstream from Phase 1.
- [ ] API spec includes entitlement snapshot and stable capacity errors.
- [ ] Workspace-scoped authorization and capacity enforcement pass concurrent PostgreSQL tests.
- [ ] Mobile shows correct plan after login, workspace switch, upgrade and downgrade.
- [ ] Billing provider verification, idempotency, reconciliation and recovery pass E2E tests.
- [ ] Downgrade never deletes user data; over-limit behavior is documented and tested.
- [ ] App Store/Google Play purchase rules, tax displays and legal disclosures reviewed.
- [ ] Customer support procedures cover refunds, restores, billing owner transfer and failed payments.
- [ ] Privacy, observability and rollout/rollback plans reviewed.

## 14. Decisions still required

1. Confirm **Free 1 Property / 10 Items**, **Plus 3 / 50**, **Family 10 / 200**, or change any number.
2. Decide whether Family has extra members or storage, and define exact quotas.
3. Decide whether archived Properties and Items count, and specify restoration behavior.
4. Decide whether multiple Workspaces can share a payer or receive bundle discounts.
5. Choose iOS/Android billing provider architecture and supported regions.
6. Annual price targets selected; finalize store products, auto-renewal disclosures, annual cancellation/refund handling, free trials, introductory offers and grace periods.
7. Define cancellation, refunds, restore purchases, subscription transfer and data retention.
8. Decide the actual subscription launch gate rather than equating it automatically with Phase 2.

## 15. Implementation guidance for coding agents

Do not implement payment flows or database schema based on this proposal without explicit approval. In Phase 1, keep business logic independent of client plan labels, preserve Workspace/tenant isolation, and consider an injectable entitlement interface only if it adds minimal complexity. Do not hard-code proposed limits into M0–M8 MVP endpoints. Future billing must preserve `Asset.PropertyId = NULL`, immutable asset type versions, the maintenance occurrence lifecycle (`Open`, `Completed`, `Skipped`, `Cancelled`), actual-only mileage readings and private S3 access controls.

---

**Change log — v1.1:** Replaces exploratory price-point list with targeted **$4.99 Plus** and **$9.99 Family** monthly plans; proposes Free/Plus/Family capacity tiers; establishes **per-Workspace** Property and Item accounting; clarifies Phase 2 timing, downgrade safety, billing ownership, backend enforcement and approval gates.


## 17. Annual billing decision (v1.2)

- **Plus:** $4.99 billed monthly or **$49.90 billed annually upfront**. Twelve monthly payments would total $59.88, so annual saves $9.98 (approximately 16.7%).
- **Family:** $9.99 billed monthly or **$99.90 billed annually upfront**. Twelve monthly payments would total $119.88, so annual saves $19.98 (approximately 16.7%).
- Marketing copy may say **“Save 2 months with annual billing”** because each annual price equals ten times its corresponding monthly price. The effective monthly equivalents are approximately $4.16 and $8.33, respectively.
- Annual subscriptions use the **same Property and My Item limits** as the corresponding monthly tier; only billing cadence and price differ.
- Mobile Plans & Billing should offer a Monthly / Yearly selector, clearly show the **full amount charged upfront**, and disclose auto-renewal, taxes where applicable, cancellation terms, and applicable app-store conditions before purchase.
- Plan codes and billing cadence must be separate fields, e.g. `planCode=PLUS`, `billingInterval=YEAR`, not separate capacity policies.
- Billing interval changes, prorations, refunds, grace periods, failed renewals, restore purchases, and entitlement reconciliation remain implementation decisions pending store/provider integration. Do not invent behavior.
- All prices are **USD target prices, not live store listings**. App Store / Google Play localization and final approved price points must be verified before launch.
