# RELEASE 1.3.0 — Scanner 분석 워크플로 / one-shot test / global hotkeys

상태: **PUBLIC RELEASE / VERIFIED**
날짜: 2026-08-23

## Release identity

```text
version: v1.3.0
release source: f03441672d39165678fa53f57af46f103070d50e
final PR: #142
final PR CI: 32611343850 — SUCCESS
public verification status commit: 4cefa27012eafa62d40ef99f4efd630f3c53127a
GitHub release id: 375089921
asset: Junhyun-Helper-v1.3.0-win-x64.zip
bytes: 80,306,655
SHA-256: 5880c71098d737b7ffd3447eb77a55195d09d76ea12be7ff79df4eb055ac8344
ProductVersion: 1.3.0+f03441672d39165678fa53f57af46f103070d50e
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
verifiedAtUtc: 2026-08-23T01:58:02.2393394+00:00
```

## Scope

- explicit raw recognition PNG export
- one-shot all-display test recognition
- hotkey-only one-shot in-game/test commands
- Scanner ON/OFF global command
- F10/F11/F12 defaults with Ctrl+Shift
- one hotkey settings window; disable/change/duplicate prevention
- MainWindow-lifetime registration
- Scanner settings schema v4 migration

## Preserved contracts

- Scanner Lab v3.8 recognition architecture
- Windows ko-KR OCR primary
- current official Korean catalog authority
- conservative visual recovery
- unchanged confidence/margin
- unchanged best-trader / flea avg24h / RequiredTotal semantics
- no scan-time HTTP / game memory / DLL injection / packet interception

## Verification

- 256 passed / 0 failed / 0 skipped
- Windows Release build
- win-x64 self-contained single-file publish/root audit
- ProductVersion/FIRST_RUN identity audit
- rendered Scanner v1.3 UI contract
- schema v4 + v3 migration self-check
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap actual EXE smoke
- Draft re-download/checksum/root/ProductVersion/FIRST_RUN + EXE smoke
- public/latest and exact tag source verification
- independent public re-download/checksum/root/ProductVersion/FIRST_RUN + EXE smoke

Public verification evidence: `docs/.release-v1.3.0-status.json`.