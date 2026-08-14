# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.3 RELEASE CANDIDATE — Map/MiniMap hotfix / Windows x64**

현재 공개 릴리즈는 **v0.1.2**이며 v0.1.3은 PR #79에서 최종 release gate를 진행 중입니다.

```text
hotfix PR: #79 OPEN
branch: agent/map-floor-performance-hotfix-2026-08-15
latest functional RC code: 35485f1507b4b3424253b6752660c6a36447d42b
RC verification run: 31834407168 — SUCCESS
Desktop ProductVersion: 0.1.3
Content schema: unchanged (v5)
user.db schema: unchanged
required data update from v0.1.2: none
public v0.1.3 release: PENDING final merge + exact-baseline release workflow
```

현재 확인된 **기능/패키징 blocker는 없습니다.** 남은 release gate는 최신 문서 정합성 확인, PR 최종 review thread 정리, 병합, exact merged baseline 재검증과 공개 GitHub Release 무결성 확인입니다.

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
- known other-floor marker: `Floor.Order` 기준 약 50% opacity + ↑/↓ 유지
- MiniMap duplicate off-floor renderer/timer 제거, canonical marker/extract renderer로 통합
- Quest/Raider scale polling timer 제거, `ScaleTransform.Changed`/signature 기반 갱신
- MiniMap Raider floor/zoom/marker-scale/container reload 갱신 보강
- MiniMap extract container child-count transition 시 product cache invalidate → off-floor extract 자동 복구
- v0.1.2 floor-hotkey zoom + map-space viewport center 보존 유지
- EXE ProductVersion / FIRST_RUN을 v0.1.3으로 정합화
- 모든 향후 릴리즈에 `최종 전체 검토 → 문제 수정 → release gate 재실행 → GitHub Release` 절차를 `AGENTS.md`에 공식화

상세: `docs/RELEASE_0.1.3.md`, `docs/MAP_PRODUCT_REQUIREMENTS.md`

## v0.1.3 release-candidate 검증

최신 기능 코드 `35485f1507b4b3424253b6752660c6a36447d42b` 기준:

```text
CI run: 31834407168 — SUCCESS
Desktop Release build: SUCCESS
automated tests: 176 passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
Main Map + MiniMap startup/runtime smoke: SUCCESS
multi-floor SVG switch: SUCCESS
other-floor relation / ↑↓ / opacity smoke: SUCCESS
floor-hotkey zoom + map-space viewport-center preservation: SUCCESS
MiniMap window / zoom / floor / marker-scale smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
```

Codex 최종 review에서 기능 관련 P1/P2 지적은 수정했습니다.

- body-only opacity 실험과 smoke 불일치 → 최종 구현은 root 50% opacity 계약으로 복귀, smoke와 일치
- Raider floor/scale stale 상태 → product pulse + dedicated renderer 연동으로 수정
- empty legacy extract refresh → child-count transition cache invalidation으로 수정
- release candidate 문서가 v0.1.2에 머물던 문제 → README/STATE/RELEASE_0.1.3 갱신

최종 공개 릴리즈에서는 merged release baseline을 다시 build/test/publish/smoke하고 public asset을 재다운로드하여 SHA-256까지 검증해야 합니다.

## 이전 공개 릴리즈 — v0.1.2

릴리즈일: **2026-08-15**

```text
feature PR: #77 MERGED
CI stabilization PR: #78 MERGED
release baseline: b974d942dbddf09ebe91c6c2af337b66ae1e1ba0
main verification run: 31829061453 — SUCCESS
release workflow run: 31829344223 — SUCCESS
public asset: Junhyun-Helper-v0.1.2-win-x64.zip
public SHA-256: 163a2a33184a6f5d8abcefa542239cd2f29a686d924cf4d784081c47939398ab
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.2
```

## Quest / Game Content 기준

Quest를 포함한 Game Content 분류는 `데이터 업데이트` 시 프로그램 importer가 수행합니다. Runtime GPT/AI 의존성은 없습니다.

v0.1.1에서 도입한 Quest 정확도 기준은 유지됩니다.

- `taskRequirements`의 `active / complete / failed` 상태 모델
- Lightkeeper / BTR Driver / Ref 상인 접근 gate 보강
- `globalVariable` / `dialogue` unresolved condition은 `판정 문제`에 보존
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

- **v0.1.2 → v0.1.3:** 필수 `데이터 업데이트` 없음. Map runtime hotfix이며 Content schema v5 / `user.db` 그대로입니다.
- **v0.1.1 → v0.1.3:** 필수 `데이터 업데이트` 없음. Content schema는 v5입니다.
- **v0.1.0 → v0.1.3:** 최신 Quest 판정을 위해 설치 후 `데이터 업데이트`를 한 번 실행합니다.
- `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료 / Inventory / Hideout 진행은 유지됩니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / v0.1.1 live prerequisite audit 유지 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 / v0.1.2 flexible status + Item Wiki |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / v0.1.3 release-candidate hotfix 검증 통과 |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

## 현재 공개 릴리즈

```text
v0.1.2
https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.2
asset: Junhyun-Helper-v0.1.2-win-x64.zip
SHA-256: 163a2a33184a6f5d8abcefa542239cd2f29a686d924cf4d784081c47939398ab
```

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- deeper pinned Map renderer refactor only when concrete regression/performance value justifies the risk
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책
