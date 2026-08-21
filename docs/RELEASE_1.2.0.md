# v1.2.0 — Scanner title recognition overhaul

Status: **RELEASE CANDIDATE / VALIDATION IN PROGRESS**

Target release: 2026-08-22 KST

## Product scope

This MINOR release adds user-facing Scanner diagnostic-image and one-shot high-precision scan capabilities while structurally hardening item-name recognition.

Recognition pipeline:

```text
Tarkov display pixels
→ detail-window structural candidates
→ red close + magnifier + title-field anchor refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ semantic official-name matching
   OR OCR-independent full-catalog Bender/Noto glyph-shape recovery
→ confidence + top1/top2 margin gates
→ Item ID
→ local presentation / Mini Scanner
```

## Recognition contracts

- Existing successful OCR remains the primary path.
- Han ideographs are invalid OCR evidence for the Korean item-title catalog; unexpected characters are validated against the current official catalog rather than a hand-maintained punctuation blacklist.
- Corrupted or empty OCR may fall back to full-catalog visual comparison using locally extracted Tarkov Bender + Noto Sans CJK KR fonts.
- Visual recovery requires conservative score and margin thresholds and fails closed when ambiguous.
- Magnifier pixels are excluded from the OCR ROI when the anchor is detected.
- Anchor failure falls back to the existing Scanner Lab geometry ROI rather than inventing an unverified region.
- Scan-time network remains prohibited. No game memory, DLL injection, or packet interception is used.

## User-facing additions

- `인식 이미지`: keeps one latest diagnostic capture in process memory only and shows the selected detail window, actual title ROI, magnifier/close anchors and recognition evidence. No screenshot is persisted to disk.
- `1회 고정밀 스캔`: performs one precision pass even when continuous Scanner is disabled.
- Configurable global one-shot hotkey; default `Ctrl+Shift+F10`, optional disable.

If continuous Scanner/Test mode is active, one-shot scanning cancels the existing runtime loop and awaits its actual completion before sharing capture/OCR/presentation state. The previous continuous mode is restored afterward, subject to the latest user setting.

## Compatibility

```text
Desktop version: 1.2.0
Content schema: v7
Readable Content schemas: v3-v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.1.6 -> v1.2.0 mandatory Game Content update: none
v1.1.6 -> v1.2.0 user.db migration: none
```

## Validation status

Before the final version bump, the integrated recognition branch passed Windows build, 255 automated tests with 0 failure / 0 skip, win-x64 self-contained single-file publish, and the published EXE Product UI / Scanner / Main Map / Factory / MiniMap / graceful-shutdown smoke.

The final v1.2.0 candidate additionally adds published-EXE smoke assertions for Scanner settings schema v3/default hotkey and a synthetic Tarkov inspect-header contract proving that a detected magnifier is excluded from the OCR title ROI.

Final v1.2.0 exact-source release verification, Draft re-download verification, public/latest verification, exact tag verification, and public-downloaded EXE smoke are required before this document can be marked **PUBLIC / VERIFIED**.
