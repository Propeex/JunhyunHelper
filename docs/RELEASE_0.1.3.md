# RELEASE 0.1.3 — Map/MiniMap hotfix

기록일: **2026-08-15**

상태: **RELEASED / PUBLIC RELEASE VERIFIED**

## 목적

v0.1.2 실사용에서 확인된 두 가지 사용자 문제를 우선 해결했습니다.

1. 지도 탭 진입 시 프로그램이 멈추려는 것처럼 느껴지는 UI 지연
2. 다른 층 marker가 올바른 층 방향/가시성으로 표시되지 않는 문제

Content schema 또는 User Progress 모델을 변경하는 릴리즈가 아닙니다.

## 원인과 수정

### Main Map UI polling

v0.1.2의 floor direction bridge가 200ms마다 Main Map 표준 marker 전체를 UI thread에서 순회했습니다.

v0.1.3에서는:

```text
permanent 200ms full scan
→ marker tree/map/floor 변화 감지
→ one-shot debounce
→ 필요한 시점에만 floor presentation 적용
```

### Quest floor unknown

온라인 Quest geometry에 신뢰 가능한 height가 없을 때 `FloorId=null`을 `main`으로 간주하던 동작을 제거했습니다.

```text
height 있음    → Floor.Order 기준 current / above / below
height 불명확  → floor unknown, 방향 추측 안 함
```

### MiniMap 중복 floor renderer

기존 MiniMap marker/extract renderer 위에 별도 off-floor layer/timer가 중복으로 존재하던 구조를 제거하고 canonical renderer 한 경로로 통합했습니다.

### Raider / additional marker

MiniMap floor, zoom, marker-scale 또는 core marker container가 바뀐 뒤 Raider가 이전 arrow/opacity/scale을 유지할 수 있던 문제를 수정했습니다. 기존 product pulse에서 signature가 달라졌을 때 dedicated additional-marker renderer가 다시 적용됩니다.

### Extract empty-refresh recovery

legacy marker refresh가 `ExtractMarkersContainer`를 잠시 비우면 빈 collection의 `All(...) == true` 때문에 product renderer가 이를 정상 동기화 상태로 오인할 수 있었습니다.

v0.1.3은 container child-count transition을 O(1)로 감지하여 product extract cache를 invalidate하고 다음 product pulse에서 canonical extract set을 복구합니다.

## 유지되는 제품 계약

- Main Map / MiniMap marker는 다른 층이라는 이유만으로 숨기지 않음
- known above floor → 약 50% opacity + `↑`
- known below floor → 약 50% opacity + `↓`
- unknown floor → 방향 추측 없음
- Main Map과 MiniMap에서 같은 floor 의미 사용
- floor hotkey 전후 zoom과 map-space viewport center 보존
- screenshot은 Map/player tracking에 사용하며 floor 자동 선택에는 사용하지 않음
- MiniMap AutoFloorSelection OFF

## 데이터/업그레이드

```text
Content schema: v5 그대로
user.db schema: 변경 없음
v0.1.2 → v0.1.3 필수 데이터 업데이트: 없음
```

기존 Profile / Quest 완료 / Inventory / Hideout 진행 / Map product settings / Ammo favorites는 유지합니다.

## 최종 검토에서 추가로 발견해 수정한 항목

공개 릴리즈 직전 전체 변경 범위를 다시 검토했고 다음 release-blocking 항목을 추가로 발견해 수정했습니다.

- MiniMap Raider/additional marker가 floor/scale 변경 뒤 stale 상태를 유지할 수 있던 경로
- legacy extract refresh가 컨테이너를 비운 뒤 타층 extract가 자동 복구되지 않을 수 있던 경로
- EXE ProductVersion / FIRST_RUN / README / STATE가 v0.1.3 후보와 불일치하던 릴리즈 메타데이터

모든 PR review thread를 해결한 뒤 release gate를 다시 처음부터 통과시켰습니다.

## 최종 공개 release gate

Exact release baseline:

```text
PR: #79 MERGED
release baseline: 3c49d4ca5af549afb4a4a5ce376cb6f8869709fb
pre-merge final CI: 31834842097 — SUCCESS
release workflow: 31835116544 — SUCCESS
Desktop ProductVersion: 0.1.3+3c49d4ca5af549afb4a4a5ce376cb6f8869709fb
Automated tests: 176 passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
Main Map + MiniMap runtime smoke: SUCCESS
multi-floor SVG switch: SUCCESS
other-floor relation / ↑↓ / opacity smoke: SUCCESS
floor-hotkey zoom + map-space viewport-center preservation: SUCCESS
MiniMap window / zoom / floor / marker-scale smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
package root / PDB / nested ZIP / runtime Logs validation: SUCCESS
```

## 공개 GitHub Release

```text
tag: v0.1.3
title: 준현 헬퍼 v0.1.3
release URL: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.3
target commit: 3c49d4ca5af549afb4a4a5ce376cb6f8869709fb
draft: false
prerelease: false
```

공개 asset은 정확히 두 개입니다.

```text
Junhyun-Helper-v0.1.3-win-x64.zip
SHA256SUMS.txt
```

Windows ZIP:

```text
size: 74,030,429 bytes
SHA-256: 41e674d0186846076e62a1edd92c1a5ac9849f53ab48bbedeb2a6a00101f6941
```

Release workflow가 GitHub에 게시된 ZIP과 `SHA256SUMS.txt`를 다시 다운로드하여 생성 직후 hash, manifest hash, 공개 다운로드 hash가 모두 같은지 확인했습니다.

```text
VERIFIED_RELEASE_SHA256=41e674d0186846076e62a1edd92c1a5ac9849f53ab48bbedeb2a6a00101f6941
```

릴리즈 게시에 사용한 일회성 workflow는 성공 후 저장소에서 제거했습니다.

## 결론

**v0.1.3은 RELEASED / PUBLIC RELEASE VERIFIED 상태이며 현재 알려진 기능 또는 패키징 blocker는 없습니다.**
