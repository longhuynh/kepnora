# Kepnora Documentation Folder Structure

This file describes the **packaged** documentation tree. Files in this ZIP are design/specification artifacts, not evidence that application code is implemented.

## Directories

- `architecture/`: tenant isolation, background processing, storage, scheduling, notifications, architecture decisions and integration patterns.
- `features/`: user-facing feature specifications and acceptance criteria.
- `api/`: proposed contracts; generated OpenAPI from running backend is authoritative.
- `implementation/`: backend, mobile and deferred admin project structures.
- `ui/`: screen specifications, design system, tokens reference and mockup workflow.
- `product/`: MVP scope, roadmap, pricing proposals and feature matrix.
- `testing/`: test strategy.
- `deployment/`: deployment and release guide.

## Product invariants

One workspace maps to one ABP Tenant; active membership/permissions are mandatory; properties are optional for assets; only actual mileage is recorded; maintenance lifecycle states are `Open`, `Completed`, `Skipped`, `Cancelled`; S3 objects are private; notifications are best effort.

## Navigation

Start with `README.md`, `kepnora-project-context.md`, `product/mvp-scope.md`, `architecture-overview.md`, `database-overview.md`, `mvp-build-order.md`, and `api/api-contracts-master.md`. Follow individual feature specs when implementing.

## Important

Older specifications may refer to `docs/architecture/database-overview.md`; the canonical packaged file is `docs/database-overview.md`. Avoid creating two independently edited database specs.
