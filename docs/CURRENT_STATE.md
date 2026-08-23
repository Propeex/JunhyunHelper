# CURRENT STATE

기준일: 2026-08-23

상태: **`v1.3.0 PUBLIC RELEASE / VERIFIED`**

```text
release source: f03441672d39165678fa53f57af46f103070d50e
final PR #142 CI: 32611343850 — SUCCESS
tests: 256/256
asset: Junhyun-Helper-v1.3.0-win-x64.zip
bytes: 80,306,655
SHA-256: 5880c71098d737b7ffd3447eb77a55195d09d76ea12be7ff79df4eb055ac8344
ProductVersion: 1.3.0+f03441672d39165678fa53f57af46f103070d50e
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

Scanner v1.3.0 current delta:

- explicit user-selected raw recognition PNG export; no automatic screenshot persistence
- one-shot in-game + one-shot all-display test, hotkey-only
- global defaults F10/F11/F12 with Ctrl+Shift
- one settings window, per-command disable/change, duplicate prevention
- MainWindow-lifetime global registrations
- settings schema v4 with v3 user-gesture preservation/collision-safe migration
- recognition thresholds and market/RequiredTotal semantics unchanged

상세: `docs/STATE.md`, `docs/SCANNER_V1.3.0_WORKFLOW.md`, `docs/RELEASE_1.3.0.md`.