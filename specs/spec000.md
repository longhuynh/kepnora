# Kepnora — M0 Foundation Implementation

You are a Senior .NET Software Architect and Backend Engineer working on the **Kepnora** project.

Your task is to review the existing project specifications and implement **Milestone M0 — Foundation**.

## 1. Read the Project Specifications First

Before writing any code, carefully read the following Markdown files in order:

1. `docs/kepnora-project-context.md`
2. `docs/architecture-overview.md`
3. `docs/mvp-build-order.md`
4. `docs/architecture/tenant-isolation.md`
5. `docs/architecture/database-overview.md`
6. `docs/implementation/backend-project-structure.md`

Also review these supporting files when relevant:

- `docs/features/authentication-onboarding.md`
- `docs/api/api-contracts-master.md`
- `docs/implementation/mobile-project-structure.md`

**Important:** Treat these documents as the source of truth. Do not invent requirements or make architectural changes without identifying and explaining them first.

If any document is missing, incomplete, or conflicts with another document, report the issue before proceeding.

## 2. Technology Stack

Follow the specifications, including the planned technologies:

- .NET 10
- ASP.NET Core
- ABP Framework
- Modular Monolith Architecture
- Entity Framework Core
- PostgreSQL
- React Native + Expo + TypeScript
- REST APIs

Do not assume ABP compatibility with .NET 10. Verify it through a working proof of concept.

## 3. M0 Implementation Tasks

### Step 1 — Architecture Review

Review the proposed architecture, module boundaries, project dependencies, and database design.

Identify technical risks, missing requirements, and architectural inconsistencies.

### Step 2 — ABP Compatibility POC

Create a minimal working ABP application targeting .NET 10.

Verify:

- Application startup
- Dependency injection
- Module initialization
- PostgreSQL connectivity
- EF Core migrations
- Authentication integration

Document any compatibility issues and their solutions.

### Step 3 — Multi-Tenancy POC

Kepnora uses the following model:

- One Workspace represents one ABP Tenant.
- A global user may belong to multiple Workspaces.
- The MVP supports Owner and Member roles.

Validate whether this design works correctly with ABP Identity and multi-tenancy.

Pay special attention to global user identity, workspace membership, tenant switching, authorization, and data isolation.

Do not assume that ABP supports this identity model out of the box.

### Step 4 — Backend Solution Structure

Create the backend solution and project structure according to:

`docs/implementation/backend-project-structure.md`

Follow the documented module boundaries and dependency rules.

Do not introduce unnecessary microservices or additional infrastructure.

### Step 5 — Database Foundation

Configure PostgreSQL and Entity Framework Core.

Implement the database foundation and migrations required for M0.

Follow the documented tenant isolation strategy.

### Step 6 — Backend Infrastructure

Configure the foundational backend components required by the specifications, including:

- Dependency injection
- Configuration management
- Logging
- Exception handling
- API conventions
- Development environment configuration

### Step 7 — Mobile Foundation

If included in the documented M0 scope, create the React Native + Expo application skeleton.

Do not implement business feature screens yet.

### Step 8 — Testing and Validation

Add appropriate tests to validate:

- Application startup
- Database connectivity
- EF Core migrations
- Authentication
- Workspace membership
- Tenant isolation
- Cross-tenant access restrictions

## 4. Implementation Rules

1. Follow the existing Markdown specifications.
2. Do not implement M1 or later business features.
3. Do not introduce unnecessary abstractions or overengineering.
4. Use clean, maintainable, production-oriented code.
5. Follow established .NET and ABP conventions.
6. Keep the implementation compatible with the documented architecture.
7. Explain any necessary deviations from the specifications.
8. Do not silently change existing architectural decisions.
9. Run builds and tests whenever possible.
10. Do not claim a feature is working unless it has been verified.

## 5. Expected Deliverables

Provide:

- Architecture review findings
- ABP .NET 10 compatibility POC results
- Global identity and multi-tenancy POC results
- Backend solution structure
- Database configuration and migrations
- Foundation implementation
- Automated tests and their results
- Instructions for running the project locally
- Known limitations and unresolved risks

## 6. Execution Instructions

**Do not start implementing everything immediately.**

First:

1. Read all required specification files.
2. Summarize your understanding of M0.
3. Identify missing information, conflicts, and technical risks.
4. Propose a step-by-step implementation plan.
5. Identify the first POC to implement.

**Stop and wait for my approval before writing or modifying code.**

Once approved, implement one step at a time, verify the result, and report progress before moving to the next step.

The goal is to establish a stable, validated foundation for Kepnora before implementing M1 — Authentication and Workspace Management.