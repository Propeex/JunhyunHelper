# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 프로젝트의 기준입니다.

기준일: **2026-08-31 KST**  
상태: **v1.11.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

v1.11.3 직전의 상세 상태 문서는 역사 보존을 위해 `docs/archive/STATE_v1.11.2.md`에 보관한다.

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
version: v1.11.3
exact product release source/tag target:
043abad38f4c3ebc9101463a162614ef67df7536
PR: #234 — MERGED
superseded draft PR: #233 — CLOSED / NOT MERGED
PR exact-head CI: 33319386444 — SUCCESS
PR exact-head Shutdown Race CI: 33319386465 — SUCCESS
PR exact-head Documentation Consistency: 33319386455 — SUCCESS
exact-main CI: 33319592093 — SUCCESS
exact-main Shutdown Race CI: 33319592115 — SUCCESS
exact-main Documentation Consistency: 33319592111 — SUCCESS
release workflow: 33319769016 — SUCCESS
release id: 379321405
published UTC: 2026-08-30T15:29:47Z
474 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 536758239
bytes: 80,558,970
SHA-256:
e43892ecafc9920a7e3b7295f94b8a5324865977028b3573437d8ff7de4f327e

SHA256SUMS.txt
asset id: 536758240
bytes: 86
asset SHA-256:
5b3cc0468ad6a11076b547883fbd16d1276c74bc51779251c0c3421a070d63c3
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9734538554
archive bytes: 241,607,396
archive SHA-256:
cf10ab86f31c44dff00414b9f4e47ff9bf5a64df18210084bd2b41c42e3ac2a7
```

GitHub `/releases/latest`, release `target_commitish`, `refs/tags/v1.11.3`, exact-main product source가 모두 `043abad38f4c3ebc9101463a162614ef67df7536`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

Release workflow는 exact-main CI artifact를 다운로드해 verified main commit과 release identity를 확인한 뒤 stable release를 공개했다. 별도의 다른 제품 바이너리를 다시 빌드하지 않는다.

공식 공개 증거:

- `docs/RELEASE_1.11.3.md`
- `docs/.release-v1.11.3-status.json`
- `docs/RELEASE_NOTES_V1.11.3.md`

후속 documentation-only commit은 v1.11.3 제품 릴리즈 소스가 아니다. 공개 source/tag/assets는 위 exact source를 immutable historical identity로 사용한다.

## 3. v1.11.3 PATCH — Items / Hideout 검색 clear lifecycle

### 3.1 사용자 증상

v1.11.2 실사용 화면에서 Items와 Hideout 검색창에 텍스트가 입력돼도 Quest 등 다른 검색창에서 보이는 conditional inline `×`가 표시되지 않았다.

### 3.2 원인

공유 구현 `ProductSearchClearButtonBehavior` 자체는 제품 요구사항과 일치했다. 문제는 Items/Hideout가 실제 visible page lifecycle에서 behavior attach를 안정적으로 보장하지 못한 데 있었다.

또한 v1.11.2 published smoke는 실제 page lifecycle이 clear UI를 만들었는지 확인하지 않고 smoke 코드가 직접 `ProductSearchClearButtonBehavior.Attach(searchBox)`를 호출한 뒤 결과를 검사했다. 즉 smoke가 검증 대상 UI를 스스로 만들어 실제 회귀를 숨길 수 있었다.

### 3.3 현재 계약

- Items/Hideout real page lifecycle의 Loaded + template boundary에서 canonical behavior를 attach한다.
- query empty → inline clear glyph `Collapsed`
- query non-empty → inline clear glyph `Visible`
- clear click → 기존 TextBox `Clear()` 및 기존 `TextChanged` 검색/필터 경로 사용
- clear 뒤 TextBox keyboard focus 복구
- duplicate clear control 금지
- Quest/Items/Hideout 동일 product-owned behavior 공유
- published smoke는 behavior를 직접 설치하지 않고 실제 lifecycle 결과만 검증

## 4. v1.11.3 PATCH — Map 지도 마커 패널

### 4.1 사용자 증상

정상적인 큰 창에서도 지도 마커 패널의 체크박스 목록 하단 탈출구 영역이 잘렸다. 창 높이를 줄이면 scrollbar가 나타나 하단 일부가 보이는 역설적인 상태가 관찰됐다.

### 4.2 원인

v1.11.2 body layout은 expanded panel의 높이를 그 시점의 `MapMarkersContent.DesiredSize`에 맞추는 content-sized popup 방식이었다. donor content tree의 탈출구 행 생성/reparent가 완전히 정착하기 전에 작은 DesiredSize가 측정되면 tall window에서도 panel height가 짧게 고정될 수 있었다.

기존 smoke는 이미 선택된 panel 내부를 viewport가 채우는지만 검사했기 때문에 panel 자체가 잘못 짧은 상태를 놓쳤다.

### 4.3 현재 계약

- expanded marker panel은 content-sized popup이 아니라 available-height viewport다.
- `maximumPanelHeight = max(120, mapHeight - 16)`을 사용한다.
- expanded 상태에서는 panel이 available map height를 사용한다.
- inner checkbox viewport가 header/chrome을 제외한 panel body를 채운다.
- vertical scrolling은 `ScrollBarVisibility.Auto`이며 실제 rendered overflow에서만 scrollbar가 나타난다.
- collapsed 상태에서는 explicit height/max-height를 해제한다.
- actual published EXE smoke가 panel height, body fill, `ScrollableHeight`, `ComputedVerticalScrollBarVisibility` 일관성을 확인한다.

## 5. v1.11.3 PATCH — Scanner 교정 이미지 zoom

### 5.1 제품 요구사항

Saved Case/교정 화면의 screenshot 또는 ROI image를 마우스 휠로 확대/축소해 작은 텍스트/경계를 확인할 수 있어야 한다. zoom 때문에 Ground Truth rectangle이나 직접 지정 좌표의 의미가 변하면 안 된다.

### 5.2 구현 계약

- source image/canvas는 항상 원본 pixel width/height를 유지한다.
- display scale만 `LayoutTransform`으로 변경한다.
- fit multiplier 1.0을 최소로 하고 최대 8.0까지 zoom한다.
- wheel step은 1.15×다.
- 확대 시 `ScrollViewer`를 통해 pan/scroll할 수 있다.
- pointer 위치 기준 anchor 비율을 보존해 확대 시 관심 지점이 불필요하게 튀지 않게 한다.
- Ground Truth/manual selection 좌표는 source pixel coordinate로 저장한다.

### 5.3 runtime smoke가 발견한 추가 결함

첫 published EXE zoom smoke에서 확대 전 fit scale은 약 `0.573`, 한 번 확대 후 다시 축소한 scale은 약 `0.596`으로 돌아왔다.

원인은 `ViewportWidth/ViewportHeight`가 Auto scrollbar 출현/소멸에 따라 달라져 nominal fit scale이 이전 zoom state에 의존한 것이었다.

현재 fit scale은 `ImageScrollViewer.ActualWidth/ActualHeight`에서 padding/border를 제외한 stable arranged control bounds를 기준으로 계산한다. 창 resize는 SizeChanged를 통해 다시 계산하지만 zoom 중 scrollbar 상태는 fit 기준을 바꾸지 않는다.

actual published EXE smoke는 다음을 검증한다.

- wheel zoom-in 실제 scale 증가
- 같은 양의 zoom-in/out 후 fit scale 복귀
- source-pixel coordinate canvas 유지

## 6. Scanner calibration/diagnostics evidence 분석

사용자가 제공한 private diagnostics/calibration bundle에는 reviewed Ground Truth correction 5건이 있었다. 원본 screenshots/bundle은 public repository에 커밋하지 않는다.

저장된 5건의 pipeline stage는 모두 `NOT_RUN`이었지만 bundled runtime log를 대조하면 적어도 마지막 두 case는 저장 직전 실제 OCR/matcher가 실행됐다.

확인된 대표 evidence:

- `Corrugated hose 주름진 호스`
  - title ROI까지 도달
  - WinRT OCR 선두 Latin glyph 일부가 Han/CJK glyph로 오인됨
  - 보수적 invalid-character gate에서 `OCR_INVALID_CHARACTERS` reject
- `7.62x25mm TT P gl ammo pack (25 pcs)`
  - nearest official candidate가 Ground Truth와 동일
  - confidence 약 0.846, margin 약 0.038
  - current threshold 아래라 `LOW_CONFIDENCE` fail-closed

이 evidence는 false-positive 우선 안전 계약을 깨고 threshold를 완화할 근거로 사용하지 않는다.

### 6.1 확인된 timing defect

`ScannerRecognitionDebugStore`가 단일 latest frame만 유지했다. 분석 완료 frame 뒤에 빠른 새 capture가 발생하면 screenshot/geometry만 있는 `NOT_RUN` frame이 latest를 덮어쓸 수 있었다. 사용자가 그 뒤 correction hotkey를 누르면 실제로 존재했던 OCR/matcher semantics가 저장 case에서 사라졌다.

### 6.2 현재 correction semantic carry 계약

correction snapshot에서만 다음 조건을 모두 만족할 때 직전 analyzed frame의 semantic evidence를 carry한다.

1. current title signature가 non-empty
2. analyzed title signature와 ordinal exact match
3. capture mode가 동일
4. analyzed frame age가 0~3초

carry 대상은 recognition reason/OCR/matcher evidence이며 screenshot과 geometry는 항상 current exact frame을 유지한다.

이 retained semantics는 live recognition decision이나 candidate acceptance에 사용하지 않는다. threshold/character policy는 변경하지 않는다.

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

### 7.3 correction hotkey

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

이 경계는 제품 요구사항이자 유지보수 계약이다.

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

### 8.2 MiniMap first activation map replay

- MiniMap window가 없어도 product registry가 최신 desired map key를 보존한다.
- 새 window Register 시 최신 selection을 replay한다.
- Unregister는 desired map selection을 지우지 않는다.
- main product가 selection persistence ownership을 유지한다.

### 8.3 extract / marker lifecycle

- donor Extract controls가 product settings bridge보다 늦게 만들어질 수 있으므로 bounded retry/idempotent reparent를 사용한다.
- marker/name presentation은 donor visual rebuild 이후 product presentation을 다시 적용한다.
- marker empty-layer recovery는 확인된 donor refresh cancellation race에만 bounded one-shot으로 적용한다.
- deliberate all-hidden 상태나 무한 retry는 허용하지 않는다.

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

### 10.2 Program Update

- GitHub public stable release를 확인한다.
- 사용자 동의 없이 자동 교체하지 않는다.
- stable ZIP + checksum contract를 사용한다.
- release workflow는 exact-main CI artifact를 사용한다.

## 11. Schema / compatibility

```text
Desktop version: 1.11.3
Content schema write: v8
Readable Content schemas: v3, v4, v5, v6, v7, v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1, v2, v3, v4
```

v1.11.2 → v1.11.3:

- mandatory Game Content migration: none
- user.db migration: none
- Scanner display settings migration: none

## 12. v1.11.3 검증

Exact product source:

```text
043abad38f4c3ebc9101463a162614ef67df7536
```

최종 검증:

- 474 deterministic tests PASS
- Windows Release build PASS
- Windows x64 self-contained publish PASS
- ProductVersion `1.11.3+043abad...` 확인
- actual published EXE startup PASS
- rendered Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke PASS
- Items/Hideout real lifecycle search clear PASS
- Map marker available-height/body fill/real overflow scrollbar PASS
- Scanner correction zoom/stable fit/source-pixel coordinates PASS
- graceful shutdown + portable root cleanliness PASS
- active-async Shutdown Race PASS
- package root/dependency/checksum audit PASS
- exact-main Documentation Consistency PASS
- exact-main artifact upload PASS
- Release workflow PASS
- `/releases/latest` = v1.11.3
- tag target = exact product source
- release target = exact product source
- public ZIP/checksum assets uploaded and digest readback 완료

### 12.1 release 과정에서 발견·수정한 검증 이슈

1. 오래된 v1.8.3 source-string contract가 삭제된 내부 변수명 `requestedPanelHeight`를 강제해 최신 available-height 구현을 잘못 실패시켰다. 현재 제품 계약을 검사하도록 갱신했다.
2. correction zoom 최초 published smoke가 scrollbar-dependent fit scale 불안정을 실제로 검출했다. 구현을 수정한 뒤 final smoke에서 통과했다.
3. Draft PR #233의 Ready-for-review GraphQL mutation은 GitHub connector schema의 `Repository.fullDatabaseId` 오류로 실패했다. 제품 diff/head를 변경하지 않고 #233을 닫고 동일 head의 일반 Ready PR #234를 열어 전체 CI를 다시 통과시킨 뒤 병합했다.

이 세 항목은 최종 공개 제품에서 모두 해결되었거나 제품 외 GitHub tooling 이슈로 격리됐다.

## 13. 사용자 실사용 상태

v1.11.2에서 보고된 검색 clear/Map marker 증상과 Scanner diagnostics는 v1.11.3 수정의 실제 사용자 evidence로 사용했다.

v1.11.3 공개 바이너리를 사용한 사용자의 실제 PC/Tarkov 최종 실사용 확인은 자동화 release verification과 별개이며 현재 **PENDING**이다.

## 14. 다음 작업

현재 남은 릴리즈 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

새 사용자 요구사항, 실사용 회귀, Tarkov 데이터/동작 변화가 확인되면 v1.11.3 stable에서 필요한 범위만 분석·수정한다. 근거 없는 threshold 완화, 추측성 최적화, 기능 변경 또는 대규모 리팩터링을 시작하지 않는다.

이 문서 및 이후 documentation-only commit은 제품 릴리즈 source가 아니다. v1.11.3 historical identity는 `043abad38f4c3ebc9101463a162614ef67df7536`에 고정한다.
