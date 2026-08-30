# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 프로젝트의 기준입니다.

기준일: **2026-08-30 KST**  
상태: **v1.11.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

v1.11.2 직전의 상세 상태 문서는 역사 보존을 위해 `docs/archive/STATE_v1.11.1.md`에 그대로 보관한다.

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
version: v1.11.2
exact product release source/tag target:
5822757f6490ec82aab33793752e48de14490628
PR: #232 — MERGED
superseded draft PR: #231 — CLOSED / NOT MERGED
PR exact-head CI: 33307979144 — SUCCESS
exact-main CI: 33308162829 — SUCCESS
exact-main Shutdown Race CI: 33308162797 — SUCCESS
exact-main Documentation Consistency: 33308162850 — SUCCESS
release workflow: 33308291656 — SUCCESS
release id: 379257951
published UTC: 2026-08-30T11:11:52Z
470 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 536514791
bytes: 80,554,866
SHA-256:
d013ac2d423d2a83c49e1e6483dcad038a3792a5b865c1400085fd56e25592a9

SHA256SUMS.txt
asset id: 536514792
bytes: 86
asset SHA-256:
4860aceab06843707951dcd50951a62843d40ef7a2ea2a9d8efa7972847aa657
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9731167378
archive bytes: 241,597,223
archive SHA-256:
5eef3f620d46f3ac3c7990ec18fdcf46877741fc2c1647a856b3accb2fa26c8b
```

GitHub `/releases/latest`, release `target_commitish`, `refs/tags/v1.11.2`, exact-main product source가 모두 `5822757f6490ec82aab33793752e48de14490628`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

Release workflow는 exact-main CI artifact를 다운로드해 EXE ProductVersion, `FIRST_RUN_KO.txt`, ZIP checksum manifest를 검증한 뒤 stable release를 공개한다. v1.11.2는 이 경로를 성공적으로 통과했다.

공식 공개 증거:

- `docs/RELEASE_1.11.2.md`
- `docs/.release-v1.11.2-status.json`
- `docs/RELEASE_NOTES_V1.11.2.md`

후속 documentation-only commit은 v1.11.2 제품 릴리즈 소스가 아니다. 공개 source/tag/assets는 위 exact source를 immutable historical identity로 사용한다.

## 3. v1.11.2 PATCH — Scanner 교정 hotkey

### 3.1 사용자 증상

레이드 중 `교정 데이터 추가` 전역 단축키를 누르면 저장 성공 후 교정 데이터/Saved Cases 창이 열리고 UI focus가 이동했다. 사용자는 레이드 중에는 저장만 하고 검토는 나중에 직접 하기를 원한다.

### 3.2 원인

`ScannerCoordinator.CorrectionCapture.cs`의 성공 경로가 다음 두 책임을 섞고 있었다.

1. 최신 Scanner evidence를 durable Saved Case로 저장
2. 저장 직후 review window를 `ShowDialog()`로 열고 Scanner/MainWindow를 foreground로 가져옴

두 번째 동작은 explicit review UI에는 적합하지만 global raid-time capture hotkey 계약에는 부적합했다.

### 3.3 현재 계약

전역 hotkey는 capture/save 전용이다.

1. 최신 exact Scanner evidence snapshot 확인
2. evidence 없음 → `저장할 스캔 결과가 없습니다.` / Case 생성 없음
3. evidence 있음 → explicit save용 distinct Case ID 생성
4. Saved Case 저장
5. `GroundTruthItemName = null`
6. `UserConfirmed = false`
7. 성공 status 게시
8. Mini Scanner에 `저장 완료` transient feedback 표시
9. review window 자동 open 금지
10. Main Window/Scanner focus 강제 이동 금지

동일 latest evidence를 사용자가 반복해서 명시적으로 저장하는 것은 허용한다. hotkey는 Ground Truth를 추측·생성·자동 확정하지 않는다.

교정 데이터 검토는 Scanner UI에서 사용자가 명시적으로 여는 별도 동작으로 남는다.

## 4. v1.11.2 PATCH — Items / Hideout 검색 clear

### 4.1 사용자 증상

Quest/Ammo/Scanner 검색창은 검색어가 없을 때 clear glyph가 보이지 않고 입력 중일 때만 inline `×`가 표시된다. 반면 v1.11.1의 Items/Hideout는 별도 표준 Button 형태의 `×`가 처음부터 보였고 시각적으로 일관되지 않았다.

### 4.2 원인

제품에는 이미 `ProductSearchClearButtonBehavior`가 존재해 Quest/Hideout/Items의 `SearchBox`에 conditional inline clear behavior를 제공한다.

v1.11.1에서 Items/Hideout에 별도 `SearchClearButtonInstaller`와 page partial을 추가하면서 같은 목적의 두 구현이 중복되었다. 새 installer가 always-visible 별도 Button을 만들면서 사용자 보고 회귀가 발생했다.

### 4.3 현재 계약

canonical 구현은 `ProductSearchClearButtonBehavior` 하나다.

- query empty → clear glyph `Collapsed`
- query non-empty → 오른쪽 inline `×` `Visible`
- clear click → 기존 TextBox `Clear()`
- 기존 `TextChanged` 검색/필터 경로 그대로 사용
- clear 뒤 TextBox focus 복구
- duplicate clear control 금지
- Quest/Items/Hideout 동일 behavior 공유

v1.11.1에서 추가된 duplicate installer/partial은 제거됐다.

### 4.4 runtime smoke에서 발견한 검증 결함

초기 v1.11.2 published EXE smoke는 Scanner 탭에서 실행되면서 아직 `Collapsed` 상태인 Items/Hideout page가 정상 `Loaded` attachment를 거치지 않았는데도 clear button이 이미 존재해야 한다고 가정했다. `Items search clear button was not rendered` 실패는 제품 결함이 아니라 smoke lifecycle 가정 문제였다.

최종 smoke는 canonical behavior를 해당 SearchBox에 직접 attach한 뒤 다음 실제 상태 전이를 검증한다.

- empty → glyph hidden
- typed → glyph visible
- clear → query empty + glyph hidden
- single inline clear control 유지

## 5. v1.11.2 PATCH — Map / MiniMap player heading

### 5.1 사용자 증상

Factory에서 screenshot으로 위치를 읽은 player marker가 실제 바라보는 방향보다 약 90° 반시계 방향으로 틀어진 것으로 보였다. 위치와 방향이 정확한지 Factory를 포함한 전체 맵 audit가 필요했다.

### 5.2 기존 위치 경로

screenshot 기반 player 위치 경로:

```text
ScreenshotCoordinateParser
→ MapTrackerService
→ MapCoordinateTransformer.TryTransformPlayerPosition
→ map-specific playerMarkerTransform
→ Main Map / MiniMap marker
```

각 map config의 `playerMarkerTransform`은 affine transform `[a,b,c,d,tx,ty]`이며 player 위치에는 이미 적용되고 있었다. 이 위치 placement 계약은 유지한다.

### 5.3 기존 heading 오류

screenshot quaternion에서 얻은 raw yaw는 `ScreenPosition.Angle`에 전달됐다.

- Main Map: Factory `+90°`, Labs `-90°`만 이름 기반으로 별도 보정
- MiniMap: raw yaw를 그대로 사용
- Reserve / Labyrinth처럼 affine transform 자체에 회전 성분이 있는 맵은 일반적으로 처리되지 않음

따라서 위치와 방향이 서로 다른 map coordinate system을 사용할 수 있었다. Factory MiniMap에서 약 90° 오차가 발생할 수 있는 실제 코드 경로가 확인됐다.

### 5.4 현재 heading projection

`JunhyunHelper.Core.Maps.PlayerHeadingProjection`이 player 위치와 동일한 affine orientation을 heading vector에도 적용한다.

입력은 WPF 기준 screen heading이다.

```text
0° = up
positive = clockwise
```

baseline screen vector:

```text
screenX = sin(angle)
screenY = -cos(angle)
```

screenshot yaw baseline orientation을 world vector로 되돌린다.

```text
worldX = -screenX
worldZ = screenY
```

position affine transform의 translation을 제외한 선형부를 heading vector에 적용한다.

```text
projectedX = a * worldX + b * worldZ
projectedY = c * worldX + d * worldZ
```

최종 WPF angle:

```text
atan2(projectedX, -projectedY)
→ normalize 0..360
```

non-finite transform 또는 degenerate vector는 normalized input heading으로 fail-safe한다.

### 5.5 적용 범위

- Factory의 기존 +90° 의미를 일반식으로 재현
- Labs의 기존 -90° 의미를 일반식으로 재현
- Reserve / Labyrinth 등 회전된 affine transform 처리
- 현재 map config의 모든 `playerMarkerTransform`에 동일 규칙 적용
- Main Map과 MiniMap 모두 donor render 이후 같은 projected heading을 최종 적용
- 새로운 map-name-specific angle exception을 늘리지 않음

위치 좌표는 기존 transform을 그대로 사용하며 heading만 동일 좌표계에 맞춘다.

## 6. Map / MiniMap 유지 계약

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

### 6.1 MiniMap first activation map replay

- MiniMap window가 없어도 product registry가 최신 desired map key를 보존한다.
- 새 window Register 시 최신 selection을 replay한다.
- Unregister는 desired map selection을 지우지 않는다.
- main product가 selection persistence ownership을 유지한다.

### 6.2 Extract checkbox late-load

- donor Extract controls가 product settings bridge보다 늦게 만들어질 수 있다.
- bounded retry와 idempotent reparenting을 사용한다.
- MiniMap extract projection이 비면 현재 presentation을 다시 동기화한다.

### 6.3 marker/name presentation repair

Player Marker Size 등 donor visual rebuild 뒤 Junhyun presentation을 다시 적용한다.

- marker scale
- name scale
- name visibility
- hidden categories
- additional marker scale
- edge-label presentation

### 6.4 marker empty-layer recovery

확인된 donor refresh cancellation race에 대해서만 bounded one-shot recovery를 사용한다.

- same map/floor 직전 stable marker history 필요
- 연속 empty observation 필요
- deliberate all-hidden 상태는 복구하지 않음
- 무한 retry 금지

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

### 7.3 안전 경계

Scanner는 external screen pixels + OCR만 사용한다.

사용하지 않음:

- game process memory read
- code/DLL injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

이 경계는 제품 요구사항이자 유지보수 계약이다.

## 8. Hideout FIR / Needed Items / cleanup

Hideout item requirement의 Tarkov source `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.

- FIR requirement에는 non-FIR inventory가 충당되지 않음
- 동일 item의 불필요한 non-FIR copy는 다른 requirement가 없으면 cleanup 후보 가능
- Quest FIR / Hideout FIR을 임의 하드코딩으로 분리하지 않음
- Game Content 변경 시 Needed Items / cleanup derived state를 현재 canonical requirement 기준으로 재계산

## 9. Ammo pickup evaluator

same-caliber penetration ranking과 현재 profile의 직접 구매 가능 상태를 사용한다.

직접 구매 가능으로 인정:

- Trader LL requirement <= 현재 해당 Trader LL
- 현금 direct purchase
- quest unlock 필요 시 해당 quest 완료가 현재 profile에서 증명됨

현재 직접 구매 가능으로 인정하지 않음:

- barter
- craft
- flea market
- higher LL
- 완료 여부를 증명할 수 없는 quest unlock

ranking 계약:

- direct-buy penetration band 내부 unavailable 중간 탄은 pickup 우선 대상에서 제외
- band 밖의 높은 penetration 탄은 pickup 대상
- 동일 penetration tie deterministic
- 해당 caliber에 direct-buy 탄이 없으면 unavailable ammo는 모두 pickup 후보

고정 예제:

```text
rank 1 2 3 4 5 / buyable 2,4
=> pickup 1 / not 3 / pickup 5

rank 1 2 3 4 5 6 7 / buyable 3,5,6
=> pickup 1,2 / not 4 / pickup 7
```

## 10. Ammo Pack → canonical ammo

Scanner가 Ammo Pack을 인식하면 pack 자체가 아니라 contained canonical ammo를 기준으로 pickup 판단한다.

Resolution priority:

1. authoritative `containsItems`
2. authoritative relation이 빈 경우에만 narrow name fallback
3. non-empty authoritative relation이 mixed/ambiguous하면 heuristic으로 덮어쓰지 않고 fail-closed

## 11. Game Content / Program Update

Game Content update는 다음 안전 계약을 유지한다.

- remote/source read
- candidate canonicalization
- deterministic validation
- completeness/integrity gate
- valid candidate만 active로 승격
- 기존 정상 active/LKG 보존
- fatal validation failure 시 fail-closed

외부 Tarkov 데이터의 의미가 바뀌는 작업은 importer → canonical model → derived workspace → UI/Scanner downstream까지 추적한다.

Program Update는 GitHub latest stable을 확인하고 사용자 동의 후 verified stable ZIP을 사용한다. 릴리즈 package checksum 계약을 유지한다.

## 12. Schema / compatibility

```text
Desktop target version: 1.11.2
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

Migration:

```text
v1.11.1 → v1.11.2 mandatory Game Content migration: none
v1.11.1 → v1.11.2 user.db migration: none
v1.11.1 → v1.11.2 Scanner display settings migration: none
```

## 13. Architecture / ownership

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

- Core: canonical domain, deterministic calculations/policies
- Application: user use cases, workspaces, authoritative mutation orchestration
- Infrastructure: source parsing, persistence, content/update I/O
- Desktop: WPF UI, Scanner capture/OCR/runtime/diagnostics, Map product bridge
- donor Map/MiniMap: limited compile-link exception; donor updater/content ownership은 사용하지 않음

## 14. v1.11.2 검증

### 14.1 PR exact-head

Ready PR #232 exact head:

```text
db4da8307de25fa8e4a6e60c043d2239eb6184fc
CI: 33307979144 — SUCCESS
Shutdown Race CI: 33307979132 — SUCCESS
Documentation Consistency: 33307979269 — SUCCESS
```

제품 diff가 동일한 이전 draft #231은 connector의 ready-for-review GraphQL schema 호환 오류 때문에 `CLOSED / NOT MERGED` 처리했다. 제품 branch와 구현을 바꾸지 않고 ready PR #232로 승계했다.

### 14.2 exact-main product source

```text
source: 5822757f6490ec82aab33793752e48de14490628
CI: 33308162829 — SUCCESS
Shutdown Race CI: 33308162797 — SUCCESS
Documentation Consistency: 33308162850 — SUCCESS
Release workflow: 33308291656 — SUCCESS
```

검증 범위:

- 470 deterministic tests
- Windows Release build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- canonical Items/Hideout conditional inline clear runtime behavior
- Factory/Labs/Reserve/Labyrinth heading known-orientation regression
- 현재 전체 player transform deterministic heading validation
- Main Map/MiniMap projected-heading runtime bridge contract
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main artifact upload
- public tag/release/assets/latest-stable readback

Exact-main artifact `9731167378`을 Release workflow가 사용해 v1.11.2를 공개했다.

### 14.3 검증 중 발견해 수정한 gate 문제

두 가지 검증 자체의 문제도 제품 공개 전에 잡았다.

1. v1.11.2 version bump 뒤 `docs/RELEASE_NOTES_V1.11.2.md`가 아직 없어 ReleaseIdentityTests 1건이 실패했다. 제품 코드와 build는 정상이었고 release notes를 추가한 뒤 470/470 PASS로 복구했다.
2. 초기 published EXE search smoke가 아직 Loaded되지 않은 Collapsed Items/Hideout page에 clear control이 이미 attach되어 있어야 한다고 잘못 가정했다. 제품 canonical behavior가 아니라 smoke lifecycle 가정을 수정했다.

## 15. 공개 릴리즈 검증

Public release:

```text
v1.11.2
release id: 379257951
published: 2026-08-30T11:11:52Z
draft: false
prerelease: false
latest stable: true
target: 5822757f6490ec82aab33793752e48de14490628
```

`refs/tags/v1.11.2`은 lightweight commit ref이며 정확히 product release source를 가리킨다.

Public ZIP과 checksum asset은 release workflow가 exact-main artifact에서 검증한 결과다. GitHub public asset digest와 release metadata를 readback했다.

## 16. 유지보수 원칙

- 사용자 보고 실사용 증상을 높은 우선순위의 회귀 증거로 취급한다.
- 현재 코드가 존재한다는 이유만으로 올바른 설계라고 가정하지 않는다.
- 공식 제품 요구사항과 현재 구현이 충돌하면 원인을 분석해 명시적으로 바로잡는다.
- 실제 결함 증거 없이 대규모 리팩터링하지 않는다.
- user-visible WPF lifecycle 변경은 source assertion만으로 완료 선언하지 않는다.
- 외부 Tarkov 데이터 의미 변경은 importer/canonical/derived state까지 추적한다.
- release는 PR exact-head 검증 → main merge → exact-main CI/published EXE → verified artifact → Release workflow → public readback 순으로 관리한다.
- 공개 릴리즈 후 documentation-only commit은 historical product source를 덮어쓰지 않는다.

## 17. 현재 남은 작업

제품 개발 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.11.2 실사용 검증은 자동화 검증과 별개이며 현재 **PENDING**이다. 실사용에서 새 회귀가 확인되면 해당 증상을 우선 증거로 삼아 현재 stable에서 최소 범위로 수정한다.
