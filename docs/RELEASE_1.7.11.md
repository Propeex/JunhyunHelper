# 준현 헬퍼 v1.7.11 — verified release record

기준일: 2026-08-27
상태: **PUBLIC STABLE / VERIFIED**

## 목적

v1.7.11은 v1.7.10 공개 안정판 이후 확인된 표시·입력·MiniMap 사용성 문제를 수정한 patch-level maintenance release다.

이 릴리즈는 Scanner Item identity recognition의 threshold, candidate cap, matcher, visual recovery acceptance 또는 200 ms observation target을 변경하지 않는다.

## 제품 변경

1. Scanner / Mini Scanner `필요 개수`
   - 전체 요구량 `RequiredTotal` 대신 canonical `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`을 표시한다.
   - 현재 Inventory와 FIR 조건을 반영한 기존 Needed Items 계산 결과를 그대로 사용한다.
   - Item ID 확정 전에는 이 값을 읽거나 identity evidence로 사용하지 않는다.
2. Configurable Map / Scanner hotkey
   - 등록한 Ctrl/Alt/Shift는 모두 필요하다.
   - 등록하지 않은 Ctrl/Alt/Shift가 추가로 눌려도 compatible로 본다.
   - 같은 primary key에 여러 compatible binding이 있으면 required modifier 수가 많은 더 구체적인 binding을 우선한다.
   - Windows modifier는 지원하지 않는다.
   - Map bare NumPad0~5 직접 층 선택은 유지한다.
3. MiniMap first-open synchronization
   - MiniMap 첫 표시 전에 현재 Main Map UI 선택을 shared `MapTrackerService`에 동기화한다.
4. MiniMap window size persistence
   - width/height를 `%LocalAppData%/JunhyunHelper/minimap-window-state.json`에 저장하고 재시작 뒤 복원한다.
   - donor가 정의한 안전 범위로 clamp한다.
5. Standard WPF ToolTip removal
   - 설명용 standard WPF ToolTip은 제품 전역에서 열리지 않는다.
   - 지도 marker detail 같은 기능성 custom Popup은 유지한다.

공식 제품 결정: `docs/DECISION_V1.7.11_MAINTENANCE.md`.

## Scanner 안전 계약

유지한 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

또한 false positive보다 miss 선호, stale/cross-frame identity proof 금지, Item ID 확정 전 mapped metadata identity 사용 금지, scan-time network identity work 금지, matcher/visual recovery acceptance 완화 금지를 유지한다.

## PR 검증

PR #194 final head:

```text
4351670d378fedf7000ada4d613bf1527e203a16
```

PR CI:

```text
run: 33032104032
CI number: #1994
result: SUCCESS
392 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

## exact product release source

PR #194 merge/main release source:

```text
0f97c6e5340ae91581a9242ec236bbd7885b34d5
```

이 커밋이 v1.7.11 tag의 exact target이며 v1.7.11 ProductVersion metadata에도 이 SHA가 포함된다. 이후 documentation-only main commit은 제품 릴리즈 소스로 해석하지 않는다.

## main CI

```text
run: 33033282963
CI number: #1995
result: SUCCESS
392 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

Published ProductVersion:

```text
1.7.11+0f97c6e5340ae91581a9242ec236bbd7885b34d5
```

Main-CI release package:

```text
name: Junhyun-Helper.zip
bytes: 80,477,565
SHA-256: f1ad15debc29b7a167a13448c8df65785f57139a91d8b5d246205a14f9a5800d
```

## Release workflow

```text
run: 33033434877
Release number: #36
result: SUCCESS
```

Release workflow는 exact main CI artifact를 내려받아 ProductVersion, FIRST_RUN identity, package checksum을 다시 검증한 뒤 stable release를 게시했다.

## public release readback

GitHub `/releases/latest` 및 tag readback:

```text
release id: 377531277
tag: v1.7.11
name: 준현 헬퍼 v1.7.11
target commitish: 0f97c6e5340ae91581a9242ec236bbd7885b34d5
draft: false
prerelease: false
latest stable: true
published at UTC: 2026-08-27T02:30:01Z
```

Public asset:

```text
name: Junhyun-Helper.zip
asset id: 531635485
bytes: 80,477,565
GitHub asset digest: sha256:f1ad15debc29b7a167a13448c8df65785f57139a91d8b5d246205a14f9a5800d
```

이 공개 asset digest는 exact main CI가 생성·검증한 package SHA-256과 일치한다.

Checksum asset:

```text
name: SHA256SUMS.txt
asset id: 531635486
bytes: 86
GitHub asset digest: sha256:ccf9adf714298341adf87caeafa3c082e571646c00a720e27f6bcffa32484b67
```

현재 도구 세션에서는 public binary asset을 독립 anonymous client로 다시 내려받아 byte-level 재검증하는 기능이 제공되지 않았다. 따라서 이 문서는 실제로 수행한 검증인 exact main-CI package verification, Release workflow verification, public GitHub metadata/digest readback의 일치만 기록하며 수행하지 않은 anonymous redownload를 주장하지 않는다. 제품의 release quality gate 자체는 독립 public redownload 검증 요구를 유지한다.

## 관련 문서

- `docs/DECISION_V1.7.11_MAINTENANCE.md`
- `docs/RELEASE_NOTES_V1.7.11.md`
- `docs/.release-v1.7.11-status.json`
- `docs/SCANNER.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`