# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 프로젝트의 기준입니다.

기준일: **2026-08-31 KST**  
상태: **v1.11.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

v1.11.4 직전의 상세 상태 문서는 역사 보존을 위해 `docs/archive/STATE_v1.11.3.md`에 보관한다.

## 1. 제품과 운영 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정된 제품 요구사항 범위와 Scanner 기능은 완성 상태이며 기본 운영 모드는 유지보수다.

주요 제품 영역:

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / profile-aware pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- 사용자 동의형 Program Update
- Scanner + Mini Scanner
- Scanner Saved Case / Ground Truth / diagnostics / regression dataset
- Scanner 아이템 정보 DB
- Scanner Favorites / Recents

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 제품 사양 권위가 아니다. Map/MiniMap에 한해 검증된 pinned donor source를 제한적으로 compile-link하며 준현 헬퍼의 제품 요구사항과 product-owned bridge가 우선한다.

현재 진행 중 작업은 없다. `docs/ACTIVE_WORK.md`의 상태는 `NONE`이다.

## 2. 현재 public stable

```text
version: v1.11.4
exact product release source/tag target:
f9d3497004241ea80193e5a0d242e7219cf04f2a
PR: #236 — MERGED
superseded draft PR: #235 — CLOSED / NOT MERGED
final feature head: 84b56e81171543e289ed417d822c40c9d607d4d3
PR exact-head CI: 33345630940 — SUCCESS
PR exact-head Shutdown Race CI: 33345630896 — SUCCESS
PR exact-head Documentation Consistency: 33345630871 — SUCCESS
exact-main CI: 33345851673 — SUCCESS
exact-main Shutdown Race CI: 33345851704 — SUCCESS
exact-main Documentation Consistency: 33345851658 — SUCCESS
release workflow: 33346020525 — SUCCESS
release id: 379449740
published UTC: 2026-08-31T00:56:10Z
478 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 537252429
bytes: 80,564,330
SHA-256:
99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643

SHA256SUMS.txt
asset id: 537252430
bytes: 86
asset SHA-256:
6b81b3816b63b49999e225244214f3d2a3eeabc67fa88da2dd38542c0969f092
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9741999225
archive bytes: 241,626,166
archive SHA-256:
0af92581d315e2e69d7ff319f1c9968e52fa0093d8635db0eec894e954e2a450
```

GitHub `/releases/latest`, release `target_commitish`, `refs/tags/v1.11.4`, exact-main product source가 모두 `f9d3497004241ea80193e5a0d242e7219cf04f2a`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

Release workflow는 exact-main CI artifact를 다운로드한 뒤 `SHA256SUMS.txt`의 ZIP manifest hash와 실제 ZIP hash를 비교했다. 검증값은 `99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643`이며 공개 `Junhyun-Helper.zip`의 GitHub asset digest도 같은 값이다. 공개용 다른 제품 바이너리를 별도로 다시 빌드하지 않았다.

공식 공개 증거:

- `docs/RELEASE_1.11.4.md`
- `docs/.release-v1.11.4-status.json`
- `docs/RELEASE_NOTES_V1.11.4.md`

후속 documentation-only commit은 v1.11.4 제품 릴리즈 소스가 아니다. 공개 source/tag/assets는 위 exact source를 immutable historical identity로 사용한다.

## 3. v1.11.4 PATCH — MiniMap 최초 생성 지도 동기화

### 3.1 사용자 증상 / 위험 경로

Main Map에서 지도를 변경한 직후 MiniMap을 처음 열 때 첫 visible frame이 이전 지도 상태를 읽을 수 있는 timing race가 있었다. 이미 생성된 MiniMap의 재표시만 검증하면 fresh first-create 경로를 놓칠 수 있었다.

### 3.2 원인

Main Map selection handler가 product state synchronization을 `ContextIdle` queued callback에 의존할 수 있었다. 같은 input turn에 MiniMap constructor/registration이 먼저 진행되면 stale `MapTrackerService` state를 읽을 수 있었다.

### 3.3 현재 계약

- 실제 Main Map selection 변경 시 `SynchronizeCore()`를 동기적으로 먼저 실행한다.
- product tracker/registry가 현재 selected map을 먼저 보유한다.
- 기존 queued reconciliation도 유지해 같은 dispatcher cycle의 후속 donor 상태를 다시 정합화한다.
- 아직 한 번도 생성되지 않은 MiniMap first-create와 기존 window reuse 모두 현재 selected map을 첫 visible frame부터 사용한다.
- actual published EXE smoke가 `first-minimap-creation-boundary=ok`를 요구한다.

## 4. v1.11.4 PATCH — Extract / standard marker lifecycle

### 4.1 PMC / Scav / Transit extract

MiniMap extract 검증은 checkbox 값 또는 data object 존재만 확인하지 않는다.

- PMC / Scav / Transit product filter state를 donor visual state와 연결한다.
- Transit은 `ExtractFaction.Transit` / `ShowTransits` 계약을 사용한다.
- packaged extract data에서 실제 Transit이 존재하는 map을 runtime smoke가 선택한다.
- expected grouped Transit count와 실제 MiniMap rendered Transit marker count가 일치해야 한다.

### 4.2 standard marker empty-layer race

Donor marker refresh는 live standard marker layer를 먼저 clear한 뒤 비동기 loading/rebuild를 수행한다. 뒤따르는 refresh가 이전 async work를 cancel하면 marker DB에는 표시 대상 data가 있는데 live layer만 비어 있는 상태가 남을 수 있었다.

현재 복구 계약:

- 표시 대상 marker data가 이미 `MapMarkerDbService`에 로드돼 있어야 한다.
- standard marker layer만 일정 시간 비어 있는 상태여야 한다.
- another `QueueMarkerRefresh()`를 시작하지 않는다.
- loaded data에서 standard marker layer만 직접 재구성한다.
- 현재 floor / filter / MiniMap Marker Size presentation을 다시 적용한다.
- deliberate all-hidden state를 오류로 오인하지 않는다.
- bounded recovery만 허용하며 무한 retry를 만들지 않는다.

actual published EXE smoke는 `actual-transit-marker-render=ok`와 `standard-marker-direct-recovery=ok`를 요구한다.

## 5. v1.11.4 PATCH — Player Marker Size isolation

Player Marker Size를 바꾸는 동작은 MiniMap 전체 view refresh를 호출하지 않는다.

현재 계약:

- player marker `PlayerMarkerScale`만 변경한다.
- player marker setting persistence만 갱신한다.
- Name Size를 변경하지 않는다.
- MiniMap Marker Size를 변경하지 않는다.
- 일반 / Quest / Extract marker presentation을 재초기화하지 않는다.

actual published EXE smoke는 `player-marker-size-isolated=ok`를 요구한다.

## 6. v1.11.4 PATCH — Mini Scanner context menu

Mini Scanner 우클릭 `현재 결과 교정` context menu와 해당 modal correction path는 제거했다.

유지 계약:

- 좌클릭 drag 이동
- topmost
- recognition/result display
- 전역 `교정 데이터 추가` hotkey의 evidence-only Saved Case 저장
- Ground Truth 자동 생성/추측 금지

actual published EXE smoke는 `mini-scanner-context-menu=none`을 요구한다.

## 7. Scanner 유지 계약

### 7.1 정확도 정책

- false positive보다 miss를 선호한다.
- OCR/matcher/candidate/recovery acceptance threshold는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- price/needed/source/relationship metadata를 Item ID proof에 사용하지 않는다.
- scan-time 외부 network I/O를 recognition proof에 추가하지 않는다.

### 7.2 사용자 정보 표시

Scanner current needed:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

Scanner source:

```text
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner display settings schema는 v9이며 `ammo_pickup`은 정상 visibility/order field다. `플리마켓 최저가` compatibility data/model은 유지하지만 사용자 presentation에서는 숨긴다.

### 7.3 correction evidence

전역 `교정 데이터 추가` hotkey는 capture/save 전용이다.

- latest evidence 없음 → Case 생성 없음
- evidence 있음 → distinct Saved Case 저장
- `GroundTruthItemName = null`
- `UserConfirmed = false`
- Mini Scanner `저장 완료` transient feedback
- Saved Cases/review window 자동 open 금지
- Main Window/Scanner focus 강제 이동 금지
- duplicate explicit save 허용
- Ground Truth 자동 생성/추측 금지

v1.11.3에서 추가된 correction semantic carry는 동일 non-empty title signature, 동일 capture mode, 3초 이내에서 correction snapshot에만 적용된다. retained semantics는 live recognition decision에 사용하지 않는다.

### 7.4 안전 경계

Scanner는 external screen pixels + OCR만 사용한다.

사용하지 않음:

- game process memory read
- code/DLL injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

## 8. Map / MiniMap 유지 계약

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

### 8.1 player position / heading

player 위치는 map별 `playerMarkerTransform` affine transform을 사용한다. heading도 동일 affine transform의 translation을 제외한 선형부에 투영해 Main Map과 MiniMap이 같은 map coordinate system을 사용한다.

- Factory/Labs 이름별 임시 angle exception 대신 일반 affine heading projection 사용
- Reserve/Labyrinth 등 회전 transform 지원
- degenerate/non-finite transform은 normalized input heading으로 fail-safe

### 8.2 MiniMap map state ownership

- Main Map selection이 product tracker/registry의 desired map state를 소유한다.
- MiniMap window가 없어도 최신 desired map key를 보존한다.
- 새 window Register 시 최신 selection을 replay한다.
- Unregister는 desired map selection을 지우지 않는다.
- first-create synchronization은 selection input turn에서 synchronous state update를 보장한다.

### 8.3 marker presentation

- donor Extract controls가 product settings bridge보다 늦게 만들어질 수 있으므로 bounded retry/idempotent reparent를 사용한다.
- marker/name presentation은 donor visual rebuild 이후 product presentation을 다시 적용한다.
- standard marker direct recovery는 loaded data + unexpected empty layer 조건에서만 수행한다.
- Player Marker Size는 player marker scale에만 격리한다.

## 9. Hideout FIR / Needed Items / Ammo 유지 계약

### 9.1 Hideout FIR

Tarkov source `attributes.foundInRaid` 의미를 canonical Hideout requirement에 보존한다.

- FIR requirement에는 non-FIR inventory가 충당되지 않는다.
- 동일 item의 불필요한 non-FIR copy는 다른 requirement가 없으면 cleanup 후보가 될 수 있다.
- source semantics를 UI 추정으로 덮어쓰지 않는다.

### 9.2 Ammo pickup

Ammo pickup은 동일 caliber penetration과 현재 profile의 직접 구매 가능 상태를 기준으로 한다.

현재 직접 구매 가능으로 인정하지 않는 것:

- flea availability만 존재
- barter
- craft
- higher trader LL
- proof 없는 quest unlock

Ammo Pack은 authoritative `containsItems` 관계를 우선한다.

## 10. Game Content / Program Update 유지 계약

### 10.1 Game Content update

- candidate download/build
- schema/completeness/integrity validation
- validated active 승격
- Last Known Good 보존
- 검증 실패 시 기존 정상 데이터 유지
- external live-data semantics 변화가 있는 작업에서만 필요한 live-data 검증 수행

### 10.2 Program Update / Release

- GitHub public stable release를 확인한다.
- 사용자 동의 없이 자동 교체하지 않는다.
- stable ZIP + checksum contract를 사용한다.
- release workflow는 exact-main CI artifact를 사용한다.
- published stable release는 immutable하게 취급한다.
- documentation-only main commit이 같은 assembly version의 다른 bytes를 만들 수 있어도 이미 공개된 asset을 교체하지 않는다.

## 11. Schema / compatibility

```text
Desktop version: 1.11.4
Content schema write: v8
Readable Content schemas: v3, v4, v5, v6, v7, v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1, v2, v3, v4
```

v1.11.3 → v1.11.4:

- mandatory Game Content migration: none
- user.db migration: none
- Scanner display settings migration: none

## 12. v1.11.4 검증

Exact product source:

```text
f9d3497004241ea80193e5a0d242e7219cf04f2a
```

최종 검증:

- 478 deterministic tests PASS
- Windows Release build PASS
- Windows x64 self-contained publish PASS
- actual published EXE startup PASS
- rendered Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke PASS
- fresh MiniMap first-create selection sync PASS
- actual Transit marker rendering PASS
- standard marker direct recovery PASS
- Player Marker Size isolation PASS
- Mini Scanner context-menu absence PASS
- graceful shutdown + portable root cleanliness PASS
- active-async Shutdown Race PASS
- package root/dependency/checksum audit PASS
- exact-main Documentation Consistency PASS
- exact-main artifact upload PASS
- Release workflow PASS
- `/releases/latest` = v1.11.4
- tag target = exact product source
- release target = exact product source
- public ZIP/checksum assets uploaded
- verified ZIP SHA-256 = public GitHub asset digest

### 12.1 release 과정에서 발견·수정한 검증 이슈

1. release notes 첫 제목이 FIRST_RUN canonical version heading과 다르게 `Release Notes` suffix를 포함해 release identity consistency test 1개가 실패했다. 제목 형식을 맞춘 뒤 final 478/478 gate를 통과했다.
2. Draft PR #235의 Ready-for-review GraphQL mutation은 GitHub connector schema의 `Repository.fullDatabaseId` 오류로 실패했다. 제품 diff/head를 바꾸지 않고 #235를 닫고 동일 validated head의 일반 Ready PR #236을 생성해 전체 CI를 다시 통과시킨 뒤 병합했다.

두 항목 모두 최종 제품 runtime 결함이 아니며 공개 product source에 workaround성 제품 코드가 들어가지 않았다.

## 13. 사용자 실사용 상태

v1.11.3에서 보고된 MiniMap lifecycle/marker presentation과 Mini Scanner 우클릭 증상은 v1.11.4 수정의 실제 사용자 evidence로 사용했다.

v1.11.4 공개 바이너리를 사용한 사용자의 실제 PC/Tarkov 최종 실사용 확인은 자동화 release verification과 별개이며 현재 **PENDING**이다.

## 14. 다음 작업

현재 남은 릴리즈 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

새 사용자 요구사항, 실사용 회귀, Tarkov 데이터/동작 변화가 확인되면 v1.11.4 stable에서 필요한 범위만 분석·수정한다. 근거 없는 threshold 완화, 추측성 최적화, 기능 변경 또는 대규모 리팩터링을 시작하지 않는다.

이 문서 및 이후 documentation-only commit은 제품 릴리즈 source가 아니다. v1.11.4 historical identity는 `f9d3497004241ea80193e5a0d242e7219cf04f2a`에 고정한다.
