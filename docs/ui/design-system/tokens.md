# Kepnora — Design Tokens Reference

**Status:** Reference document. The proposed token values currently live in [`../design-system.md`](../design-system.md). Do not introduce a second independent token source.

## Source of truth

Use the design-system specification for colors, typography, spacing, radii, elevation, motion and accessibility. Confirm values on actual iOS and Android devices before freezing them. When implemented, extract the approved tokens into the React Native theme and update this file to reflect that single source of truth.

## Implementation guardrails

- No hard-coded token copies in screens.
- Use semantic color names and accessible contrast.
- Support dark mode and dynamic type when approved.
- Keep platform-specific differences explicit.
- Update visual regression tests when tokens change.
