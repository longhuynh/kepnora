# Kepnora — Future Features

**Status:** Exploration only; no Phase 2/3 commitment or implementation approval.

## Phase 1 boundary

Phase 1 is **Own & Maintain**: global sign-in/workspaces, optional properties, assets and custom asset types, maintenance/actual mileage, private documents, Expo push, dashboard/search/settings.

## Phase 2 — Find Services

Explore provider discovery inside Kepnora, provider profiles, location-aware results with explicit consent, quality and verification, contact workflows, reporting and moderation. A possible `IProviderDiscoveryService` abstraction could isolate provider data sources. This phase **does not** promise appointment availability, booking or payments. Validate provider data licensing, user consent and abuse prevention before implementation.

## Phase 3 — Book & Pay

Explore service appointment availability, booking, rescheduling, cancellation, checkout, refunds, provider payouts, reconciliation, disputes and regulatory requirements. These require explicit product decisions, operational support, security review and payment-provider selection.

## Other candidates, not approved

- Advanced reports, exports and multi-property analytics.
- Warranty expiration dates (requires a reviewed database/API extension).
- Integrations with mileage/telematics sources (must not infer mileage in MVP).
- Collaboration and notifications beyond current workspace membership model.
- Paid subscriptions and entitlements (pricing is currently a proposal).
- Dedicated admin portal only when ABP/infrastructure operations prove insufficient.

## Prioritization gates

Before advancing any candidate: validate customer demand, retention, data quality, privacy, unit economics, architectural fit and a testable acceptance contract. Record the approved scope in `product/roadmap.md`, `product/feature-matrix.md` and an ADR when appropriate.
