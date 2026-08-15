# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.5 RELEASE CANDIDATE — Main Map 타층 일반 marker 소실 회귀 패치 / Windows x64**

현재 공개 릴리즈는 **v0.1.4**입니다. v0.1.5는 v0.1.4 실사용에서 확인된 `다른 층 일반 marker가 잠깐 보인 뒤 깜박이며 사라짐` 회귀만 우선 수정하는 패치입니다.

```text
feature PR: #82 OPEN
branch: agent/fix-off-floor-marker-flicker-2026-08-15
Desktop ProductVersion: 0.1.5
Content schema: unchanged (v5)
user.db schema: unchanged
required data update from v0.1.4: none
public v0.1.5 release: PENDING final CI + merge + exact-baseline release gate
```

### v0.1.5 원인과 수정

원인은 Main Map standard marker의 cross-floor near-overlap `vertical stack` 최적화였습니다.

```text
같은 marker type
AND 서로 다른 known floor
AND X/Z가 가까움
→ 비대표 marker Canvas.Opacity = 0
```

legacy marker renderer가 marker를 비동기로 순차 추가하기 때문에 초기에는 타층 marker가 보였고, marker tree가 완성된 뒤 위 정리 로직이 실행되면서 `보임 → 깜박임 → 사라짐` 현상이 발생했습니다.

v0.1.5에서는 다음처럼 정정합니다.

- 일반 marker는 서로 다른 floor라는 이유만으로 중복 제거하지 않음
- category ON + current Map이면 각 marker visual 유지
- current/above/below는 초록/빨강/파랑 compact ring + known off-floor 약 75% opacity로만 표현
- cross-floor near-overlap marker에 `Opacity=0`/`Collapsed`를 적용하지 않음
- 실제 같은 물리 항목의 semantic duplicate extract 정규화는 유지
  - 예: Factory `Gate 3` same-name / same normalized floor / near-identical-position PMC+Scav raw rows
- permanent full-tree polling 재도입 없음

### v0.1.5 검증 상태

수정 핵심 head `ea4ccfc6cd25885e302d5d790933ce20f2192cf3`에서 CI run `31861199425`가 성공했습니다.

```text
Desktop Release build: SUCCESS
automated tests: SUCCESS
Windows x64 self-contained single-file publish: SUCCESS
Main Map + MiniMap startup/runtime smoke: SUCCESS
actual MapMarkersContainer off-floor standard-marker async-settle assertion: SUCCESS
Factory Gate 3 / Office Window regression smoke: SUCCESS
floor-hotkey zoom + map-space viewport-center preservation: SUCCESS
normal Main Window close / process exit: SUCCESS
release artifact upload: SUCCESS
```

그 뒤 ProductVersion/배포 문서를 v0.1.5로 정합화했으므로 **최종 PR head CI를 다시 통과한 뒤** 병합합니다. 공개 릴리즈는 병합된 exact baseline에서 release gate를 다시 실행합니다.

상세: `docs/FEEDBACK_2026-08-15_OFF_FLOOR_MARKER_FLICKER.md`, `docs/MAP_PRODUCT_REQUIREMENTS.md`, `docs/DECISIONS.md` DEC-041.

## 현재 공개 릴리즈 — v0.1.4

릴리즈일: **2026-08-15**

```text
release baseline: 68038c6aac43e91f9ba8e810918eed389c753dea
public asset: Junhyun-Helper-v0.1.4-win-x64.zip
public SHA-256: 0238d059f3c714c826c2a962b30e5361b6e3e16c247d2657993a612aed8d8ef9
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.4
```

v0.1.4에서 도입한 핵심은 유지합니다.

- Main Map에서 floor를 visibility filter로 사용하지 않는 제품 정책
- current/above/below 초록/빨강/파랑 compact ring, known off-floor 약 75% opacity
- Factory `Gate 3` 같은 동일 물리 PMC/Scav extract 대표 visual 정규화
- `Office Window` 같은 Scav 회색 body와 floor ring 의미 분리
- Core Indeterminate Quest를 `진행 중`으로 강제하지 않고 UI `확인 필요`로 분리
- `확인 필요`는 Current count/Map Current sidebar에서 제외
- Future Needed Items의 `IndeterminatePotential` 보수 보호 유지

단, v0.1.4의 일반 marker cross-floor vertical-stack representative 예외는 실사용 회귀 때문에 DEC-041 / v0.1.5에서 폐기합니다.

## v0.1.3 변경 — Map/MiniMap 회귀 핫픽스

v0.1.2 실사용 피드백에서 다음 문제가 확인되었습니다.

- 지도 탭 진입 시 새 floor presentation bridge가 200ms마다 Main Map 표준 marker 전체를 UI thread에서 영구 순회
- Quest geometry에 신뢰 가능한 height가 없는데 `FloorId=null`을 `main`으로 간주하여 잘못된 ↑/↓/opacity 적용
- MiniMap에 기존 renderer와 별도 off-floor layer/timer가 중복되어 marker/extract 표시 경로가 경쟁
- MiniMap Raider 등 Junhyun 추가 marker가 floor/marker-scale/core marker reload 뒤 stale floor/scale 상태를 유지할 수 있음
- legacy MiniMap extract refresh가 container를 비운 경우 product renderer가 empty container를 이미 synchronized 된 상태로 오인할 수 있음

수정:

- Main Map 표준 marker: permanent full scan 제거 → marker tree/map/floor 변화 시 one-shot debounce
- Quest floor unknown: `main` 추측 금지, 방향 badge 없음
- known other-floor marker: 당시 약 50% opacity + ↑/↓ 유지; 이후 v0.1.4에서 약 75% + compact ring으로 대체
- MiniMap duplicate off-floor renderer/timer 제거, canonical marker/extract renderer로 통합
- Quest/Raider scale polling timer 제거, `ScaleTransform.Changed`/signature 기반 갱신
- MiniMap Raider floor/zoom/marker-scale/container reload 갱신 보강
- MiniMap extract container child-count transition 시 product cache invalidate → off-floor extract 자동 복구
- v0.1.2 floor-hotkey zoom + map-space viewport center 보존 유지
- release publish 정리 및 exact-baseline gate 정착

상세: `docs/RELEASE_0.1.3.md`, `docs/MAP_PRODUCT_REQUIREMENTS.md`

## Quest / Game Content 기준

Quest를 포함한 Game Content 분류는 `데이터 업데이트` 시 프로그램 importer가 수행합니다. Runtime GPT/AI 의존성은 없습니다.

현재 Quest 정확도 기준:

- `taskRequirements`의 `active / complete / failed` 상태 모델
- Lightkeeper / BTR Driver / Ref 상인 접근 gate 보강
- `globalVariable` / `dialogue` unresolved condition은 `확인 필요`로 보존
- `availableDelaySecondsMin/Max` canonical 보존, 가짜 countdown 없음
- Content snapshot schema **v5**
- `user.db` schema/progress 변경 없음

2026-08-15 live product importer/validator 기준:

```text
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
validation errors: 0
importer warnings: 0
```

## 업그레이드 정책

- **v0.1.4 → v0.1.5:** 필수 `데이터 업데이트` 없음. Map runtime patch이며 Content schema v5 / `user.db` 그대로입니다.
- **v0.1.3 → v0.1.4:** 필수 `데이터 업데이트` 없음.
- **v0.1.2 → v0.1.3:** 필수 `데이터 업데이트` 없음.
- **v0.1.1 → 최신:** 필수 `데이터 업데이트` 없음. Content schema는 v5입니다.
- **v0.1.0 → 최신:** 최신 Quest 판정을 위해 설치 후 `데이터 업데이트`를 한 번 실행합니다.
- `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료 / Inventory / Hideout 진행은 유지됩니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / `확인 필요` availability 분리 적용 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 / flexible status + Item Wiki |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / v0.1.5 off-floor standard-marker regression patch 검증 중 |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- deeper pinned Map renderer refactor only when concrete regression/performance value justifies the risk
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책
