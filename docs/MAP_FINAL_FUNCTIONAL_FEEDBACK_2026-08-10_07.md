# Map final functional feedback — 2026-08-10

Status: IMPLEMENTED / AUTOMATED VALIDATION PENDING / WINDOWS USER VALIDATION NEXT

## User feedback

After PR #70 most requested Map/MiniMap functionality works. Remaining functional issue:

- Floor hotkey changes MiniMap correctly, but Main Map artwork can disappear after the hotkey floor transition.

Additional product request:

- Add a MiniMap opacity slider to Main Map settings.

## Implementation

### Main Map floor hotkey serialization

The product floor hotkey no longer starts Main Map and MiniMap floor rendering concurrently.

New sequence:

```text
floor hotkey
→ Main Map floor ComboBox state update
→ await Main Map SVG regeneration
→ await Main Map marker refresh
→ MiniMap floor change
```

The normal manual dropdown path remains unchanged.

The Main Map product hotkey endpoint now exposes awaited floor-up/down operations and prevents overlapping floor-hotkey renders.

### MiniMap base opacity

Main Map settings now contains a `미니맵 투명도` slider.

- Range: 10%–100%
- Default: 100%
- Persisted in `%LocalAppData%/JunhyunHelper/map-product-settings.json`
- Changes apply immediately to an active MiniMap
- New MiniMap windows restore the persisted value on registration

The base opacity is applied independently from the existing full-hide presentation behavior:

```text
normal state
→ configured base opacity

cursor hover OR timed-hide active
→ fully transparent (0%)
```

This preserves the existing hover-hide and temporary-hide semantics.
