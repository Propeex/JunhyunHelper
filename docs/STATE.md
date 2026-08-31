# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-08-31 KST**  
상태: **v1.13.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품과 운영 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 공개 stable은 v1.13.1이다. v1.13.0에서 추가된 **파밍 가이드 Loadout / Inventory Editor**의 실사용 UI/drag-drop 회귀를 v1.13.1 PATCH에서 수정했고, 구현·검증·병합·공개 릴리즈까지 완료되어 기본 운영 모드는 다시 유지보수다.

현재 진행 중 개발 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

주요 제품 영역:

- GameMode별 Profile / User Progress
- Quest / Hideout / Needed Items / Inventory / cleanup
- Items / Ammo / cross-navigation / profile-aware pickup
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Program Update
- Scanner + Mini Scanner
- Scanner Saved Case / Ground Truth / diagnostics / regression dataset
- Scanner item database / Favorites / Recents
- Farming Guide raid-start Loadout / Inventory Editor
- opt-in PC capture/Scanner 지원 진단

Runtime GPT/AI 의존성은 없다.

## 2. 현재 public stable

```text
version: v1.13.1
exact product release source/tag target:
302f83e88cc65b5fae9b86b5cae294b2586c85a0
PR: #243 — MERGED
validated PR head: 314ce0501c0f680aacb13d2b3c61b20487c4eb15
PR exact-head CI: 33364597514 — SUCCESS
PR exact-head Shutdown Race CI: 33364597501 — SUCCESS
PR exact-head Documentation Consistency: 33364597497 — SUCCESS
exact-main CI: 33364865109 — SUCCESS
exact-main Shutdown Race CI: 33364865123 — SUCCESS
exact-main Documentation Consistency: 33364865134 — SUCCESS
release workflow: 33365070880 — SUCCESS
release id: 379553485
published UTC: 2026-08-31T06:39:45Z
494 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 537579591
bytes: 80,614,695
SHA-256:
d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737

SHA256SUMS.txt
asset id: 537579593
bytes: 86
SHA-256:
14c38f75b70a27d3d6d0ec956404e363dd7d134a6111da3a4b11538a97864e8c
```

Exact-main artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9747973218
archive bytes: 241,778,025
archive SHA-256:
58b38558b33095ddb20ec2e3cdd1ebeea7abb4e9c9c4614ce5d8747927b8e3f6
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.1`, exact-main source가 모두 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

공식 공개 증거:

- `docs/RELEASE_1.13.1.md`
- `docs/.release-v1.13.1-status.json`
- `docs/RELEASE_NOTES_V1.13.1.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

## 3. Farming Guide 제품 의미

파밍 가이드는 Scanner 오른쪽의 first-class section이다.

제품 목적은 **레이드 시작 상태를 제품이 이해할 수 있게 구성하는 Loadout / Inventory Editor**다.

이 UI는 실제 인게임 inventory grid 좌표를 지속적으로 1:1 mirror하는 기능이 아니다. 사용자가 출발 장비, 점유 공간, 보유 아이템, carrier 내부 상태를 구성하면 향후 판단 엔진이 이를 입력으로 사용할 수 있도록 하는 기반이다.

현재 포함하지 않는 것:

- loot 가치 판단
- 무엇을 주울지 추천
- 무엇을 버릴지 추천
- 장비/아이템 교체 추천
- Scanner 실시간 추천 연동
- 실제 raid inventory 좌표의 지속적인 1:1 동기화

이 제품 경계는 `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`가 authority다.

## 4. v1.13.1 Farming Guide UI / interaction 계약

v1.13.1에서 v1.13.0의 텍스트 목록형 회귀를 사용자 의도에 맞는 **아이콘 중심 Tarkov 인벤토리 유사 presentation**으로 바로잡았다.

- 장비는 spatial slot board로 표현한다.
- equipped item은 실제 item icon으로 표시한다.
- Rig / Backpack / Secure Container는 carrier icon target과 실제 내부 grid를 한 영역에서 표현한다.
- storage grid placement도 실제 item icon을 사용한다.
- drag ghost도 실제 item icon을 사용한다.
- `R` 회전 상태는 footprint와 icon layout 모두에 반영한다.
- 90도 회전한 비정사각형 image는 layout-aware rotation을 사용해 swapped footprint 안에서 축소/clip되지 않게 한다.
- valid/invalid target은 transient success/danger presentation을 사용하며 pointer가 벗어나거나 drag가 끝나면 기본 border로 복원한다.
- preset save affordance와 search input은 일반 WPF layout에서 clipping되지 않아야 한다.

Drag target 판정은 WPF mouse capture만 신뢰하지 않는다.

- 실제 RootGrid 좌표와 rendered target geometry를 이용해 equipment/carrier/grid target을 찾는다.
- geometry fallback은 target 자체와 clipping ancestor의 visible bounds를 존중한다.
- ScrollViewer / ScrollContentPresenter 밖으로 잘린 offscreen target은 선택하지 않는다.
- grid 인접 snap tolerance는 유지하되 ancestor viewport clipping은 우회하지 않는다.
- mouse-up 시 마지막 move의 cached target을 신뢰하지 않고 실제 release 좌표에서 probe를 다시 계산한다.

## 5. Farming Guide equipment / storage 모델

Equipment는 현재 제품 설계에 필요한 Tarkov raid-start 장비 슬롯을 표현한다.

예:

- headset
- helmet/headwear
- face cover / eyewear
- body armor / armored rig
- armband
- weapon 1 / weapon 2
- sidearm
- melee

Melee와 PMC dogtag는 레이드마다 반복 입력하는 대상이 아니므로 per-profile preset과 분리된 fixed setting이다.

Storage는 다음 구조를 표현한다.

- Pocket
- Rig
- Backpack
- Secure Container
- Special Slot

Carrier의 실제 grid 구성은 current validated Game Content의 item structure에서 읽는다. 모든 grid cell은 동일한 화면 단위를 사용하고 item은 실제 Tarkov `width × height`에 맞춰 렌더링한다.

## 6. Farming Guide drag / placement 계약

새 아이템 추가는 오른쪽 검색 결과에서 시작한다.

- drag preview는 실제 footprint를 사용한다.
- drag 중 `R`로 90도 회전한다.
- grid 주변에는 bounded snap tolerance를 사용한다.
- bounds / overlap / current filter / contiguous-space를 검증한다.
- 유효/불가 상태를 시각적으로 구분한다.
- 명백한 빈 영역 drop은 기존 배치 item 제거 의미를 가질 수 있다.

Carrier contents 보호:

- contents가 있는 carrier를 다른 carrier로 묵시적으로 덮어써서 내부 상태를 잃게 하지 않는다.
- 현재 모델에서 안전한 이동 의미를 증명할 수 없으면 destructive replacement를 fail closed한다.

Persisted state sanitization:

- Tarkov 업데이트로 grid가 사라짐
- grid 크기가 줄어 out-of-bounds가 됨
- 서로 overlap하게 됨
- current filter를 위반함

위 경우 impossible placement를 그대로 복원하지 않고 current content 기준으로 제거/정리한다. 오래된 preset 때문에 불가능한 editor state를 생성하지 않는다.

## 7. Farming Guide preset / persistence

Preset은 전체 raid-start working state를 보존한다.

포함:

- equipped item
- carrier
- attachment
- armor plate
- stored item
- grid id / row / column
- rotation

저장된 preset을 선택하면 전체 상태를 복원한다. 불러온 상태를 수정하면 원본 preset 선택 상태를 해제한다.

Melee / PMC dogtag fixed setting은 preset과 분리한다.

사용자 상태 authority:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema: v1
```

Game Content와 사용자 Farming Guide state를 분리한다. Program Update / Game Content Update가 Farming Guide user state를 덮어쓰지 않는다.

## 8. Farming Guide Game Content 구조

v1.13.0부터 current validated item source에서 다음 optional structure를 canonical content에 보존한다.

- item width / height
- storage grids
- grid allowed/blocked filters
- equipment / attachment slots
- armor plate slots
- item conflicts
- headphone-blocking 등 현재 editor compatibility에 필요한 구조

이 확장으로 Content write schema는 v9다. 이전 offline snapshot compatibility를 유지하기 위해 v3~v9를 읽는다.

Game Content field가 없거나 source 구조를 importer가 이해하지 못하면 해당 구조를 추측하지 않는다.

## 9. Scanner 유지 계약

- false positive보다 miss를 선호한다.
- OCR/matcher/candidate/recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- recognition proof에 price/needed/source/relationship metadata를 사용하지 않는다.
- scan-time network I/O를 proof에 추가하지 않는다.
- recognition은 external screen pixels + OCR만 사용한다.

사용하지 않음:

- game process memory read
- code/DLL injection
- process/game hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

Correction hotkey는 evidence-only Saved Case를 저장하고 Ground Truth를 자동 생성/추측하지 않는다.

## 10. Quest / Needed Items 계약

- exact ProfileVariable 값은 항상 최우선이다.
- audited staged task-pool compatibility는 current structure가 확인되는 범위에서만 사용한다.
- current trader LL이 audited stage보다 낮으면 잠금 의미를 유지한다.
- current stage는 보수적 reconstruction / fail-closed를 유지한다.
- current trader LL이 audited stage보다 높으면 과거 stage threshold가 충족됐다는 runtime-only effective floor를 사용할 수 있다.
- 이 floor를 hidden server counter의 exact fact로 저장하지 않는다.
- structural drift는 fail closed한다.
- Future Needed Items / cleanup은 current Quest UI compatibility를 낙관적으로 전파하지 않는다.

Scanner 필요 수량/source는 `ItemsWorkspace.Plan.NeededItems` authority를 사용한다.

## 11. Hideout / Ammo 계약

Hideout:

- source `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.

Ammo:

- pickup 판단은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- flea/barter/craft/higher trader LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않는다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선한다.

## 12. Map / MiniMap 계약

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper first-party bridge가 selection/lifecycle/presentation 의미를 소유한다.

- Main Map selection은 fresh/reused MiniMap에 동기화된다.
- player heading은 position과 동일한 map별 affine transform 좌표계를 사용한다.
- PMC / Scav / Transit extract filter와 실제 rendered marker를 검증한다.
- loaded marker data는 있는데 standard layer만 비는 bounded empty-layer race는 direct recovery한다.
- Player Marker Size 변경은 unrelated Map/MiniMap presentation setting을 재초기화하지 않는다.
- Mini Scanner 우클릭 correction context menu는 제거 상태를 유지한다.

## 13. Game Content / Program Update 계약

Game Content:

```text
remote source
→ parse/import
→ semantic/schema validation
→ candidate content
→ completeness/LKG guard
→ candidate DB
→ SQLite read-back/integrity
→ atomic active replacement
```

- candidate 완성 전 active overwrite 금지
- failed candidate 폐기
- healthy Last Known Good 유지
- suspicious shrink는 baseline-relative completeness guard로 차단
- collection/schema drift를 이해하지 못하면 fail closed
- Wiki Ballistics enrichment는 fail-soft
- User Progress / Farming Guide state를 덮어쓰지 않음

Program Update:

- GitHub latest public stable release 사용
- 사용자 동의 없이 program files 자동 교체 금지
- stable ZIP + checksum 검증
- release workflow는 exact-main CI artifact 사용
- 이미 공개된 stable release asset은 immutable historical product identity
- 동일 version의 후속 docs-only main build가 다른 commit metadata를 갖더라도 기존 stable release를 교체하지 않음

## 14. 김태영 PC 진단 계약

현재 authority:

- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

정상 성공 경로:

```text
프로필 이미지 클릭
→ “혹시 김태영 본인?”
→ 예
→ indeterminate progress
→ Desktop diagnostic ZIP 생성
→ “진단 완료.”
→ “파일을 hyune4784@naver.com 으로 보내주세요.”
→ 기본 브라우저에서 https://mail.naver.com/v2/new 열기
```

ZIP은 자동 업로드/첨부/발송하지 않는다. diagnostic evidence는 display/GPU/HDR/capture/Scanner 관련 allowlist 정보만 수집하고 불필요한 식별/credential 정보를 제외한다.

사용자 노트북의 실제 v1.12.0 diagnostic ZIP에서는 exporter 정상 동작을 확인했다. 김태영 PC 원인 판정은 김태영 실제 PC evidence가 들어온 뒤 수행한다.

## 15. Schema / compatibility

```text
Desktop version: 1.13.1
Content schema write: v9
Readable Content schemas: v3, v4, v5, v6, v7, v8, v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1, v2, v3, v4
```

v1.13.0 → v1.13.1:

- user.db mandatory migration: none
- Scanner settings mandatory migration: none
- Farming Guide state migration: none
- Game Content schema change: none

## 16. v1.13.1 검증

Exact product source `302f83e88cc65b5fae9b86b5cae294b2586c85a0`은 다음을 통과했다.

- 494 deterministic tests
- Windows Release build
- Windows x64 self-contained single-file publish
- ProductVersion `1.13.1+302f83e88cc65b5fae9b86b5cae294b2586c85a0` identity 확인
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- ZIP checksum manifest / actual hash equality
- exact-main Documentation Consistency
- exact-main Actions artifact upload
- automatic verified Release workflow
- public tag / latest release / assets readback
- GitHub public asset digest readback

Exact-main package:

```text
Junhyun-Helper.zip
80,614,695 bytes
d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737
```

Release workflow는 exact-main artifact `9747973218`을 사용했고, CI artifact archive digest는 `58b38558b33095ddb20ec2e3cdd1ebeea7abb4e9c9c4614ce5d8747927b8e3f6`이다. 공개 ZIP은 GitHub release asset digest와 exact-main package checksum이 동일하다.

## 17. PR / review 운영 기록

v1.13.1 실사용 회귀 수정 PR은 #243이다.

최종 검토에서 다음 두 추가 결함을 merge 전에 발견하고 수정했다.

- ScrollViewer 밖으로 잘린 offscreen drop target을 geometry fallback이 선택할 수 있는 문제
- `RenderTransform` 기반 90도 회전에서 비정사각형 item icon이 footprint 안에서 축소/clip될 수 있는 문제

각각 ancestor visible-bounds 검증과 layout-aware rotation으로 수정했고, mouse-up actual-coordinate reprobe까지 보강한 뒤 exact-head CI를 다시 통과시켰다. 알려진 release-blocking review thread는 모두 해결 후 merge했다.

## 18. 사용자 실사용 / 다음 작업

자동화와 published EXE smoke는 모두 완료됐다. 다음 외부 evidence는 release 완료 조건과 별개로 **PENDING**이다.

- 사용자의 실제 PC/Tarkov에서 v1.13.1 최종 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집/분석

실사용에서 회귀가 보고되면 자동화 테스트보다 높은 우선순위의 회귀 evidence로 취급한다.

현재 남은 릴리즈 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

새 사용자 요구사항, 실사용 회귀, Tarkov 변화, reviewed Scanner Ground Truth, 또는 김태영 실제 diagnostic evidence가 들어오면 v1.13.1 public stable을 기준으로 필요한 범위만 분석·수정한다.

후속 documentation-only commit은 v1.13.1 제품 릴리즈 source가 아니다. historical identity는 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 고정한다.
