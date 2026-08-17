# UI Alignment Feedback — 2026-08-17

## Status

This document records the correction made after the user verified the public v0.1.11 build with actual screenshots.

- Public release remains: `v0.1.11`
- Corrected implementation is merged to `main` but is **not yet released**.
- Feature PR: `#94` — `Fix rendered UI alignment and compact ammo controls`
- Main implementation commit: `64f353dd71ee69ec4e474a73fa94717b015e9c4b`
- PR rendered-layout CI: `32022249988` — SUCCESS
- Main rendered-layout CI: `32022514487` — SUCCESS
- Automated tests: `210 passed / 0 failed / 0 skipped`

## User-verified regressions in v0.1.11

The v0.1.11 screenshots proved that source-level layout declarations and startup smoke alone were not sufficient to validate these UI requirements.

1. Flexible hand-in candidate rows were still visually content-centered instead of using a common left/right axis.
2. Ammo favorite control still rendered `☆ 즐겨찾기` / `★ 즐겨찾기` after runtime refresh.
3. Map current-Quest rows still had different title start positions depending on marker/checkbox content.
4. Ammo detail handle still contained explanatory text and used the opposite arrow direction from the requested interaction model.
5. The expanded Map Quest sidebar handle remained on the inner/left side instead of the panel's outer/right boundary.

## Root cause

### Global Button template

`Themes/DarkControls.xaml` owns the application-wide Button template and its `ContentPresenter` is hard-centered.

That means merely setting `HorizontalContentAlignment="Stretch"` on an individual Button did **not** make its content fill the Button.

This affected two independently reported screens:

- `FlexibleCandidateTemplate`: the entire four-column item Grid remained content-sized and centered.
- Map current-Quest row: the Quest text lived inside Button content and therefore remained centered/content-sized even though the row Grid itself had fixed lanes.

The previous fixes changed source structure but did not test the final WPF arranged coordinates, allowing the visual regression to survive.

### Ammo favorite runtime overwrite

`AmmoPage.xaml` already declared a star-only favorite Button, but `UpdateFavoriteButton()` later overwrote its content with:

- `☆ 즐겨찾기`
- `★ 즐겨찾기`

Therefore static XAML inspection was insufficient.

## Final implementation

### Flexible hand-in item rows

The candidate Button now owns a local template whose `ContentPresenter` stretches horizontally.

Canonical candidate row layout:

```text
52px icon | * name/category | 108px in-raid | 96px normal
```

Properties:

- row: 68px
- icon frame: 44px
- icon and item name use a fixed left axis
- in-raid and normal quantities use fixed right-side lanes
- the full candidate Grid occupies the rendered row width rather than floating in the center

### Ammo favorite

Both static XAML and runtime update paths now use only:

```text
☆
★
```

The Korean `즐겨찾기` text is never appended to the star Button.

### Ammo detail handle

The handle is a compact 42px centered Button with arrow only.

Interaction contract:

```text
Details expanded  -> ▼
Details collapsed -> ▲
```

No `탄약 / 수급 경로 상세정보` text remains in the handle.

### Map current-Quest row

The row keeps permanent lanes regardless of whether an individual Quest has data for the lane:

```text
30px checkbox | 34px A/B/C/D badge | * Quest text
```

The transparent Button is now only the click surface. Quest title/subtext is rendered directly in the third Grid column, outside the application-wide centered Button ContentPresenter.

Therefore marker presence, checkbox presence, and title length do not change the Quest title start X-axis.

### Map Quest sidebar handle

The sidebar root columns were reversed to:

```text
expanded Quest content | 34px handle
```

- collapsed: content column = 0px, handle remains the entire collapsed strip
- expanded: content fills the left side and the handle remains on the **right outer boundary**, adjacent to the map

This matches the user's drawer-handle interaction model.

## Rendered UI regression gate

A new permanent `MainWindow.ProductUiLayoutSmoke` is part of the published-app smoke path.

This is intentionally not a source-string or compile-only test. It instantiates/arranges real WPF controls and measures final rendered geometry/state.

The release smoke now verifies:

### Flexible item candidate

- 900px probe host
- inner candidate Grid actually expands to the row (`> 820px`), catching the old centered-content failure
- icon/name fixed left axes
- in-raid/normal **right edges** align with their fixed right-side columns

### Ammo

- favorite content is exactly one of `☆` / `★`
- favorite control remains compact
- expanded detail state renders `▼`
- collapsed detail state renders `▲`
- detail host visibility changes with the handle state

### Map Quest sidebar

Synthetic rows cover:

- checkbox + marker badge
- no checkbox/no badge
- badge without marker coordinates

Their actual rendered Quest title X positions must differ by no more than `0.75px`.

The expanded sidebar handle's actual right-edge gap must be no more than `6px`.

## Validation history

The first PR run with this new rendered gate deliberately failed and exposed a mistake in the test itself:

```text
Flexible candidate rendered X lanes drifted:
icon=0.0, name=60.0, fir=734.0, general=850.0, row=870.0
```

The important observation was that the row had in fact expanded to `870px`; the assertion was incorrectly comparing the left edge of right-aligned content. The gate was corrected to validate the fixed **right edges**, which is the actual product requirement.

Final validated runs:

```text
PR #94 CI:  32022249988 — SUCCESS
main CI:    32022514487 — SUCCESS
Tests:      210 / 210 passed
Publish:    Windows x64 SUCCESS
Runtime:    Main Map / Factory / MiniMap SUCCESS
UI layout:  rendered WPF coordinate/state assertions SUCCESS
Shutdown:   graceful SUCCESS
```

## Release policy

Do not describe this correction as part of public v0.1.11. The public v0.1.11 screenshots are the evidence that the prior implementation was insufficient.

The next release may include this `main` state only after release/version packaging is intentionally prepared and revalidated from the exact release baseline.
