# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

**v1.14.1 PATCH — Farming Guide exact storage-layout stale geometry fail-closed 보강**

v1.14.0에서 확정한 제품 계약은 product-owned exact multi-grid coordinates를 current live grid **count/width/height signature가 정확히 일치할 때만** 사용하고, structure drift 시 finite compact layout으로 fallback하는 것이다.

공개 v1.14.0 closure review에서 resolver가 expected width/height signature를 저장·비교하지 않는 구현 누락이 확인됐다. v1.14.1은 이 실제 회귀를 수정한다.

## Base

Public v1.14.0 exact product source / current main base:

```text
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
```

Public v1.14.0 release:

```text
release id: 380133403
release workflow: 33454002732 — SUCCESS
527 passed / 0 failed / 0 skipped
```

Working branch:

```text
fix/v1.14.1-storage-layout-signature-2026-09-01
```

Target version: **v1.14.1**

v1.14.0 tag/source/assets는 immutable historical identity로 유지하며 교체하지 않는다.

## Confirmed scope

1. `FarmingGuideStorageVisualLayoutResolver`의 product-owned exact profile에 각 grid의 expected width/height signature를 함께 저장한다.
2. exact layout 적용 전에 layout identity, grid count뿐 아니라 **각 grid index의 width/height가 모두 expected signature와 일치하는지** 검증한다.
3. 단 하나의 dimension mismatch라도 exact layout을 거부하고 기존 finite compact fallback으로 보낸다.
4. 기존 non-overlap 검증은 corrupted/internally inconsistent profile에 대한 secondary defense로 유지한다.
5. 현재 product-owned exact profiles의 expected signature는 검증된 factual geometry만 사용한다.
6. v1.14.0에서 width/height mismatch가 non-overlap인 경우 stale exact coordinates를 통과시킬 수 있던 회귀를 deterministic test로 고정한다.
7. 그 외 recursive assembly / inline compatible-item picker / nested storage / drag-drop / Scanner / Map 계약은 변경하지 않는다.

## Completed

- v1.14.0 public release / exact-main / public assets verification 완료.
- v1.14.0 documentation closure PR #252에서 implementation/documentation mismatch review 발견.
- exact v1.14.0 product source code 확인 결과 review가 사실임을 재현·확정.
- PR #252를 unmerged 상태로 닫아 잘못된 `PUBLIC VERIFIED` 계약 기록이 main에 들어가지 않도록 차단.
- fix branch `fix/v1.14.1-storage-layout-signature-2026-09-01` 생성.
- `FarmingGuideStorageVisualLayoutResolver` profile을 coordinate + expected width/height signature로 확장.
- exact layout 적용 전에 각 live grid index의 width/height exact-match 검증 추가.
- A18 / ANA Tactical M1 / current product-owned exact profile의 정상 signature test 추가.
- grid count drift, non-overlapping height drift, width drift 거부 regression 추가.
- desktop version을 1.14.1로 bump.
- `packaging/FIRST_RUN_KO.txt`를 v1.14.1로 정합화.
- `docs/RELEASE_NOTES_V1.14.1.md` 추가.
- `docs/PROJECT_STATE.json` desktop target을 1.14.1로 갱신.

## Current step

- v1.14.1 fix branch exact HEAD로 non-draft PR을 생성한다.
- CI / Shutdown Race / Documentation Consistency / actual published EXE smoke를 수행해 test count와 release-candidate integrity를 확정한다.

## Remaining

- PR exact-head 전체 gate green 확인 및 review thread 처리.
- main 병합.
- exact-main CI / Shutdown Race / Documentation Consistency green 확인.
- exact-main artifact에서 automatic v1.14.1 Release 완료 확인.
- public v1.14.1 tag/source/assets/checksum/latest-stable 무결성 검증.
- 공개 사실값으로 release proof / README / PROJECT_STATE / CURRENT_STATE / STATE / DECISIONS / PRODUCT 정리.
- docs-only closure PR을 검증·병합.
- post-docs Release workflow가 이미 공개된 v1.14.1 assets를 변경하지 않고 immutable existing-release 경로로 성공하는지 확인.
- `docs/ACTIVE_WORK.md`를 `NONE`으로 닫고 최종 public readback 확인.
