# RELEASE v1.12.0 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.12.0
status: PUBLIC STABLE / VERIFIED
exact product release source:
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

Tag readback:

```text
refs/tags/v1.12.0
type: commit
target: b2fcec460df256c581e87b53c6293dc4d2177b9c
```

GitHub `/releases/latest`, release `target_commitish`, lightweight tag target, exact-main product source가 모두 v1.12.0 / `b2fcec460df256c581e87b53c6293dc4d2177b9c`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## Exact-main CI artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9742966369
archive bytes: 241,651,154
archive SHA-256:
c6122103fefa1c0b5ffd30787a4a60f6af1e151c3dd4694dca3584c7081145e9
```

이 artifact는 exact-main CI `33348916340`가 exact product source를 checkout해 Release build, deterministic tests, Windows x64 self-contained publish, actual published EXE Product UI / Map / Scanner smoke, graceful shutdown, release package audit를 통과한 뒤 업로드했다.

Release workflow `33349066686`는 이 verified artifact를 다운로드해 공개 package를 검증·게시했다. 공개 릴리즈용 다른 제품 바이너리를 별도로 다시 빌드하지 않았다.

## Public assets

### Junhyun-Helper.zip

```text
asset id: 537304923
bytes: 80,572,903
SHA-256 / GitHub asset digest:
d8ad140ee39ef533471a229ae01e80bc4ad7baeb5b513490c645bdbd3af137c0
```

Release workflow는 `SHA256SUMS.txt`의 package hash와 실제 ZIP hash를 비교한 뒤 게시했다. 공개 GitHub asset metadata의 digest도 같은 값이므로 검증된 exact-main package와 공개 ZIP이 일치한다.

### SHA256SUMS.txt

```text
asset id: 537304924
bytes: 86
SHA-256 / GitHub asset digest:
76a0dfb4e7734001a938798c2f6180f815d79b914e7d2b3933423f1f827673d7
```

## Product changes

### Quest staged task-pool availability

사용자 실사용에서 fresh profile은 `확인 필요 0`이지만 일부 Quest/Trader 진행 뒤 `확인 필요 49`가 나타났다. current EFT 1.1 audited LL1 task-pool Quest 48개와 증상이 정확히 겹쳤고, 기존 compatibility가 과거 stage hidden value를 알 수 없다는 이유로 trader LL 상승 뒤에도 pool을 unknown으로 유지하는 것이 주원인이었다.

v1.12.0은 exact ProfileVariable 우선과 current-stage fail-closed를 유지하면서, 현재 trader LL이 audited pool stage를 이미 넘어섰다면 그 과거 stage threshold가 충족된 runtime-only availability floor를 사용한다. 이 floor는 hidden server counter의 exact 값을 저장하거나 추정하지 않는다. 구조 drift는 계속 fail-closed하며 Future Needed Items / cleanup은 이 current UI compatibility를 사용하지 않는다.

### Hideout search clear alignment

공통 clear glyph sibling overlay가 TextBox의 전체 outer margin을 반영하도록 수정해 Hideout 검색창의 `×`만 수직으로 어긋나던 문제를 해결했다.

### 김태영 PC 진단

메인 헤더 좌측 프로필 이미지에 opt-in PC diagnostic entry point를 추가했다.

```text
프로필 이미지 클릭
→ 본인 확인
→ display/GPU/HDR/capture/Scanner 진단
→ Desktop ZIP
→ hyune4784@naver.com 으로 전달 안내
```

자동 업로드/자동 이메일은 없다. display/GPU/driver/HDR/color/luminance, allowlisted capture/overlay app, Scanner 상태, display screenshots, Tarkov screen-copy/PrintWindow comparison과 bitmap 통계를 수집한다. 사용자명·컴퓨터명·IP/MAC·네트워크 목록·credential·전체 environment/process dump·설치 경로는 제외한다. 화면 PNG에 실제 화면 내용이 포함될 수 있음을 실행 전에 알린다.

상세 계약은 `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`를 따른다.

## Regression coverage

v1.12.0 deterministic suite는 482 tests다.

신규/강화된 회귀 범위:

- LL1→LL2 past-stage task-pool satisfaction
- LL2→LL3 past-stage task-pool satisfaction
- exact ProfileVariable precedence
- current-stage conservative behavior
- structural drift fail-closed
- shared search clear vertical-margin contract
- header avatar diagnostic entry point
- diagnostic local/privacy-bounded bundle contract

기존 Product UI / Ammo / Map / Factory / MiniMap / Scanner published EXE smoke와 Shutdown Race gate도 유지했다.

## PR transition note

초기 PR #237은 draft였다. exact feature-head CI/Shutdown Race/Documentation Consistency가 모두 성공한 뒤 Ready-for-review mutation을 시도했으나 연결된 GitHub GraphQL schema의 `Repository.fullDatabaseId` 오류로 실패했다.

제품 diff/head를 변경하지 않고 #237을 닫은 뒤 동일 validated head `5216ab410c8a4384aee7d9f1a69fbd30302ad0a8`로 일반 PR #238을 생성해 main에 merge commit `b2fcec460df256c581e87b53c6293dc4d2177b9c`로 병합했다. 이 전환을 위한 workaround성 제품 코드는 없다.

## Validation

v1.12.0 exact product release source는 다음을 통과했다.

- 482 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup / Product UI / Map / Scanner smoke
- Quest past-stage availability regression
- Hideout clear / diagnostic source-contract regression
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback
- public ZIP GitHub digest = verified package SHA-256

## Compatibility

```text
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1~v4
Map donor: SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

v1.11.4 → v1.12.0 mandatory content/user/settings migration은 없다.

## User real-PC validation

v1.12.0 공개 제품을 실제 사용자 PC/Tarkov 환경에서 최종 확인하는 절차와 김태영 PC diagnostic ZIP의 실제 수집·분석은 자동 release verification과 별개이며 현재 **PENDING**이다.

## Historical identity

v1.12.0 공개 제품의 immutable historical identity는 다음이다.

```text
b2fcec460df256c581e87b53c6293dc4d2177b9c
```

이후 상태 문서 정리를 위한 documentation-only commit은 v1.12.0 제품 릴리즈 소스가 아니다.
