# RELEASE v1.13.0 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.13.0
status: PUBLIC STABLE / VERIFIED
exact product release source:
103ade0c5d54ffb59a6844330d19a930899c12fb
PR: #241 — MERGED
validated feature head: 30424d0cc401a62b415dd772c52e5de4f6c931ee
PR exact-head CI: 33358670772 — SUCCESS
PR exact-head Shutdown Race CI: 33358670694 — SUCCESS
PR exact-head Documentation Consistency: 33358670722 — SUCCESS
exact-main CI: 33358877907 — SUCCESS
exact-main Shutdown Race CI: 33358877912 — SUCCESS
exact-main Documentation Consistency: 33358877946 — SUCCESS
release workflow: 33359054856 — SUCCESS
release id: 379519928
published UTC: 2026-08-31T05:01:47Z
494 passed / 0 failed / 0 skipped
```

Tag `refs/tags/v1.13.0`, release `target_commitish`, GitHub `/releases/latest`, exact-main product source가 모두 `103ade0c5d54ffb59a6844330d19a930899c12fb`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

Draft PR #240은 기능/검증 문제가 아니라 connected GitHub draft→ready GraphQL 전환 도구의 schema mismatch 때문에 닫았다. 동일한 source branch와 검증 완료 HEAD로 non-draft PR #241을 생성해 병합했으며 제품 bytes/feature source는 변경되지 않았다.

## Exact-main artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9746074189
archive bytes: 241,774,204
archive SHA-256:
d1119a9931695016085e71bd84514f15c0bd5b051734deddce6dfb43053cf94e
```

Release workflow는 위 exact-main CI artifact를 digest 검증과 함께 다운로드했다. 공개용 제품을 별도로 다시 빌드하지 않았다.

## Public assets

```text
Junhyun-Helper.zip
asset id: 537475557
bytes: 80,613,758
SHA-256 / GitHub digest:
cbd8bafbf31ae65ecc659b15fc90a17408b87ecacdd9545c7b78de81c1835326

SHA256SUMS.txt
asset id: 537475554
bytes: 86
SHA-256 / GitHub digest:
c3f174348668c0dfe9fc7b0ebcf5c1c2846b802b60a78f205833f6ffcb9f6a71
```

Release workflow에서 `SHA256SUMS.txt`의 `Junhyun-Helper.zip` 항목과 실제 ZIP SHA-256을 비교해 동일한 `cbd8bafbf31ae65ecc659b15fc90a17408b87ecacdd9545c7b78de81c1835326`임을 확인한 뒤 release를 공개했다.

## Product changes

v1.13.0은 사용자가 명시적으로 확정한 새 MINOR 기능인 **파밍 가이드**의 첫 제품 slice다.

- Scanner 오른쪽에 `파밍 가이드` first-class section을 추가했다.
- 헤드셋, 헬멧, 얼굴/안경, 방탄복/아머드 리그, 무기, 권총 등 raid-start 장비 상태를 구성한다.
- Pocket / Rig / Backpack / Secure Container / Special Slot 수납 구조를 표현한다.
- 검색 결과에서 실제 Tarkov `width × height` footprint로 drag한다.
- drag 중 `R` 키로 90도 회전한다.
- grid snap, bounds/overlap, 연속 공간, current filter를 검증하고 valid/invalid 상태를 표시한다.
- current validated Tarkov item source의 storage grids, filters, equipment slots, attachment slots, armor slots, conflicts를 사용한다.
- 장비 attachment와 교체형 armor plate를 별도 설정 UI에서 편집한다.
- 전체 출발 상태를 프리셋으로 저장/복원한다.
- 근접무기와 PMC 인식표는 per-profile preset과 분리된 fixed setting이다.
- 총 무게와 사용/전체 storage cell을 표시한다.
- 내용물이 든 carrier를 다른 carrier로 덮어써서 내부 아이템이 사라지는 경로를 fail closed한다.
- Tarkov 변화로 오래된 preset의 grid/filter 배치가 불가능해지면 impossible placement를 복원하지 않고 current content 기준으로 정리한다.

이번 릴리즈에는 파밍 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 추천 연동, 실제 raid inventory grid 좌표의 지속적인 1:1 동기화를 포함하지 않는다.

## Persistence / schema

Farming Guide 사용자 상태는 Game Content와 분리해 저장한다.

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema: v1
```

Farming Guide item structure를 canonical content에 보존하기 위해 Content write schema는 v9다.

```text
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1~v4
Map donor: SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

기존 v1.12.x user.db/Scanner 설정에 대한 mandatory migration은 없다. Game Content는 현재 source에서 v9 snapshot으로 다시 생성/활성화할 수 있으며 v3~v9 read compatibility를 유지한다.

## Regression coverage

v1.13.0 exact-main deterministic suite는 494 tests다. 주요 신규 검증은 다음을 포함한다.

- item footprint / rotation / bounds / overlap
- fragmented space와 contiguous packing
- carrier contents 보호
- persisted placement current-content sanitization
- preset full-state round-trip
- fixed melee/dogtag와 per-profile preset 분리
- attachment / armor plate / storage grid importer
- Content v9 round-trip 및 old readable schema compatibility
- MainWindow Farming Guide section lifecycle

Exact-main은 Windows Release build, Windows x64 self-contained publish, actual published EXE Product UI / Farming Guide / Map smoke, graceful shutdown, active-async Shutdown Race, clean portable root, package/checksum audit, Documentation Consistency, exact-main artifact upload을 통과했다.

## Public verification

- release workflow `33359054856`: SUCCESS
- `/releases/latest`: v1.13.0
- tag ref `refs/tags/v1.13.0`: exact product source와 일치
- release target: exact product source와 일치
- public ZIP asset metadata/digest: verified
- checksum asset metadata/digest: verified
- stable release: `draft=false`, `prerelease=false`

후속 documentation-only commit은 v1.13.0 product release source가 아니다. v1.13.0 historical product identity는 `103ade0c5d54ffb59a6844330d19a930899c12fb`에 고정한다.
