# 준현 헬퍼 v1.7.10 — verified release record

기준일: 2026-08-26
상태: **PUBLIC STABLE / VERIFIED**

## 목적

v1.7.10은 특정 사용자 PC에 맞춘 Scanner 튜닝이 아니라 공개 배포 환경에서의 cross-environment robustness를 강화한 유지보수 PATCH다.

핵심 변경은 item-title OCR 입력의 luminance 환경 차이를 조건부로 정규화하는 계층을 추가한 것이다. 정상 OCR 성공 경로와 semantic/catalog acceptance는 그대로 유지한다.

## 구현 계약

```text
proven normal OCR
→ text 있음: 기존 결과 즉시 사용
→ text 없음: title luminance profile 분석
    → reference/flat profile: 기존 경로 유지
    → lifted/washed/low-contrast profile: normalized auxiliary OCR
→ existing bounded deep OCR
    → environment abnormal일 때만 normalized auxiliary evidence 추가
→ existing conservative catalog matching
→ Item ID or fail closed
```

환경 정규화는 Item identity proof가 아니다. 새 OCR evidence도 기존 official Korean Tarkov item catalog의 동일한 matcher/ambiguity 기준을 통과해야 한다.

유지한 안전 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

또한 matcher/visual acceptance, stale/cross-frame identity 금지, scan-time network 금지, game memory read/DLL injection/packet interception/process hook 금지는 변경하지 않았다.

## 환경 회귀 검증

새 deterministic procedural regression은 다음을 포함한다.

- reference SDR-like luminance
- HDR→SDR-like lifted/washed luminance
- lifted + compressed contrast
- low-contrast gamma/rendering variation
- 1080p / 1440p / 4K proportional title raster
- effectively flat/no-contrast negative input

flat/no-contrast 입력은 임의 contrast를 생성하지 않고 fail closed한다.

## PR 검증

PR #192 final head:

```text
322c2e4e1dd641905411cc10fb9a81ba22816d33
```

PR CI:

```text
run: 32981693237
CI number: #1982
result: SUCCESS
389 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

## exact product release source

PR #192 merge/main release source:

```text
a557daad5b37aca11a189524ecf256564d2b8ea4
```

이 커밋이 v1.7.10 tag의 exact target이다. 이후 문서 동기화 커밋은 제품 릴리즈 소스로 해석하지 않는다.

## main CI

```text
run: 32983155982
CI number: #1983
result: SUCCESS
389 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

## Release workflow

```text
run: 32983498402
Release number: #34
result: SUCCESS
```

Release workflow는 위 main CI가 생성한 verified Windows artifact를 내려받아 version/checksum을 다시 검증한 뒤 stable release를 게시했다.

## public release readback

GitHub `/releases/latest` 검증 결과:

```text
release id: 377231814
tag: v1.7.10
name: 준현 헬퍼 v1.7.10
target commitish: a557daad5b37aca11a189524ecf256564d2b8ea4
draft: false
prerelease: false
latest stable: true
published at UTC: 2026-08-26T14:59:06Z
```

Public asset:

```text
name: Junhyun-Helper.zip
asset id: 530959212
bytes: 80,471,678
SHA-256: 6d4f3f8580318d05361cd4d62bf265c4590532722df22dc8b8d734fe8ec10eb9
```

Checksum asset:

```text
name: SHA256SUMS.txt
asset id: 530959213
bytes: 86
SHA-256: c8e4923dd6a0dd2b13c45b2b63a8821bffdc5b40646573c30cfc270bdee8c095
```

## 관련 문서

- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`
- `docs/RELEASE_NOTES_V1.7.10.md`
- `docs/.release-v1.7.10-status.json`
- `docs/SCANNER.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
