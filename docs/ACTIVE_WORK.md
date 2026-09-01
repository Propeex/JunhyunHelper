# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

**v1.14.1 PATCH — Farming Guide exact storage-layout stale geometry fail-closed 보강**

v1.14.0에서 확정한 제품 계약은 product-owned exact multi-grid coordinates를 current live grid **count/width/height signature가 정확히 일치할 때만** 사용하고, structure drift 시 finite compact layout으로 fallback하는 것이다.

공개 v1.14.0 closure review에서 resolver가 실제로는 expected width/height signature를 저장·비교하지 않는 구현 누락이 확인됐다. v1.14.1은 이 실제 회귀를 수정한다.

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
5. 현재 product-owned exact profiles의 expected signature는 검증된 factual geometry만 사용한다. provenance/license 검토 없이 외부 atlas 전체를 제품 데이터로 복사하지 않는다.
6. v1.14.0에서 width/height mismatch가 non-overlap인 경우 stale exact coordinates를 통과시킬 수 있던 회귀를 deterministic test로 고정한다.
7. 그 외 recursive assembly / inline compatible-item picker / nested storage / drag-drop / Scanner / Map 계약은 변경하지 않는다.

## Reproduction / evidence

v1.14.0 exact source의 `FarmingGuideStorageVisualLayoutResolver.TryResolve`는:

- profile/layout name 확인
- live grid count 확인
- live width/height positive 확인
- current dimensions로 만든 rectangle non-overlap 확인

까지만 수행한다.

Expected dimensions 자체가 profile에 없으므로, 예를 들어 기존 1×1 grid가 1×2로 바뀌어도 다른 rectangle과 겹치지 않으면 stale exact coordinates를 계속 사용할 수 있다.

검증된 current geometry evidence를 이용한 product-owned profile signature:

```text
A18:
(1×2) × 10, (1×1) × 5

ANA Tactical M1:
(1×2) × 4, (2×2) × 2, (1×1) × 4

mbss_rig profile order:
1×1, 1×1, 1×1, 1×2, 1×2, 2×1, 1×3
```

## Completed

- v1.14.0 public release / exact-main / public assets verification 완료.
- v1.14.0 documentation closure PR #252에서 implementation/documentation mismatch review 발견.
- exact v1.14.0 product source code 확인 결과 review가 사실임을 재현·확정.
- PR #252를 unmerged 상태로 닫아 잘못된 `PUBLIC VERIFIED` 계약 기록이 main에 들어가지 않도록 차단.
- v1.14.0 public tag/source/assets는 변경하지 않고 v1.14.1 PATCH로 교정하기로 결정.
- fix branch `fix/v1.14.1-storage-layout-signature-2026-09-01` 생성.

## Current step

- resolver profile에 expected grid width/height signature를 추가한다.
- exact signature success/mismatch 회귀 테스트를 수정·추가한다.
- v1.14.1 version/FIRST_RUN/release target 문서를 정합화한다.

## Remaining

- Core resolver 수정.
- deterministic regression 보강 및 exact test count 확인.
- v1.14.1 version / FIRST_RUN / release notes / current target state 정리.
- non-draft PR 생성 및 exact-head CI / Shutdown Race / Documentation Consistency / published EXE smoke green 확인.
- main 병합.
- exact-main CI / Shutdown Race / Documentation Consistency green 확인.
- exact-main artifact에서 automatic v1.14.1 Release 완료 확인.
- public v1.14.1 tag/source/assets/checksum/latest-stable 무결성 검증.
- 공개 사실값으로 release proof / README / PROJECT_STATE / CURRENT_STATE / STATE / DECISIONS / PRODUCT 정리.
- docs-only closure PR을 검증·병합.
- post-docs Release workflow가 이미 공개된 v1.14.1 assets를 변경하지 않고 immutable existing-release 경로로 성공하는지 확인.
- `docs/ACTIVE_WORK.md`를 `NONE`으로 닫고 최종 public readback 확인.
