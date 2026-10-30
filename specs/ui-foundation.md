# Kepnora — React Native Mobile Foundation

You are a Senior React Native Engineer and Mobile Software Architect.

Your task is to establish a production-ready, maintainable React Native foundation for the **Kepnora** mobile application.

Kepnora is a property and item maintenance management application backed by an ASP.NET Core / ABP .NET 10 API.

## 1. Read the Existing Specifications

Before writing code, review:

- `docs/kepnora-project-context.md`
- `docs/architecture-overview.md`
- `docs/mvp-build-order.md`
- `docs/implementation/mobile-project-structure.md`
- `docs/features/authentication-onboarding.md`
- `docs/api/api-contracts-master.md`

Also inspect the existing backend implementation and the current M1 specification.

If a referenced file is missing, report it. Do not invent its contents.

Follow the existing specifications and identify any conflicts before implementation.

## 2. Technology Stack

Use:

- React Native with Expo
- TypeScript with strict mode
- Expo Router for navigation
- TanStack Query for server state
- Zustand for lightweight client state
- Expo SecureStore for sensitive credentials
- React Hook Form and Zod for form handling and validation
- ESLint and Prettier
- Jest and React Native Testing Library for unit/component tests

Use compatible, stable versions supported by the selected Expo SDK.

Prefer Expo-supported packages and install native dependencies using `npx expo install` when appropriate.

Avoid adding libraries that are not necessary.

## 3. Project Location

Create the mobile application at:

`mobile/`

The mobile application must coexist with the existing .NET backend in the same repository.

Do not modify the backend solution structure unnecessarily.

## 4. Architecture and Folder Structure

Use a feature-based architecture.

Separate:

- Routing and navigation
- Reusable UI components
- Feature-specific components and logic
- API communication
- Server state
- Client state
- Authentication and session management
- Theme and design tokens
- Utilities and shared types

Keep Expo Router route files lightweight.

Do not put API calls, business logic, or large UI implementations directly into route files.

Avoid circular dependencies and unnecessary abstractions.

## 5. Navigation

Configure Expo Router with support for:

- Public authentication routes
- Protected application routes
- Onboarding routes
- Main tab navigation
- Modal screens

Use the documented Kepnora navigation structure.

Protect routes based on authentication and workspace state.

Do not assume that hiding a screen provides security. Backend authorization remains authoritative.

## 6. API Integration

Create a centralized, typed API client.

Requirements:

- Configurable API base URL
- Authentication token handling
- Consistent error handling
- Request cancellation
- Support for backend validation errors
- Safe token refresh handling
- TanStack Query integration
- Workspace-aware request context

Follow the backend API contracts.

Do not invent API endpoints or response models.

If an endpoint is unavailable, clearly document the integration gap.

## 7. Authentication and Security

Prepare secure session management.

Requirements:

- Store sensitive tokens using Expo SecureStore
- Never store access or refresh tokens in AsyncStorage
- Do not log credentials or authentication tokens
- Handle expired sessions
- Prevent concurrent token refresh races
- Clear session state on sign-out
- Clear workspace-scoped cached data when switching workspaces
- Handle app restart and session restoration safely

Follow the authentication flow defined in the backend specifications.

Do not invent token storage or tenant-switching behavior that conflicts with ABP.

## 8. State Management

Use TanStack Query for remote/server state.

Use Zustand only for lightweight local application state when needed.

Avoid duplicating server data inside Zustand.

Define clear query-key conventions, including workspace scope where applicable.

## 9. UI Design System

Create a reusable design system based on the existing Kepnora UI specifications.

Include:

- Color tokens
- Typography
- Spacing
- Border radius
- Icons
- Buttons
- Text inputs
- Form fields
- Cards
- Loading indicators
- Empty states
- Error states

Use the existing Kepnora branding.

Support accessibility, dynamic text sizing, keyboard handling, safe areas, and both iOS and Android.

Do not invent a new visual identity.

## 10. Forms and Validation

Use React Hook Form and Zod.

Create reusable form components with:

- Field-level validation
- Accessible error messages
- Loading and disabled states
- Consistent keyboard behavior
- Proper handling of server-side validation errors

## 11. Configuration

Configure:

- Development environment
- API base URL
- Expo app configuration
- TypeScript path aliases
- ESLint
- Prettier
- Environment variable handling

Never embed production secrets in the mobile application.

Remember that Expo public environment variables are visible in the application bundle.

## 12. Testing

Add tests for:

- Shared UI components
- Form validation
- API client behavior
- Authentication state
- Session restoration
- Token refresh
- Workspace switching and cache isolation

Use mocks for external services when appropriate.

## 13. M1 Scope

Implement only the mobile foundation and the authentication/workspace flows required by the approved M1 specification.

Do not implement later milestone business features.

Do not implement subscription billing, service discovery, booking, or payment features.

## 14. Deliverables

Provide:

1. Mobile project structure
2. Dependency list with explanations
3. Navigation architecture
4. API client and authentication foundation
5. Reusable UI components and design tokens
6. Environment configuration
7. Tests and verification results
8. Local development instructions
9. Known limitations and unresolved integration issues

## 15. Execution Rules

Before coding:

1. Inspect the existing repository.
2. Read the required specifications.
3. Check whether a mobile project already exists.
4. Verify Expo SDK and package compatibility.
5. Propose the folder structure and dependencies.
6. Identify missing backend API contracts.
7. Present an implementation plan.

**Stop and wait for my approval before creating or modifying files.**

After approval, implement incrementally.

Run TypeScript checks, linting, and tests. Verify the application starts successfully.

Do not claim iOS or Android functionality has been verified unless it has actually been tested.

Keep the architecture simple, scalable, and aligned with Kepnora's MVP requirements.