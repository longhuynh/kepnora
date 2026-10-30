# Kepnora — Mobile Design System

**Path:** `docs/ui/design-system.md`  
**Version:** 1.0-draft · **Date:** 2026-10-29  
**Status:** Proposed premium visual direction; tokens and accessibility targets require device validation  
**Platform:** React Native + Expo + TypeScript · iOS and Android  
**Related:** `../architecture/mobile-app-architecture.md`, `../architecture/api-design.md`, `../architecture/authentication-authorization.md`, `../architecture/file-storage.md`, `../mvp-build-order.md`

---

## 1. Purpose

This document defines a consistent, implementation-ready visual and interaction language for the Kepnora mobile app. It translates the explored **premium Apple Home / Airbnb-inspired direction** into original Kepnora components, tokens, states, navigation patterns, accessibility rules and acceptance criteria. Inspiration means restrained layout, clear hierarchy and high-quality photography—not reproducing another company's protected UI, logos or distinctive assets.

The document is a **design proposal**, not evidence that final colors, fonts or components have been approved or coded. Where exact measurements are supplied, they are starting tokens to validate on iOS and Android.

## 2. Product and phase boundaries

| Phase | Screens and experiences | Navigation treatment |
|---|---|---|
| Phase 1 | Authentication, onboarding, workspaces, dashboard, properties, assets, maintenance, documents, search, settings | Active routes |
| Phase 2 | Service discovery, local providers, provider profiles, quote/contact | Future extension; not in Phase 1 tabs |
| Phase 3 | Booking, payment, provider transactions | Future extension; not in Phase 1 tabs |

Phase 1 must work for users who manage a standalone vehicle or device with **no property**. The design must not assume every asset belongs to a house.

## 3. Experience principles

1. **Calm and premium:** soft neutrals, limited accent color, deliberate spacing, low visual noise.
2. **Asset first:** clear asset identity, next maintenance action and document access.
3. **Actionable status:** show what is due, why, and the next safe action.
4. **Progressive disclosure:** overview first, details and advanced controls on demand.
5. **One-handed mobile use:** primary actions in comfortable reach, large touch targets.
6. **Trustworthy state:** never show success before the API confirms persistence.
7. **Accessible by default:** dynamic text, screen reader labels, contrast and reduced motion.
8. **Tenant-safe:** changing workspaces cannot expose another workspace's cached UI.

## 4. Visual personality

**Keywords:** refined, approachable, organized, dependable, warm, quiet, tactile.  
**Avoid:** heavy gradients, neon accent overload, small low-contrast text, dense enterprise tables, excessive shadows, fake data disguised as real metrics, decorative badges on every card.

Suggested visual composition: off-white canvas, white elevated surfaces, deep ink text, muted blue accent, sage secondary accent, natural asset imagery and generous rounded corners. Use photography where it communicates the actual asset; otherwise use simple neutral category icons.

## 5. Color token proposal

Colors are **proposed values** and must be checked against WCAG contrast ratios on actual rendered screens. Never rely on color alone to convey state.

| Token | Light | Dark | Purpose |
|---|---|---|---|
| `color.canvas` | `#F7F8F6` | `#111714` | App background |
| `color.surface` | `#FFFFFF` | `#1B231F` | Cards and sheets |
| `color.surfaceMuted` | `#EEF1ED` | `#27312B` | Subtle groups |
| `color.ink` | `#1D2823` | `#F3F7F3` | Main text |
| `color.inkSecondary` | `#59665F` | `#B7C3BA` | Supporting text |
| `color.inkTertiary` | `#6B786F` | `#A5B2A9` | Metadata |
| `color.border` | `#DDE4DD` | `#39463D` | Dividers and outlines |
| `color.accent` | `#275D78` | `#8CC4DD` | Primary action/link |
| `color.accentPressed` | `#1B465B` | `#B5DDED` | Pressed action |
| `color.sage` | `#E4EEE5` | `#2A4332` | Calm informational background |
| `color.success` | `#24744B` | `#7CD8A3` | Confirmed success |
| `color.warning` | `#8A5B12` | `#F2C575` | Due/attention |
| `color.danger` | `#A63737` | `#FF9999` | Error/destructive |
| `color.overlay` | `rgba(12,20,16,0.45)` | `rgba(0,0,0,0.60)` | Modal scrim |

### 5.1 Semantic usage

- Primary buttons: accent fill, high-contrast label; validate contrast before release.
- Positive success: icon + text, not green color alone.
- Due: warm attention color + **Due** text and explicit due date.
- Overdue: stronger emphasis + **Overdue** text and date; avoid treating overdue as a persisted lifecycle value.
- Open maintenance with unknown mileage: **Mileage needed** / **Not enough data**, never an invented Due state.
- Disabled: visibly muted, but distinguish disabled from loading and permission-denied.
- Dark mode uses distinct semantic values rather than simply inverting light colors.

## 6. Typography

Use the platform's native system font by default to avoid custom-font distribution and ensure dynamic type support. Test iOS and Android font metrics independently.

| Role | Starting size | Weight | Line height | Use |
|---|---:|---|---:|---|
| Display | 32 | 700 | 38 | Main dashboard greeting/title |
| Screen title | 28 | 700 | 34 | Top-level screens |
| Section title | 21 | 650/700 | 27 | Card group headings |
| Card title | 17 | 600 | 23 | Asset name, service name |
| Body | 16 | 400 | 24 | Primary content |
| Body emphasized | 16 | 600 | 24 | Important values |
| Secondary | 14 | 400 | 20 | Descriptions and metadata |
| Caption | 12–13 | 500 | 18 | Timestamps and helper labels |
| Button | 16 | 600 | 20 | Button labels |

Never fix a text container height when dynamic type could wrap. Avoid all-caps paragraphs. Use tabular numerals for counts, money and dates where supported. Long asset names must wrap or truncate with a full-name accessibility label.

## 7. Spacing and layout grid

Base spacing unit: **4 dp/pt** in React Native logical layout units.

| Token | Value | Intended use |
|---|---:|---|
| `space.1` | 4 | Icon-to-label micro gap |
| `space.2` | 8 | Tight rows |
| `space.3` | 12 | Field label separation |
| `space.4` | 16 | Standard content gap |
| `space.5` | 20 | Card padding |
| `space.6` | 24 | Section gap |
| `space.8` | 32 | Major section separation |
| `space.10` | 40 | Hero whitespace |

Screen horizontal padding: **20** by default, **16** on very narrow screens if needed. Cards use 16–20 internal padding. Respect safe-area insets, keyboard overlap, bottom navigation and home indicator. No hard-coded assumption about iPhone screen dimensions.

## 8. Radius, border and elevation

| Token | Value | Use |
|---|---:|---|
| `radius.sm` | 10 | Tags, compact fields |
| `radius.md` | 14 | Inputs, small cards |
| `radius.lg` | 20 | Main cards |
| `radius.xl` | 26 | Featured cards, sheets |
| `radius.pill` | 999 | Pills and primary buttons |
| `border.subtle` | 1 | Neutral card outline |

Prefer borders and surface contrast to dramatic shadows. Use platform-specific shadows/elevation sparingly; never place meaningful content behind translucent surfaces without sufficient contrast.

## 9. Iconography and imagery

Use one consistent icon family compatible with Expo. Icon sizes: 18 (inline), 22 (navigation), 24 (action), 28–32 (empty state). Use filled/outline variants consistently, not randomly.

Asset photography rules:

- Prefer user-uploaded asset image when available and authorized.
- Use category illustration/icon when no photo exists; never imply a stock image is the user's real asset.
- Keep predictable image aspect ratios and rounded clipping.
- Provide skeleton/placeholder and broken-image fallback.
- Do not put confidential document previews in notification thumbnails.
- Do not request S3 presigned URLs for off-screen items unnecessarily.

## 10. App shell and navigation

Recommended **Phase 1** bottom tabs:

| Tab | Label | Primary task |
|---|---|---|
| 1 | Home | Dashboard and due summary |
| 2 | Assets | Browse, search and add assets |
| 3 | Maintenance | Due list, rules and service history |
| 4 | More | Documents entry, properties, settings, workspace |

Search may appear as a global action in Home/Assets rather than a fifth tab. Exact tab count is a UX decision to validate in usability tests. **Do not show Find Services or Booking tabs in Phase 1.**

Workspace switcher belongs near the top of Home and in More. It displays the selected workspace and role when helpful, not raw ABP tenant IDs.

## 11. Navigation and screen anatomy

```text
Safe Area
┌──────────────────────────────────┐
│ Workspace selector       Avatar │
│ Screen title                     │
│ Short context / optional action  │
│                                  │
│ Primary content cards            │
│ Lists / sections                 │
│                                  │
│                                  │
├──────────────────────────────────┤
│ Home   Assets   Maintenance More │
└──────────────────────────────────┘
Safe Area / Home Indicator
```

Screen hierarchy: App shell → tab stack → detail → editor/sheet. Back navigation preserves context. Modal sheets are for focused actions, not entire complex workflows. Deep links always reauthorize on the server.

## 12. Dashboard screen specification

**Purpose:** answer “What needs attention?” within seconds.

Content order:

1. Workspace name and greeting, without unnecessary personal data.
2. Primary summary: assets tracked and maintenance needing attention.
3. **Needs Attention** list: Overdue then Due, with explicit dates.
4. **My Assets** horizontal cards or compact list.
5. Recent activity and a quiet add-asset action.

Empty dashboard: “Add your first asset” with category choices, including Vehicle and Electronics. Do not force Property creation. Counts come from tenant-scoped API responses; derived maintenance status is calculated server-side using rule timezone and mileage policy.

## 13. Asset list screen specification

Features: search, category filter, optional property filter, sort, add action, empty and archived states. Default card includes asset image/icon, name, category, optional property and next maintenance indicator.

**Example card content:**

```text
[Vehicle image]  Toyota Camry
                 Vehicle · No property
                 Oil change due Nov 12
                 View details →
```

“No property” is valid data, not an error. Do not infer mileage or maintenance due dates without server evidence. For long lists, use a virtualized list and stable keys.

## 14. Asset detail screen specification

Header: asset name, category, photo, overflow actions. Body sections: overview, upcoming maintenance, service history, documents, custom attributes. Prominent action: **Add maintenance** or **Log service** based on context.

Edit form follows the asset type's published schema version. Read-only global templates and custom tenant fields must be clearly distinguished. Cross-workspace navigation must never show a stale detail card from the prior workspace.

## 15. Property screens

Property list is optional for users with no properties. Property detail may show address/timezone and related assets. Archiving a property should explain that associated assets and maintenance history are preserved according to backend policy. Do not present property creation as a prerequisite to adding a car.

## 16. Maintenance list screen

Use three **display** groupings: Overdue, Due, Upcoming. These are derived labels for `Open` occurrences, not stored lifecycle values. Completed, Skipped and Cancelled belong in history or secondary filters.

A maintenance row should show asset, task name, due date or mileage threshold, and a clear action. If a mileage rule has no current reading, show **Add odometer reading** rather than claiming it is Due.

## 17. Maintenance detail and completion

The detail view contains rule interval, anchor, due logic, prior service, notes and attachments where supported. Completion form can capture service date, cost, optional mileage, notes and receipt attachment.

After submit, display pending state; only show Completed when API confirms success. On HTTP 409, explain that the occurrence was already changed and refresh the server state. Avoid double-completion from repeated taps; server idempotency remains authoritative.

## 18. Document library and upload

Documents are associated with an asset. Card/list shows category, filename, date, file type and size; do not expose S3 keys. Upload UX stages:

```text
Select file → Request upload intent → Upload bytes → Finalize → Complete
```

Progress should distinguish S3 upload from backend finalization. Closing the app or losing network must not display false success. Show retry where the upload intent remains valid; refresh intent if expired. Short-lived download links must be obtained after fresh authorization.

## 19. Search experience

Global search (if adopted) should allow asset and maintenance lookup, with explicit result categories and tenant-scoped results. Empty state offers examples such as “Search by asset name or serial number.” Debounce input; paginate; never mix results across workspaces. Phase 2 provider search is a **different data source and product flow**.

## 20. Settings and workspace screen

Sections:

- Account and profile.
- Active workspace, switch workspace and member role.
- Notification preferences and device permission status.
- Appearance (system/light/dark if supported).
- Privacy, help and app version.
- Sign out.

Workspace member management is visible only to authorized Owner users. Hiding a control is not authorization; backend permissions remain mandatory.

## 21. Buttons

| Variant | Use | Visual |
|---|---|---|
| Primary | Commit a key action | Accent fill, strong label |
| Secondary | Alternate safe action | Surface/border |
| Tertiary | Inline navigation | Text/icon |
| Destructive | Archive/delete where appropriate | Danger label and confirmation |
| Icon | Compact toolbar action | Minimum accessible hit target |

States: default, pressed, focused, disabled, loading. Loading must prevent duplicate taps without suggesting backend idempotency is unnecessary. Keep minimum touch target **44×44 logical units**, preferably 48×48 for frequent actions; validate against platform guidance.

## 22. Cards

Cards may be `AssetCard`, `MaintenanceCard`, `SummaryCard`, `DocumentCard`, `WorkspaceCard` and `EmptyStateCard`. Each has consistent radius, padding, border and typography. Only interactive cards have press affordance and accessibility role `button`.

Do not nest multiple competing press targets inside a card without a clear accessibility strategy. Important actions should remain visible without hover.

## 23. Inputs and forms

Fields require visible labels, helper text and server-error rendering. Placeholder is not a substitute for a label. Prefer native keyboards appropriate to content: numeric for mileage/cost, email for login, date picker for service date.

Form rules:

1. Use React Hook Form + Zod for immediate feedback.
2. Validate custom fields against server-provided published schema.
3. Preserve user edits after transient failure.
4. Disable submit only for explicit reasons.
5. Show field errors near the field and summarize when necessary.
6. Avoid sending user-supplied `TenantId` in mutation bodies.
7. Handle optimistic concurrency errors with refresh/review, not silent overwrite.

## 24. Selection controls and chips

Use chips for small sets of categories and status filters. A selected chip requires a non-color cue such as a checkmark or clear border. For long lists, use a searchable picker or sheet. Avoid using chips as decorative status badges on every row.

## 25. Bottom sheets, dialogs and alerts

Bottom sheets: asset actions, category selection, short confirmation. Full-screen flows: complex asset editor, maintenance rule builder, document viewer. Use confirmation for destructive operations; explain consequences plainly. Use native alerts for urgent permission or irreversible actions where appropriate.

## 26. Toasts and feedback

- Success: brief and factual, only after server confirmation.
- Error: actionable explanation with retry when safe.
- Warning: explain incomplete information or potential loss of unsaved edits.
- Offline: clear banner and stale-data labeling; do not imply queued writes are persisted unless a real queue exists.

Avoid auto-dismissing critical error information before a screen reader can announce it.

## 27. Skeleton, loading and empty states

Use skeletons matching final layout to reduce visual jumping. Avoid endless spinner-only pages. Empty states must distinguish:

- No assets yet.
- No maintenance due.
- No documents attached.
- No search matches.
- Offline/no cached data.
- Permission denied.

Each state should offer one relevant next action, not a wall of buttons.

## 28. Maintenance status components

| Display state | Condition | Suggested UI |
|---|---|---|
| Upcoming | Open and not yet within due window | Neutral date label |
| Due | Open and within defined due window | Warning icon + Due |
| Overdue | Open and beyond due/grace policy | Strong warning + Overdue |
| Mileage unknown | Reading unavailable for mileage rule | Add reading action |
| Completed | Persisted lifecycle Completed | Success + completion date |
| Skipped | Persisted lifecycle Skipped | Neutral history label |
| Cancelled | Persisted lifecycle Cancelled | Neutral history label |

The exact due/grace algorithm remains an ADR decision; the UI consumes backend results rather than reimplementing conflicting date logic.

## 29. Motion and transitions

Use subtle transitions: 150–250 ms as a starting point for small feedback, with platform-native navigation transitions. Respect reduced-motion accessibility settings. Avoid motion that delays task completion or makes status seem more urgent than it is. Skeleton shimmer must not create distracting flashing effects.

## 30. Accessibility baseline

- Support dynamic type and text scaling without clipped controls.
- Provide accessible names, roles, states and hints for icons and custom components.
- Test VoiceOver and TalkBack on representative flows.
- Target WCAG 2.2 AA text contrast (4.5:1 normal, 3:1 large) and appropriate non-text contrast; verify actual color pairings.
- Do not encode maintenance urgency by color alone.
- Maintain visible focus indicators for keyboard/switch users where applicable.
- Use meaningful heading hierarchy and logical reading order.
- Support larger hit targets and adequate spacing.
- Test dark mode and high-contrast device settings.

## 31. Internationalization and formatting

English is the initial documentation and UI-copy baseline. Prepare string resources for localization instead of hardcoding text throughout screens. Dates and currency must be formatted for locale and user preferences. Maintenance due calculations use the rule's approved timezone policy, not merely the device clock.

Avoid assuming every user is in the United States or uses USD. Address formats, units and measurement preferences may require later product decisions.

## 32. Design token implementation

```typescript
// src/ui/tokens.ts — proposed values, not an installed package
export const spacing = {
  1: 4,
  2: 8,
  3: 12,
  4: 16,
  5: 20,
  6: 24,
  8: 32,
  10: 40,
} as const;

export const radius = {
  sm: 10,
  md: 14,
  lg: 20,
  xl: 26,
  pill: 999,
} as const;

export const lightColors = {
  canvas: '#F7F8F6',
  surface: '#FFFFFF',
  surfaceMuted: '#EEF1ED',
  ink: '#1D2823',
  inkSecondary: '#59665F',
  border: '#DDE4DD',
  accent: '#275D78',
  success: '#24744B',
  warning: '#8A5B12',
  danger: '#A63737',
} as const;

export const darkColors = {
  canvas: '#111714',
  surface: '#1B231F',
  surfaceMuted: '#27312B',
  ink: '#F3F7F3',
  inkSecondary: '#B7C3BA',
  border: '#39463D',
  accent: '#8CC4DD',
  success: '#7CD8A3',
  warning: '#F2C575',
  danger: '#FF9999',
} as const;
```

Use a typed `useTheme()` hook or equivalent shared provider. Components consume semantic tokens, not arbitrary hex literals. Persist user appearance preference only if the product exposes a setting.

## 33. Example reusable primary button

```tsx
import React from 'react';
import { ActivityIndicator, Pressable, Text } from 'react-native';

interface PrimaryButtonProps {
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
}

export function PrimaryButton({
  label,
  onPress,
  loading = false,
  disabled = false,
}: PrimaryButtonProps) {
  const blocked = loading || disabled;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ disabled: blocked, busy: loading }}
      disabled={blocked}
      onPress={onPress}
      style={({ pressed }) => ({
        minHeight: 48,
        borderRadius: 999,
        backgroundColor: '#275D78', // Replace with theme token.
        opacity: blocked ? 0.55 : pressed ? 0.85 : 1,
        justifyContent: 'center',
        alignItems: 'center',
        paddingHorizontal: 20,
      })}
    >
      {loading ? (
        <ActivityIndicator color="#FFFFFF" />
      ) : (
        <Text style={{ color: '#FFFFFF', fontSize: 16, fontWeight: '600' }}>
          {label}
        </Text>
      )}
    </Pressable>
  );
}
```

This example intentionally keeps the API small; production implementation must use theme tokens, localization, focus styles, dynamic type and tests. It does not establish whether a network mutation succeeded.

## 34. Example asset card contract

```typescript
export interface AssetCardViewModel {
  id: string;
  workspaceId: string;
  name: string;
  categoryLabel: string;
  propertyName?: string | null;
  imageUrl?: string | null;
  maintenanceLabel?: string | null;
  maintenanceSeverity?: 'neutral' | 'warning' | 'danger';
}
```

The view model is a presentation shape, **not** an assertion that these fields already exist in ABP DTOs. Map generated API types to it in the feature layer. Never persist the presentation severity as an occurrence lifecycle status.

## 35. Folder structure proposal

```text
mobile/src/ui/
  tokens.ts
  theme/
    ThemeProvider.tsx
    useTheme.ts
  primitives/
    AppText.tsx
    AppIcon.tsx
    AppButton.tsx
    AppCard.tsx
    AppInput.tsx
    AppSheet.tsx
    StatusLabel.tsx
    EmptyState.tsx
  patterns/
    ScreenHeader.tsx
    AssetCard.tsx
    MaintenanceCard.tsx
    DocumentRow.tsx
    WorkspaceSwitcher.tsx
    SectionHeader.tsx
  hooks/
    useReducedMotion.ts
    useResponsiveLayout.ts
```

Feature-specific state stays under `src/features/*`, not in global UI primitives. Navigation remains under Expo Router `app/` and API contracts under `src/api/`.

## 36. Component ownership and naming

- `App*` primitives are domain-agnostic and reusable.
- Domain cards know presentation models but do not directly call APIs.
- Feature hooks own TanStack Query mutations and server state.
- Screen files compose UI and navigation, not business validation.
- Domain due-state calculation belongs on the backend.
- S3 upload lifecycle belongs to a document feature service, not `AppButton`.

This separation keeps the design system testable and avoids duplicating authorization logic in presentation components.

## 37. Responsive behavior

Design for common narrow phones first. On wider phones, increase content width cautiously; on tablets, use centered max-width content and multi-column asset grids where practical. Never scale every element proportionally based on device width. Test landscape, keyboard-open, large text and notched/safe-area layouts.

## 38. Screen state matrix

| Screen | Loading | Empty | Error | Offline | Success |
|---|---|---|---|---|---|
| Home | Summary skeleton | Add first asset | Retry dashboard | Labeled cached data | Live counts |
| Assets | List skeleton | Add asset | Retry list | Cached list if available | Updated inventory |
| Asset detail | Detail skeleton | Not applicable | Safe not-found | Cached data clearly labeled | Authorized detail |
| Maintenance | List skeleton | No tasks due | Retry | Stale status warning | Fresh due status |
| Documents | Row skeleton | Add document | Retry | No new presign | Finalized files |
| Settings | Form skeleton | Not applicable | Retry save | Local-only warning | Confirmed preferences |

## 39. User flows to prototype

1. New user → login → workspace → add standalone car → dashboard.
2. Existing user → Home overdue card → maintenance detail → complete → refreshed Home.
3. Asset detail → add receipt → S3 upload → finalize → document list.
4. Owner X/Member Y → workspace switch → different asset inventory and permissions.
5. Offline user → open cached asset → attempt edit → explicit failure/retry behavior.
6. User with no properties → add electronic device → attach manual.
7. Notification tap → authorized occurrence detail; handle revoked membership.

These are prototype scenarios, not claims that corresponding endpoints already exist.

## 40. Copywriting conventions

Prefer short, calm and specific copy:

| Situation | Recommended | Avoid |
|---|---|---|
| Empty assets | “Add your first asset” | “Nothing here!” |
| Maintenance due | “Oil change due Nov 12” | “URGENT!!!” |
| Missing mileage | “Add an odometer reading” | “Overdue” without data |
| Upload in progress | “Uploading receipt…” | “Saved” before finalize |
| Upload finalizing | “Finishing upload…” | “100% complete” before DB commit |
| Access revoked | “You no longer have access to this workspace.” | Internal tenant/role diagnostics |
| Save conflict | “This item changed. Refresh to review the latest version.” | Raw HTTP 409 stack trace |

## 41. Visual QA checklist

- [ ] Primary hierarchy readable within three seconds.
- [ ] Main action visible without unnecessary scrolling.
- [ ] Spacing, radius and typography use shared tokens.
- [ ] Asset cards work with and without photos.
- [ ] Standalone assets do not require property labels.
- [ ] Due/Overdue state has text and icon, not color alone.
- [ ] No user data leaks across workspace switch.
- [ ] Long names and dynamic type do not clip.
- [ ] Dark mode colors and contrast are verified.
- [ ] VoiceOver/TalkBack labels and reading order are correct.
- [ ] Loading, empty, error and offline states are designed.
- [ ] Upload shows separate S3 and finalize stages.
- [ ] Phase 2/3 screens are not accidentally enabled in Phase 1.

## 42. Component test catalog

| Test ID | Scenario | Expected |
|---|---|---|
| UI-001 | Large accessibility font on asset card | No clipped critical text |
| UI-002 | Dark mode status chip | Legible label and icon |
| UI-003 | Button double tap during mutation | No duplicate client dispatch |
| UI-004 | Screen reader on icon button | Accessible name announced |
| UI-005 | Keyboard open on long form | Focused field and submit reachable |
| UI-006 | No asset image | Stable fallback |
| UI-007 | Null PropertyId | Correct standalone asset display |
| UI-008 | Unknown mileage | No false Due/Overdue state |
| UI-009 | Maintenance API 409 | Conflict guidance, no false success |
| UI-010 | Upload PUT success, finalize fails | Retryable incomplete state |
| UI-011 | Workspace X→Y | X cards disappear immediately |
| UI-012 | Revoked workspace deep link | Access denied, no stale detail |
| UI-013 | Search empty | Helpful empty state |
| UI-014 | Slow dashboard | Skeleton and recovery |
| UI-015 | Offline asset edit | Explicit not-saved state |
| UI-016 | Reduce motion enabled | Nonessential animations minimized |
| UI-017 | Narrow Android device | No horizontal overflow |
| UI-018 | iOS safe area and bottom tabs | No home-indicator overlap |
| UI-019 | Long localized date/name | Wrapping and readable layout |
| UI-020 | Color-vision accessibility | Status distinguishable without hue |

## 43. Milestone mapping

| Milestone | Design-system work |
|---|---|
| M0 | Theme provider, tokens, typography, app shell proof |
| M1 | Authentication forms, workspace selector, permission states |
| M2 | Property and asset-type selection components |
| M3 | Asset list, card, detail and editor patterns |
| M4 | Maintenance statuses, rule editor, completion/history |
| M5 | Document picker, upload progress, file rows |
| M6 | Notification settings and deep-link patterns |
| M7 | Dashboard, search, settings, responsive polish |
| M8 | Accessibility, device QA, contrast and release audit |

## 44. Open design decisions

The following require user/product approval or validation before implementation is frozen:

1. Final brand accent palette and logo treatment.
2. System font vs licensed custom font.
3. Four-tab vs five-tab navigation.
4. Default dark mode behavior.
5. Asset photography vs category illustration emphasis.
6. Whether workspace name is always visible in the header.
7. Maintenance due/grace wording and date formats.
8. Localization priorities and unit preferences.
9. Thumbnail generation and image privacy policy.
10. Exact tablet layout and accessibility test devices.

A mockup is not approval of these details. Document changes in the design-system changelog and relevant ADRs.

## 45. AI coding-agent implementation instructions

For each screen/component task, provide:

```markdown
Goal: Implement one specified Phase 1 UI component or screen.
Design source: docs/ui/design-system.md, section(s) X–Y.
Contracts: Generated OpenAPI types and feature view models.
Constraints: Semantic tokens, dynamic type, tenant-safe cache, no guessed API routes.
States: Loading, empty, error, offline, success, permission denied.
Tests: Accessibility, workspace switching, interaction and API failure cases.
Out of scope: Provider discovery, booking, payments and speculative backend fields.
Acceptance: Screenshots on iOS/Android, test evidence, reviewed code.
```

Do not let a coding agent invent a persisted `Overdue` status, force a property on every asset, hardcode a tenant ID, display private S3 object keys or enable Phase 2 tabs.

## 46. Definition of Done

A design-system component is complete only when it uses semantic tokens, supports light/dark mode as configured, has appropriate accessible labels and touch targets, handles all relevant states, and passes iOS/Android QA. A feature screen additionally requires verified server contracts, tenant-safe state handling and acceptance tests.

## 47. Final design principle

**Kepnora should feel like a premium personal organizer, not an enterprise administration console.** Keep the interface calm and beautiful while preserving truthful maintenance states, explicit permissions, reliable uploads and clear recovery from failure.
