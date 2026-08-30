# RELEASE v1.11.3 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.11.3
status: PUBLIC STABLE / VERIFIED
exact product release source:
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

Tag readback:

```text
refs/tags/v1.11.3
type: commit
target: 043abad38f4c3ebc9101463a162614ef67df7536
```

GitHub `/releases/latest`, release target, lightweight tag target, exact-main product source가 모두 v1.11.3 / `043abad38f4c3ebc9101463a162614ef67df7536`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## Exact-main CI artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9734538554
archive bytes: 241,607,396
archive SHA-256:
cf10ab86f31c44dff00414b9f4e47ff9bf5a64df18210084bd2b41c42e3ac2a7
```

이 artifact는 exact-main CI `33319592093`가 생성했다. 해당 CI는 `043abad38f4c3ebc9101463a162614ef67df7536`을 checkout해 474 tests, Release build, published EXE smoke, package checksum audit를 통과한 뒤 artifact를 업로드했다.

Release workflow `33319769016`의 단계는 `Checkout verified main commit` → `Download verified Windows x64 artifacts` → `Verify and publish stable GitHub release`이며 성공했다. 공개 release용 다른 제품 바이너리를 별도로 다시 빌드하지 않았다.

## Public assets

### Junhyun-Helper.zip

```text
asset id: 536758239
bytes: 80,558,970
SHA-256 / GitHub asset digest:
e43892ecafc9920a7e3b7295f94b8a5324865977028b3573437d8ff7de4f327e
```

Exact-main package audit도 동일 ZIP에 대해 다음을 출력했다.

```text
SHA256 e43892ecafc9920a7e3b7295f94b8a5324865977028b3573437d8ff7de4f327e Junhyun-Helper.zip
Bytes: 80558970
```

### SHA256SUMS.txt

```text
asset id: 536758240
bytes: 86
SHA-256 / GitHub asset digest:
5b3cc0468ad6a11076b547883fbd16d1276c74bc51779251c0c3421a070d63c3
```

Exact-main package audit는 `SHA256SUMS.txt`에 stable package가 정확히 한 번 존재하며 manifest hash와 실제 ZIP hash가 일치함을 확인했다.

## Product fixes

v1.11.3은 v1.11.2 실사용과 사용자 Scanner diagnostics에서 확인된 유지보수 문제를 수정하는 PATCH 릴리즈다.

### 1. Items / Hideout 검색 clear 실제 lifecycle

v1.11.2 사용자 화면에서는 Items/Hideout에 검색어가 입력돼도 canonical inline `×`가 보이지 않았다.

공유 behavior 자체가 아니라 실제 page lifecycle attach가 안정적으로 보장되지 않은 문제였다. Items/Hideout는 Loaded/template boundary에서 canonical `ProductSearchClearButtonBehavior`를 attach하도록 수정했다.

현재 계약:

- empty query → `×` hidden
- typed query → inline `×` visible
- click → query clear
- 기존 TextChanged 검색/필터 path 재사용
- clear 뒤 TextBox focus 복구
- Quest/Items/Hideout 동일 behavior

또한 기존 published smoke가 직접 behavior를 attach한 뒤 결과를 검사해 실사용 회귀를 숨길 수 있던 false-positive 검증 경로를 제거했다. 최종 smoke는 실제 page lifecycle 결과를 확인한다.

### 2. Map 지도 마커 패널 탈출구 클리핑

v1.11.2는 expanded marker panel 높이를 순간적인 `MapMarkersContent.DesiredSize`에 맞출 수 있었다. donor content가 완전히 정착하기 전에 짧은 DesiredSize가 측정되면 큰 창에서도 panel이 짧게 고정되어 하단 탈출구 항목이 잘릴 수 있었다.

v1.11.3은 expanded panel을 available-height viewport로 변경했다.

- 정상 큰 창에서 map 영역의 가용 높이를 사용
- inner checkbox viewport가 panel body를 채움
- 실제 content overflow에서만 Auto vertical scrollbar 렌더
- collapsed 상태에서는 explicit height constraint 해제
- published EXE smoke가 panel height/body fill/rendered scrollbar state를 직접 검증

### 3. Scanner 교정 이미지 mouse-wheel zoom

Saved Case/교정 화면 screenshot에 마우스 휠 확대/축소를 추가했다.

- fit~8× zoom multiplier
- pointer anchor 보존
- 확대 시 scroll/pan 가능
- source image/canvas는 원본 pixel dimensions 유지
- Ground Truth rectangle/manual selection은 항상 source pixel coordinate 사용

최초 runtime smoke에서 확대→축소 시 fit scale이 약 `0.573 → 0.596`으로 변하는 실제 문제를 잡았다. Auto scrollbar가 `ViewportWidth/Height`를 바꿔 fit 기준이 zoom state에 의존한 것이 원인이었다. stable arranged ScrollViewer bounds를 사용하도록 수정한 뒤 최종 published smoke를 통과했다.

### 4. Scanner correction evidence timing defect

사용자 calibration/diagnostics batch의 저장 JSON에는 5건 모두 `NOT_RUN`이 남았지만 runtime log에서는 실제 OCR/matcher가 실행된 case가 확인됐다.

대표적으로:

- `Corrugated hose 주름진 호스`: title OCR까지 도달했으나 Latin glyph 일부가 Han/CJK로 오인되어 `OCR_INVALID_CHARACTERS` fail-closed
- `7.62x25mm TT P gl ammo pack (25 pcs)`: Ground Truth와 같은 nearest candidate까지 찾았지만 약 0.846 confidence / 0.038 margin으로 `LOW_CONFIDENCE` fail-closed

원인은 분석 완료 debug frame 뒤의 빠른 geometry-only capture가 single latest frame을 덮어쓰고, 그 뒤 correction save가 실행되면서 분석 semantics가 유실되는 timing defect였다.

v1.11.3은 correction snapshot에 한해서 다음 fail-closed 조건을 모두 만족할 때만 최근 analyzed semantics를 보존한다.

- 동일 non-empty title signature
- 동일 capture mode
- 3초 이내

screenshot/geometry는 최신 frame을 그대로 유지한다. retained semantics는 live recognition 결정에는 사용하지 않는다. OCR/matcher/candidate acceptance threshold나 invalid-character policy도 완화하지 않았다.

## Regression coverage

추가·강화된 결정적 계약:

- Items/Hideout shared inline clear가 실제 lifecycle에서 활성화됨
- published smoke가 search-clear behavior를 스스로 설치하지 못함
- marker panel이 available height를 사용하고 real overflow에서만 scroll
- correction image zoom이 source-pixel coordinate 의미를 보존
- zoom-in/out 후 stable fit scale 복귀
- recent analyzed semantics는 same title signature/capture mode/3초에서만 correction snapshot에 carry
- live recognition path는 carry를 사용하지 않음

## Validation issues resolved before publication

### Stale v1.8.3 source-string test

첫 CI에서 474개 중 오래된 maintenance source-string test 1개가 삭제된 내부 변수명 `requestedPanelHeight`를 요구해 실패했다. 제품 Release build는 성공했으며 현재 available-height 제품 계약과 맞지 않는 stale test였다. 테스트를 실제 계약으로 갱신한 뒤 474/474 PASS를 확인했다.

### Correction zoom fit-scale smoke

다음 published EXE smoke가 실제 scrollbar-dependent fit-scale 불안정을 검출했다. 구현을 stable arranged bounds 기준으로 수정한 뒤 최종 runtime smoke가 성공했다.

## PR transition note

초기 작업 PR #233은 draft였다. 모든 final exact-head workflow가 성공한 뒤 Ready-for-review GraphQL mutation을 시도했으나 연결된 GitHub schema의 `Repository.fullDatabaseId` 필드 오류로 실패했다.

제품 diff/head를 변경하지 않고 #233을 닫은 뒤 동일 head `bf5df86fa638832af53a18b4ec922db81e20087d`로 일반 Ready PR #234를 열었다. #234에서 Documentation Consistency `33319386455`, CI `33319386444`, Shutdown Race `33319386465`를 다시 모두 통과한 뒤 main에 병합했다.

이 문제는 준현 헬퍼 제품 코드 결함이 아니며 공개 product source에 tooling workaround 코드가 들어가지 않았다.

## Validation

v1.11.3 exact product release source는 다음을 통과했다.

- 474 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items / Hideout real lifecycle conditional search clear
- Map marker available-height / body fill / rendered scrollbar validation
- Scanner correction zoom / stable fit / source-pixel coordinate validation
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified release workflow
- public tag/release/assets/latest-stable readback

## User real-PC validation

v1.11.2 실사용 증상과 Scanner diagnostics는 v1.11.3 변경의 실제 사용자 evidence로 사용했다.

v1.11.3 공개 제품을 실제 사용자 PC/Tarkov 환경에서 최종 확인하는 절차는 자동 release verification과 별개이며 현재 **PENDING**이다.

## Historical identity

v1.11.3 공개 제품의 immutable historical identity는 다음이다.

```text
043abad38f4c3ebc9101463a162614ef67df7536
```

이후 상태 문서 정리를 위한 documentation-only commit은 v1.11.3 제품 릴리즈 소스가 아니다.
