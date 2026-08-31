# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 프로젝트의 기준입니다.

기준일: **2026-08-31 KST**  
상태: **v1.12.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품과 운영 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정된 제품 요구사항 범위와 Scanner 기능은 완성 상태이며 기본 운영 모드는 유지보수다.

현재 진행 중 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

주요 제품 영역:

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / Ammo / cross-navigation
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- 사용자 동의형 Program Update
- Scanner + Mini Scanner
- Scanner Saved Case / Ground Truth / diagnostics / regression dataset
- Scanner 아이템 정보 DB / Favorites / Recents
- opt-in PC capture/Scanner 지원 진단

Runtime GPT/AI 의존성은 없다.

## 2. 현재 public stable

```text
version: v1.12.0
exact product release source/tag target:
b2fcec460df256c581e87b53c6293dc4d2177b9c
final PR: #238 — MERGED
superseded draft PR: #237 — CLOSED / NOT MERGED
validated feature head: 5216ab410c8a4384aee7d9f1a69fbd30302ad0a8
feature-head CI: 33348681591 — SUCCESS
feature-head Shutdown Race CI: 33348681589 — SUCCESS
feature-head Documentation Consistency: 33348681555 — SUCCESS
exact-main CI: 33348916340 — SUCCESS
exact-main Shutdown Race CI: 33348916440 — SUCCESS
exact-main Documentation Consistency: 33348916365 — SUCCESS
release workflow: 33349066686 — SUCCESS
release id: 379463868
published UTC: 2026-08-31T01:56:23Z
482 passed / 0 failed
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 537304923
bytes: 80,572,903
SHA-256 / GitHub asset digest:
d8ad140ee39ef533471a229ae01e80bc4ad7baeb5b513490c645bdbd3af137c0

SHA256SUMS.txt
asset id: 537304924
bytes: 86
SHA-256 / GitHub asset digest:
76a0dfb4e7734001a938798c2f6180f815d79b914e7d2b3933423f1f827673d7
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9742966369
archive bytes: 241,651,154
archive SHA-256:
c6122103fefa1c0b5ffd30787a4a60f6af1e151c3dd4694dca3584c7081145e9
```

GitHub `/releases/latest`, release `target_commitish`, lightweight `refs/tags/v1.12.0`, exact-main product source가 모두 `b2fcec460df256c581e87b53c6293dc4d2177b9c`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다. Release workflow는 exact-main CI artifact에서 package manifest/actual hash를 검증했고 공개 ZIP metadata digest도 같은 SHA-256을 보고한다.

공식 공개 증거:

- `docs/RELEASE_1.12.0.md`
- `docs/.release-v1.12.0-status.json`
- `docs/RELEASE_NOTES_V1.12.0.md`
- `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`
- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

후속 documentation-only commit은 v1.12.0 제품 릴리즈 소스가 아니다. 공개 source/tag/assets는 위 exact source를 immutable historical identity로 사용한다.

## 3. v1.12.0 — Quest staged task-pool availability

### 3.1 사용자 증상

새 프로필에서는 `확인 필요`가 0개였지만 Quest/Trader 진행 후 수십 개가 `확인 필요`로 증가했다. 사용자 캡처에서는 49개가 표시됐다.

current EFT 1.1 audit의 LL1 staged task-pool Quest는 정확히 48개다. 기존 compatibility는 LL1 trader에서 첫 Quest completion이 생긴 뒤 hidden pool variable의 exact write semantics를 알 수 없으면 그 pool을 unknown으로 유지했고, trader가 LL2 이상으로 올라간 뒤에도 과거 LL1 pool을 다시 확정하지 않았다.

### 3.2 현재 계약

1. exact `GameProfileSnapshot.ProfileVariables` 값이 있으면 항상 최우선이다.
2. current trader LL이 audited pool stage보다 낮으면 effective availability value는 0이다.
3. current trader LL이 pool stage와 같으면 기존 current-stage 보수적 reconstruction / fail-closed를 유지한다.
4. current trader LL이 audited pool stage보다 높으면 해당 **과거 stage의 `max(audited thresholds)`를 runtime-only availability floor**로 사용한다.
5. 이 floor는 숨은 server counter의 exact 값을 복원하거나 `user.db`에 저장하는 값이 아니다.
6. audited variable/trader/pool membership/threshold/required shape가 drift하면 synthetic value를 만들지 않는다.
7. Future Needed Items / cleanup은 이 current-UI compatibility를 사용하지 않고 conservative future reachability를 유지한다.

결정적 회귀 테스트는 LL1→LL2, LL2→LL3 past-stage satisfaction, exact-value precedence, current-stage conservative behavior, structural drift fail-closed를 고정한다.

상세 결정은 `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`의 2026-08-31 refinement를 따른다.

## 4. v1.12.0 — Hideout 검색 clear 정렬

공통 `ProductSearchClearButtonBehavior`의 clear `×`는 TextBox template child가 아니라 parent Grid의 sibling overlay다. Hideout 검색창만 TextBox 자체의 bottom margin으로 row spacing을 가지고 있어 기존 Right-only margin compensation에서는 glyph가 아래로 어긋났다.

현재 clear button은 SearchBox의 Left/Top/Right/Bottom outer margin을 모두 반영해 actual input rectangle 기준으로 정렬한다. Items/Quest/Scanner 등의 기존 clear interaction 의미는 변경하지 않는다.

## 5. v1.12.0 — 김태영 PC 진단

### 5.1 제품 흐름

```text
메인 헤더 좌측 프로필 이미지 클릭
→ “김태영 본인이 맞습니까?”
→ 예
→ local display/capture/Scanner diagnostic
→ Desktop ZIP 생성
→ hyune4784@naver.com 으로 전달 안내
```

- 아니오는 아무 작업도 하지 않는다.
- 자동 업로드/자동 이메일 전송은 하지 않는다.
- 실행 전 화면 capture evidence가 포함될 수 있음을 알린다.

### 5.2 evidence 범위

- Windows/runtime/process architecture
- display count/bounds/working area/primary/BPP/virtual screen/system DPI
- GPU model/driver/date/current mode/monitor state
- dxdiag allowlist의 HDR support, display color space, primaries, luminance, mode/driver/display fields
- Discord/OBS/NVIDIA/AMD/RTSS/Game Bar/SteelSeries/Medal/Overwolf/Lossless Scaling/Tarkov 등 allowlisted process 존재와 가능한 version
- Scanner settings/runtime/capture mode/catalog state
- 기존 Scanner support/performance/log bundle
- 각 display screen-copy + RGB/luminance/highlight clipping/near-black stats
- Tarkov window가 있으면 exact client screen-copy + PrintWindow 비교와 동일 stats

### 5.3 privacy / failure contract

수집하지 않음:

- Windows 사용자 이름
- 컴퓨터 이름
- IP/MAC
- 네트워크 목록
- credential/token/password
- 환경변수 전체 dump
- 임의 전체 process list
- application install path

화면 PNG 자체에는 실행 당시 보이는 내용이 포함될 수 있다. optional probe는 fail-soft이며 실패한 probe는 `probe-errors.txt`에 기록한다. ZIP 작성 자체가 불가능할 때만 전체 진단 실패로 처리한다.

상세 결정은 `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`를 따른다.

## 6. Scanner 유지 계약

- false positive보다 miss를 선호한다.
- OCR/matcher/candidate/recovery acceptance threshold는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- price/needed/source/relationship metadata를 Item ID proof에 사용하지 않는다.
- scan-time 외부 network I/O를 recognition proof에 추가하지 않는다.
- recognition은 external screen pixels + OCR만 사용한다.
- game process memory read / code injection / process hook / kernel/driver 접근 / input automation / game network manipulation / anti-cheat bypass를 사용하지 않는다.

Scanner current needed authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner display settings schema는 v9다. correction hotkey는 evidence-only Saved Case를 저장하고 Ground Truth를 자동 생성·추측하지 않는다.

## 7. Map / MiniMap 유지 계약

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- Main Map selection은 product tracker/registry desired map state를 소유한다.
- MiniMap fresh first-create와 reused window는 현재 selected map을 사용한다.
- marker/name presentation은 donor visual rebuild 뒤 product settings를 다시 적용한다.
- standard marker direct recovery는 loaded data + unexpected empty layer 조건에서만 수행한다.
- Player Marker Size는 player marker scale에만 격리한다.

## 8. Hideout / Needed Items / Ammo 유지 계약

- Hideout source `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory를 충당하지 않는다.
- Needed Items cleanup safety를 증명할 수 없으면 정리 가능으로 처리하지 않는다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- flea/barter/craft/higher LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않는다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선한다.

## 9. Game Content / Program Update 유지 계약

Game Content:

- candidate download/build
- schema/completeness/integrity validation
- validated active 승격
- Last Known Good 보존
- 검증 실패 시 기존 정상 데이터 유지

Program Update / Release:

- GitHub public stable release를 확인한다.
- 사용자 동의 없이 program files를 자동 교체하지 않는다.
- stable ZIP + checksum contract를 사용한다.
- release workflow는 exact-main CI artifact를 사용한다.
- 이미 공개한 stable release는 immutable하게 취급한다.

## 10. Schema / compatibility

```text
Desktop version: 1.12.0
Content schema write: v8
Readable Content schemas: v3, v4, v5, v6, v7, v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1, v2, v3, v4
```

v1.11.4 → v1.12.0:

- mandatory Game Content migration: none
- user.db migration: none
- Scanner display settings migration: none
- Scanner catalog schema migration: none

## 11. v1.12.0 검증

Exact product source:

```text
b2fcec460df256c581e87b53c6293dc4d2177b9c
```

최종 검증:

- 482 deterministic tests PASS
- Windows Release build PASS
- Windows x64 self-contained publish PASS
- actual published EXE Product UI / Map / Scanner smoke PASS
- Quest past-stage compatibility regression PASS
- search clear / diagnostic source-contract regression PASS
- graceful shutdown + portable root cleanliness PASS
- active-async Shutdown Race PASS
- package root/dependency/checksum audit PASS
- exact-main Documentation Consistency PASS
- exact-main artifact upload PASS
- Release workflow PASS
- `/releases/latest` = v1.12.0
- tag target = exact product source
- release target = exact product source
- public ZIP/checksum assets uploaded
- verified ZIP SHA-256 = public GitHub asset digest

### 11.1 PR transition note

초기 PR #237은 draft였다. exact feature-head workflow가 모두 성공한 뒤 Ready-for-review mutation을 시도했으나 연결된 GitHub GraphQL schema의 `Repository.fullDatabaseId` 오류로 실패했다. 제품 diff/head를 바꾸지 않고 #237을 닫고 동일 validated head `5216ab410c8a4384aee7d9f1a69fbd30302ad0a8`로 일반 PR #238을 생성해 main에 병합했다. workaround성 제품 코드는 추가하지 않았다.

## 12. 사용자 실사용 상태

v1.12.0 공개 바이너리를 사용한 실제 PC/Tarkov 최종 실사용 확인은 자동 release verification과 별개이며 현재 **PENDING**이다.

김태영 PC에서 생성되는 실제 diagnostic ZIP도 아직 분석 전이다. 해당 evidence를 받으면 PC 환경 문제인지 Scanner compatibility 문제인지 먼저 분리한다. 한 PC의 샘플만으로 Scanner global recognition threshold를 완화하지 않는다.

## 13. 다음 작업

현재 남은 릴리즈 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

새 사용자 요구사항, 실사용 회귀, Tarkov 데이터/동작 변화, 또는 김태영 PC diagnostic evidence가 들어오면 v1.12.0 stable에서 필요한 범위만 분석·수정한다.

이 문서 및 이후 documentation-only commit은 제품 릴리즈 source가 아니다. v1.12.0 historical identity는 `b2fcec460df256c581e87b53c6293dc4d2177b9c`에 고정한다.
