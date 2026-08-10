# MiniMap marker size feedback — 2026-08-10

Status: IMPLEMENTED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT

## User feedback

After the Map/MiniMap functional pass, MiniMap markers—especially extract markers and their labels—remain visually too large.

Requested product change:

- Add a MiniMap marker-size slider to the Main Map settings panel.

## Product behavior

Main Map settings now contains `미니맵 마커 크기`.

- Range: 25%–150%
- Step: 5%
- Default: 100%
- Persisted in `%LocalAppData%/JunhyunHelper/map-product-settings.json`
- Applies immediately to an active MiniMap
- Restored automatically when a new MiniMap window is created

The scale applies only to MiniMap non-player marker presentation:

- Quest markers and labels
- General Map markers
- PMC / Scav / Transit extract markers and labels

The player position marker remains controlled by the existing player-marker size setting and is intentionally not affected.

## Rendering rule

The new value is a MiniMap-only multiplier applied after zoom compensation. Therefore changing MiniMap zoom does not undo the chosen marker size.

```text
MiniMap visual marker scale
= inverse MiniMap zoom scale × configured MiniMap marker scale
```

General marker source normalization remains in place, with the configured product scale applied on top.

## Validation

The Map smoke test inserts a live marker probe into the real MiniMap marker container and verifies that applying 50% scale changes its actual WPF `RenderTransform`, then restores the persisted value. This prevents a settings-only implementation from passing without affecting the rendered MiniMap.

Final PR #72 validation:

```text
PR: #72 Add MiniMap marker size control
merge: c2e9b79efc27178216b6f5c3b52135465f970d0c
final head: f0ccd0cdc5c998058dd3a57603915624c0baa8ed
CI: 31352660359
artifact: 9049510735
artifact digest: sha256:2e8ff51a8146051a42e15cbcce27377b0848c829ef88e1648a602618d2acca0b

Desktop Release build: success
automated tests: success
Windows x64 self-contained publish: success
Startup + exact Map/MiniMap smoke: success
live MiniMap marker RenderTransform scale smoke: success
graceful Main Window close + process exit: success
ZIP creation/upload: success
```
