# RELEASE 0.1.5 — Main Map off-floor standard-marker regression patch

기록일: **2026-08-15**

상태: **RELEASE CANDIDATE / PUBLIC RELEASE PENDING**

## 목적

v0.1.4 실사용에서 확인된 다음 회귀를 수정합니다.

> Main Map에서 다른 층 일반 마커가 처음에는 보이지만 잠시 깜박인 뒤 완전히 사라짐.

## 원인

`LegacyStandardMarkerFloorPresentationBridge`에 있던 cross-floor near-overlap vertical-stack 로직이 다음 조건의 일반 marker를 대표 하나로 축약했습니다.

```text
same marker type
AND different known floor
AND near X/Z
→ non-representative Canvas.Opacity = 0
```

legacy `MapMarkersManager`는 marker를 비동기로 순차 추가하므로 초기에는 타층 marker가 보이고, marker tree가 완성된 뒤 suppression이 실행되어 사라지는 타이밍이 만들어졌습니다.

## 수정

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

## 직접 회귀 smoke

기존 synthetic floor helper 검사만 사용하지 않습니다. 실제 Main Map `MapMarkersContainer`의 standard `MapMarker`를 비동기 로딩과 bounded settle 이후 검사합니다.

```text
현재 multi-floor Map
- standard marker가 실제 container에 생성될 때까지 대기
- async build + bounded settle 완료 구간까지 대기
- known off-floor marker가 하나 이상 존재함을 확인
- 각 known off-floor marker Visibility == Visible
- 각 known off-floor marker Opacity >= 0.70
```

기존 Factory / MiniMap / viewport smoke도 그대로 실행합니다.

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
7. MiniMap runtime smoke
8. floor-hotkey zoom + map-space viewport center 보존
9. 정상 Main Window close / process exit
10. ProductVersion `0.1.5`
11. 배포 root 검증
    - `준현 헬퍼.exe`
    - `FIRST_RUN_KO.txt`
    - `Assets/`
    - root DLL 없음
    - PDB 없음
    - nested ZIP 없음
    - runtime `Logs/` 없음
12. `Junhyun-Helper-v0.1.5-win-x64.zip` + `SHA256SUMS.txt` 공개 GitHub Release
13. 공개 asset 재다운로드 후 SHA-256 재검증
14. draft/prerelease가 아닌 정식 공개 상태 확인

## 현재 검증 기록

핵심 수정 head `ea4ccfc6cd25885e302d5d790933ce20f2192cf3` / CI run `31861199425`:

```text
Desktop Release build: SUCCESS
automated tests: SUCCESS
Windows x64 publish: SUCCESS
actual Main Map off-floor standard-marker async-settle smoke: SUCCESS
Factory Main Map regression smoke: SUCCESS
MiniMap runtime smoke: SUCCESS
floor-hotkey viewport preservation: SUCCESS
graceful shutdown: SUCCESS
```

ProductVersion/배포 문서를 v0.1.5로 정합화한 최종 PR head는 다시 CI를 통과해야 합니다.

최종 merge baseline / release workflow run / public SHA-256 / URL은 공개 완료 후 이 문서와 `docs/STATE.md`, `README.md`에 기록합니다.
