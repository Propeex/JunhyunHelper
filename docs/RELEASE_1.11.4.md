# RELEASE v1.11.4 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.11.4
status: PUBLIC STABLE / VERIFIED
exact product release source:
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

Tag readback:

```text
refs/tags/v1.11.4
type: commit
target: f9d3497004241ea80193e5a0d242e7219cf04f2a
```

GitHub `/releases/latest`, release `target_commitish`, lightweight tag target, exact-main product source가 모두 v1.11.4 / `f9d3497004241ea80193e5a0d242e7219cf04f2a`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## Exact-main CI artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9741999225
archive bytes: 241,626,166
archive SHA-256:
0af92581d315e2e69d7ff319f1c9968e52fa0093d8635db0eec894e954e2a450
```

이 artifact는 exact-main CI `33345851673`가 생성했다. 해당 CI는 `f9d3497004241ea80193e5a0d242e7219cf04f2a`을 checkout해 478 tests, Release build, Windows x64 self-contained publish, actual published EXE Product UI / Map / MiniMap / Scanner smoke, graceful shutdown, release package audit를 통과한 뒤 artifact를 업로드했다.

Release workflow `33346020525`는 exact-main CI의 artifact `9741999225`를 다운로드했고 Actions artifact digest `0af92581d315e2e69d7ff319f1c9968e52fa0093d8635db0eec894e954e2a450`을 확인했다. 공개 릴리즈용 다른 제품 바이너리를 별도로 다시 빌드하지 않았다.

## Public assets

### Junhyun-Helper.zip

```text
asset id: 537252429
bytes: 80,564,330
SHA-256 / GitHub asset digest:
99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643
```

Release workflow는 `SHA256SUMS.txt`의 stable package entry와 실제 ZIP의 SHA-256을 비교해 다음 값을 검증한 뒤 게시했다.

```text
99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643  Junhyun-Helper.zip
Bytes: 80564330
```

공개 GitHub asset metadata의 digest도 같은 `99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643`이므로 검증된 exact-main package와 공개 ZIP이 일치한다.

### SHA256SUMS.txt

```text
asset id: 537252430
bytes: 86
SHA-256 / GitHub asset digest:
6b81b3816b63b49999e225244214f3d2a3eeabc67fa88da2dd38542c0969f092
```

Release workflow는 manifest에 `Junhyun-Helper.zip` 항목이 정확히 한 번 존재하고, manifest hash와 실제 ZIP hash가 일치하지 않으면 게시를 중단하도록 검증했다. 이번 workflow는 성공했다.

## Product fixes

v1.11.4는 v1.11.3 이후 실사용에서 확인된 MiniMap lifecycle/marker presentation 회귀와 Mini Scanner 우클릭 UX를 수정하는 PATCH 유지보수 릴리즈다.

### 1. MiniMap 최초 생성 지도 동기화

Main Map 선택 변경은 기존에 `ContextIdle` queued synchronization에 의존할 수 있었다. 같은 input turn 안에서 MiniMap window를 처음 만들면 새 window가 이전 tracker map을 읽는 race가 가능했다.

현재는 Main Map 실제 selection 변경 시 `SynchronizeCore()`를 동기적으로 먼저 실행해 product tracker/registry 상태를 갱신하고, 기존 queued reconciliation도 유지한다. 따라서 fresh first-create path와 reused window path 모두 현재 Main Map selection을 사용한다.

### 2. PMC / Scav / Transit extract 실제 렌더링 검증

MiniMap extract 경로는 checkbox state나 데이터 존재만 검사하지 않는다.

- PMC / Scav / Transit filter state를 product bridge와 연결한다.
- `ExtractFaction.Transit` / `ShowTransits` 계약을 유지한다.
- packaged data에서 실제 Transit이 존재하는 map을 runtime smoke가 찾는다.
- 예상 grouped Transit extract 수와 실제 MiniMap visual layer의 rendered Transit marker 수를 비교한다.

### 3. standard marker empty-layer 직접 복구

Donor marker refresh는 새 refresh 시작 시 live standard marker layer를 먼저 비운 뒤 async loading/rebuild를 수행한다. 후속 refresh가 이전 작업을 취소하면 표시 대상 데이터는 이미 로드되어 있는데 live layer만 빈 상태가 남을 수 있었다.

v1.11.4는 표시 대상 marker data가 메모리에 존재하고 standard layer만 일정 시간 비어 있는 경우 또 다른 refresh를 시작하지 않는다. 현재 `MapMarkerDbService`의 loaded data에서 standard marker layer만 직접 재구성하고 현재 floor/filter/marker-scale presentation을 다시 적용한다. deliberate all-hidden 상태와 무한 retry는 복구 대상으로 취급하지 않는다.

### 4. Player Marker Size 격리

Player Marker Size 변경은 donor whole-view update를 호출하지 않고 MiniMap player marker의 `PlayerMarkerScale`만 변경한다.

따라서 다음 unrelated presentation은 보존된다.

- Name Size
- MiniMap Marker Size
- 일반 marker presentation
- Quest / Extract marker presentation

### 5. Mini Scanner 우클릭 메뉴 제거

Mini Scanner의 `현재 결과 교정` context menu와 해당 modal correction path를 제거했다.

유지되는 동작:

- 좌클릭 드래그 이동
- topmost
- recognition/result 표시
- 전역 `교정 데이터 추가` hotkey를 통한 evidence 저장

## Regression coverage

v1.11.4 deterministic suite는 478 tests다.

actual published EXE smoke의 핵심 신규 evidence:

```text
first-minimap-creation-boundary=ok
actual-transit-marker-render=ok
player-marker-size-isolated=ok
standard-marker-direct-recovery=ok
mini-scanner-context-menu=none
```

기존 MiniMap selection/reuse evidence도 유지한다.

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

## Validation issue resolved before publication

최종 release identity를 포함한 첫 PR head에서 제품 build는 성공했지만 `docs/RELEASE_NOTES_V1.11.4.md` 첫 제목이 `# 준현 헬퍼 v1.11.4 Release Notes`라서 `packaging/FIRST_RUN_KO.txt`의 `# 준현 헬퍼 v1.11.4`와 exact heading consistency test 1개가 실패했다.

릴리즈 노트 제목을 canonical `# 준현 헬퍼 v1.11.4`로 맞춘 뒤 최종 feature head `84b56e81171543e289ed417d822c40c9d607d4d3`에서 478/478 및 전체 PR gate가 성공했다. 제품 runtime 결함은 아니었다.

## PR transition note

초기 작업 PR #235는 draft였다. final exact-head workflow가 모두 성공한 뒤 Ready-for-review mutation을 시도했으나 연결된 GitHub GraphQL schema의 `Repository.fullDatabaseId` 오류로 실패했다.

제품 diff/head를 변경하지 않고 #235를 닫은 뒤 동일 validated head로 일반 Ready PR #236을 열었다. PR #236에서 CI `33345630940`, Shutdown Race `33345630896`, Documentation Consistency `33345630871`을 다시 모두 통과한 뒤 main에 병합했다.

## Validation

v1.11.4 exact product release source는 다음을 통과했다.

- 478 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- fresh first-create MiniMap synchronization
- actual Transit marker rendering
- standard marker direct recovery
- Player Marker Size isolation
- Mini Scanner context-menu absence
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback
- public ZIP GitHub digest = verified exact-main ZIP SHA-256

## Compatibility

v1.11.4는 PATCH 유지보수 릴리즈이며 v1.11.3의 data/schema 계약을 변경하지 않는다.

```text
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1~v4
Map donor: SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

## User real-PC validation

v1.11.3에서 보고된 MiniMap/Mini Scanner 실사용 증상은 v1.11.4 변경의 사용자 evidence로 사용했다.

v1.11.4 공개 제품을 실제 사용자 PC/Tarkov 환경에서 최종 확인하는 절차는 자동 release verification과 별개이며 현재 **PENDING**이다.

## Historical identity

v1.11.4 공개 제품의 immutable historical identity는 다음이다.

```text
f9d3497004241ea80193e5a0d242e7219cf04f2a
```

이후 상태 문서 정리를 위한 documentation-only commit은 v1.11.4 제품 릴리즈 소스가 아니다.
