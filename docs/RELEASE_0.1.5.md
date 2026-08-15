# RELEASE 0.1.5 — Map regression patch

기록일: **2026-08-15**

상태: **PUBLIC RELEASE / VERIFIED**

## 목적

v0.1.4 실사용에서 확인된 두 Map 회귀를 수정합니다.

1. Main Map에서 다른 층 일반 마커가 처음에는 보이지만 잠시 깜박인 뒤 완전히 사라짐
2. 층을 변경할 때 MiniMap의 현재 지도 중심이 초기/이전 위치로 돌아감

## 1. 타층 일반 marker 소실

### 원인

`LegacyStandardMarkerFloorPresentationBridge`에 있던 cross-floor near-overlap vertical-stack 로직이 다음 조건의 일반 marker를 대표 하나로 축약했습니다.

```text
same marker type
AND different known floor
AND near X/Z
→ non-representative Canvas.Opacity = 0
```

legacy `MapMarkersManager`는 marker를 비동기로 순차 추가하므로 초기에는 타층 marker가 보이고, marker tree가 완성된 뒤 suppression이 실행되어 사라지는 타이밍이 만들어졌습니다.

### 수정

- 일반 marker cross-floor vertical-stack suppression 제거
- 서로 다른 floor의 same-type/near-XZ marker도 enabled 상태면 모두 visual 유지
- floor 관계는 visibility가 아닌 presentation으로만 표현
  - current: 초록 compact ring + 정상 opacity
  - above: 빨강 compact ring + 약 75% opacity
  - below: 파랑 compact ring + 약 75% opacity
- 타층 일반 marker에 `Opacity=0`/`Collapsed`를 적용하지 않음
- permanent full-tree polling 재도입 없음
- 실제 같은 물리 항목의 semantic duplicate extract 정규화는 유지
  - Factory `Gate 3` PMC/Scav same-physical extract 대표 visual 규칙 유지

## 2. MiniMap floor 변경 시 viewport 초기화

### 원인

제품 MiniMap은 `PlayerTracking` 고정입니다. legacy `CenterOnPlayer()`는 이 모드의 실제 player-centered 위치를 live `MapTranslate.X/Y`에는 반영하지만 persisted `_settings.MapOffsetX/Y`에는 쓰지 않습니다.

기존 floor renderer는 SVG artwork를 교체한 뒤 `UpdateMapView()`를 호출했고, 여기서 stale persisted offset을 다시 `MapTranslate`에 적용하여 사용자가 보고 있던 중심을 초기/이전 위치로 되돌렸습니다.

### 수정

- MiniMap floor 변경 직전 live `MapScale` + `MapTranslate`에서 zoom과 viewport 중앙 map-space X/Y 캡처
- floor render 전에 live transform을 persisted offset에도 동기화하여 중간 점프 방지
- floor SVG render 완료까지 await
- layout 안정 후 동일 zoom + 동일 map-space 중심 복원
- 복원 후 persisted `MapOffsetX/Y`와 live `MapTranslate` 재동기화
- floor up/down product hotkey와 NumPad 0~5 direct floor selection 모두 viewport-safe 경로 사용
- Map 자체 변경이나 새로운 screenshot player position은 정상적인 tracking 이벤트이므로 이 보존 규칙의 대상이 아님

## 직접 회귀 smoke

### Main Map off-floor marker

실제 `MapMarkersContainer`의 standard `MapMarker`를 비동기 로딩과 bounded settle 이후 검사합니다.

```text
현재 multi-floor Map
- standard marker가 실제 container에 생성될 때까지 대기
- async build + bounded settle 완료 구간까지 대기
- known off-floor marker가 하나 이상 존재함을 확인
- 각 known off-floor marker Visibility == Visible
- 각 known off-floor marker Opacity >= 0.70
```

### MiniMap viewport

PlayerTracking에서 실제로 발생한 stale-settings 상태를 runtime에서 직접 재현합니다.

```text
live MapTranslate = 현재 유효한 viewport
settings MapOffsetX/Y = 의도적으로 다른 stale 값
→ product floor 변경
→ floor 실제 변경 확인
→ zoom 동일
→ viewport 중앙 map-space X/Y 동일
→ settings MapOffsetX/Y == live MapTranslate
```

Factory `Gate 3` / `Office Window`, Main Map viewport, MiniMap marker scale, legacy hotkey suppression smoke도 그대로 실행합니다.

## 데이터/업그레이드

```text
Desktop ProductVersion: 0.1.5
Content schema: v5 유지
user.db schema: 변경 없음
v0.1.4 → v0.1.5 필수 데이터 업데이트: 없음
```

프로그램 파일만 v0.1.5로 교체하면 기존 Profile / Quest 완료 / Inventory / Hideout / Map 설정을 그대로 사용합니다.

## Release gate

공개 v0.1.5는 다음을 모두 통과한 뒤에만 생성합니다.

1. 최종 변경 범위 전체 review — release-blocking P1/P2 없음
2. Desktop Release build
3. 전체 automated tests
4. Windows x64 self-contained single-file publish
5. 실제 Main Map standard-marker async-settle regression smoke
6. Factory `Gate 3` / `Office Window` direct regression smoke
7. Main Map floor-hotkey zoom + map-space viewport center 보존
8. MiniMap stale-offset floor viewport direct regression smoke
9. MiniMap marker-scale / floor indicator / legacy hook smoke
10. 정상 Main Window close / process exit
11. ProductVersion `0.1.5`
12. 배포 root 검증
    - `준현 헬퍼.exe`
    - `FIRST_RUN_KO.txt`
    - `Assets/`
    - root DLL 없음
    - PDB 없음
    - nested ZIP 없음
    - runtime `Logs/` 없음
13. `Junhyun-Helper-v0.1.5-win-x64.zip` + `SHA256SUMS.txt` 공개 GitHub Release
14. 공개 asset 재다운로드 후 SHA-256 재검증
15. draft/prerelease가 아닌 정식 공개 상태 확인

## 최종 공개 검증 기록

```text
PR: #82 — MERGED
release baseline: 2ff504c24661b6e37ec40e685dd344ce5581350f
branch CI run: 31863894702 — SUCCESS
main CI run: 31864041783 — SUCCESS
release workflow run: 31864223946 — SUCCESS
Desktop ProductVersion: 0.1.5
automated tests: 177 passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
Main Map off-floor standard-marker async-settle smoke: SUCCESS
Factory Gate 3 / Office Window regression smoke: SUCCESS
Main Map viewport preservation smoke: SUCCESS
MiniMap stale-offset floor viewport preservation smoke: SUCCESS
MiniMap marker-scale/floor runtime smoke: SUCCESS
graceful shutdown: SUCCESS
public asset: Junhyun-Helper-v0.1.5-win-x64.zip
public SHA-256: 565bf0ad01ac9ec8385e99b26aa692e0962550a0c975a889e4b56ad33a6a41f7
public ZIP re-download + SHA-256 verification: SUCCESS
draft: false
prerelease: false
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.5
```

`v0.1.4 → v0.1.5` 업그레이드에는 필수 데이터 업데이트가 없습니다. Content schema v5와 `user.db` schema는 변경하지 않았으며 기존 사용자 진행과 Map 설정을 유지합니다.
