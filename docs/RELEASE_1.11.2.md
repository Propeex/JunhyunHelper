# RELEASE v1.11.2 — PUBLIC / VERIFIED

Date: **2026-08-30 KST**

## Release identity

```text
version: v1.11.2
status: PUBLIC STABLE / VERIFIED
exact product release source:
5822757f6490ec82aab33793752e48de14490628
PR: #232 — MERGED
superseded draft PR: #231 — CLOSED / NOT MERGED
PR exact-head CI: 33307979144 — SUCCESS
PR exact-head Shutdown Race CI: 33307979132 — SUCCESS
PR exact-head Documentation Consistency: 33307979269 — SUCCESS
exact-main CI: 33308162829 — SUCCESS
exact-main Shutdown Race CI: 33308162797 — SUCCESS
exact-main Documentation Consistency: 33308162850 — SUCCESS
release workflow: 33308291656 — SUCCESS
release id: 379257951
published UTC: 2026-08-30T11:11:52Z
470 passed / 0 failed / 0 skipped
```

Tag readback:

```text
refs/tags/v1.11.2
type: commit
target: 5822757f6490ec82aab33793752e48de14490628
```

GitHub `/releases/latest`, release target, lightweight tag target, exact-main product source가 모두 v1.11.2 / `5822757f6490ec82aab33793752e48de14490628`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## Exact-main CI artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9731167378
archive bytes: 241,597,223
archive SHA-256:
5eef3f620d46f3ac3c7990ec18fdcf46877741fc2c1647a856b3accb2fa26c8b
```

이 artifact는 exact-main CI `33308162829`가 생성했다. Release workflow는 이 검증된 artifact를 다운로드해 사용하며 다른 바이너리를 다시 빌드하지 않았다.

## Public assets

### Junhyun-Helper.zip

```text
asset id: 536514791
bytes: 80,554,866
SHA-256 / GitHub asset digest:
d013ac2d423d2a83c49e1e6483dcad038a3792a5b865c1400085fd56e25592a9
```

### SHA256SUMS.txt

```text
asset id: 536514792
bytes: 86
SHA-256 / GitHub asset digest:
4860aceab06843707951dcd50951a62843d40ef7a2ea2a9d8efa7972847aa657
```

Release workflow는 공개 전에 exact-main artifact의 EXE ProductVersion, `FIRST_RUN_KO.txt`, ZIP checksum manifest를 검증하고 `Junhyun-Helper.zip`과 `SHA256SUMS.txt`를 stable release asset으로 공개했다.

## Product fixes

v1.11.2는 v1.11.1 실사용에서 보고된 세 가지 유지보수 문제를 수정하는 PATCH 릴리즈다.

### 1. 레이드 중 교정 데이터 hotkey

기존에는 `교정 데이터 추가` 전역 단축키로 evidence 저장에 성공하면 Saved Cases/교정 데이터 창이 자동으로 열리고 Main Window focus가 이동했다.

v1.11.2는 hotkey를 capture/save 전용으로 고정한다.

- latest Scanner evidence를 distinct Saved Case로 저장
- 저장 성공 시 Mini Scanner에 `저장 완료` transient feedback 표시
- Saved Cases/교정 데이터 창 자동 open 제거
- Main Window/Scanner focus 강제 이동 제거
- no-evidence exact status 유지
- evidence-only Saved Case 유지
- `GroundTruthItemName = null`, `UserConfirmed = false` 유지
- duplicate explicit save 허용 유지
- hotkey가 Ground Truth를 자동 생성·추측하지 않음

레이드 중 저장만 하고 검토는 사용자가 나중에 직접 열 수 있다.

### 2. Items / Hideout 검색창 clear UI

v1.11.1에서 Items/Hideout에 추가된 always-visible 별도 `×` Button이 기존 product search behavior와 중복되어 Quest 검색창과 시각적으로 달랐다.

v1.11.2는 duplicate installer/partial을 제거하고 canonical `ProductSearchClearButtonBehavior` 하나로 통일한다.

- empty query → `×` hidden
- typed query → inline `×` visible
- click → query clear
- 기존 TextChanged 검색/필터 path 재사용
- clear 뒤 TextBox focus 복구
- Quest/Items/Hideout 동일 behavior

### 3. Map / MiniMap player facing direction

player 위치에는 각 맵의 `playerMarkerTransform` affine transform이 적용됐지만 heading은 같은 좌표계 변환을 일관되게 사용하지 않았다.

기존 Main Map은 Factory `+90°`, Labs `-90°`만 이름 기반으로 보정했고 MiniMap은 raw yaw를 사용했다. 따라서 Factory MiniMap에서 약 90° 오차가 날 수 있었으며 Reserve/Labyrinth처럼 transform 자체에 회전 성분이 있는 맵도 일반적으로 처리되지 않았다.

v1.11.2는 player 위치 affine의 선형부 `[a,b;c,d]`를 heading vector에도 적용한다.

- Factory/Labs 회전을 동일 일반식으로 재현
- Reserve/Labyrinth 포함 회전 transform 처리
- 현재 전체 map player transform에 동일 규칙 적용
- Main Map과 MiniMap 동일 projected heading 사용
- 위치 placement 계약은 그대로 유지
- 맵 이름별 angle exception을 추가하지 않음

## Regression coverage

추가·강화된 결정적 계약:

- correction hotkey 성공 경로가 modal review window를 열지 않음
- correction hotkey evidence-only / no automatic Ground Truth 보존
- Items/Hideout/Quest canonical conditional inline search clear behavior
- Factory/Labs/Reserve/Labyrinth heading known orientation
- 현재 모든 `playerMarkerTransform` heading projection 유효성
- Main Map/MiniMap donor render 이후 projected heading 최종 적용

Published EXE smoke도 실제 WPF SearchBox의 empty → typed → clear 상태를 확인하도록 강화했다.

## Validation

v1.11.2 exact product release source는 다음을 통과했다.

- 470 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items / Hideout conditional search clear runtime behavior
- heading projection deterministic regressions
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified release workflow
- public tag/release/assets/latest-stable readback

## Validation issues resolved before publication

### Release notes identity gate

v1.11.2 version을 올린 직후 `docs/RELEASE_NOTES_V1.11.2.md`가 아직 없어 `ReleaseIdentityTests` 1건이 실패했다. 당시 469 tests는 통과했고 제품 Release build도 성공했다. 릴리즈 노트를 추가해 identity contract를 완성한 뒤 최종 470/470 PASS를 확인했다.

### Search clear smoke lifecycle

초기 published EXE smoke에서 `Items search clear button was not rendered`가 발생했다. 제품 오류가 아니라 Scanner 탭에서 smoke가 실행될 때 `Collapsed` 상태의 Items/Hideout page가 normal `Loaded` attachment를 아직 거치지 않았는데도 control이 이미 존재한다고 가정한 테스트 lifecycle 문제였다.

최종 smoke는 canonical behavior를 직접 attach한 뒤 실제 empty/typed/clear 상태 전이를 검증한다.

## PR transition note

초기 작업 PR #231은 draft였다. 연결된 ready-for-review GraphQL mutation이 GitHub schema의 `Repository.fullDatabaseId` 호환 오류로 실패했다. 제품 diff나 branch를 변경하지 않고 draft #231을 닫은 뒤 동일 branch/head를 ready PR #232로 열어 exact-head CI를 다시 검증하고 병합했다.

이 문제는 준현 헬퍼 제품 코드 결함이 아니며 공개 product source에 별도 우회 코드가 들어가지 않았다.

## User real-PC validation

사용자의 실제 PC/Tarkov 플레이 환경 실사용 검증은 자동 release verification과 별개이며 현재 **PENDING**이다.

## Historical identity

v1.11.2 공개 제품의 immutable historical identity는 다음이다.

```text
5822757f6490ec82aab33793752e48de14490628
```

이후 상태 문서 정리를 위한 documentation-only commit은 v1.11.2 제품 릴리즈 소스가 아니다.
