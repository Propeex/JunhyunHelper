# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-30 KST**

## Goal

사용자 실사용에서 확인된 Map/MiniMap/marker 회귀를 수정하고, Scanner correction hotkey 및 ammo/ammo-pack pickup 판단을 추가하며 Hideout FIR requirement/cleanup 정확도를 보강한 **v1.11.0**을 구현·검증·릴리즈한다.

## Base / branch

```text
base main: fdbf321964035ed4b7eb5c793fc2418031d9f518
public stable: v1.10.1
working branch: feature/v1.11.0-scanner-ammo-map-maintenance-2026-08-30
PR: #225 (draft)
latest code checkpoint commit: 64bd4189f27b62a29a6b7256cea1b8337924e3de
```

새 사용자 기능이 포함되므로 `docs/VERSIONING.md`에 따라 목표 버전은 **v1.11.0**이다.

## Confirmed scope

### Map / MiniMap maintenance

1. 프로그램 시작 뒤 Main Map을 A→B로 변경하고 MiniMap을 처음 켜도 첫 visible frame부터 현재 B와 동기화될 것.
2. 지도 marker 목록에서 extract 관련 checkbox가 late donor initialization 때문에 누락되지 않을 것.
3. Player marker size 변경이 MiniMap marker size/name size의 실제 렌더 상태를 초기값으로 되돌리지 않을 것.
4. marker refresh 취소/재진입으로 standard marker layer가 비어 버리는 경우 안정적으로 복구할 것.

### Scanner / Needed Items maintenance

5. Scanner/Mini Scanner 표시에서 flea `최저가` 항목을 제거하되 underlying flea minimum data/model은 호환 목적으로 유지한다.
6. Hideout requirement의 FIR 여부를 source semantics 그대로 보존한다. `attributes.foundInRaid`를 canonical requirement에 반영한다.
7. Content update로 FIR 의미가 바뀌면 Needed Items/cleanup derived state를 현재 canonical requirement에 맞춰 재계산한다.

### New features

8. Scanner correction-case capture global hotkey를 추가한다. 최신 exact Scanner evidence를 Saved Case로 저장하되 hotkey 자체는 Ground Truth를 생성하거나 확정하지 않는다.
9. caliber별 penetration 순위와 사용자의 현재 Trader LL/완료 퀘스트를 기준으로 direct-money purchase 가능 범위를 계산해 ammo pickup 판단을 제공한다. barter/craft/flea/LL 미달/확인되지 않은 quest unlock은 구매 가능으로 보지 않는다.
10. Ammo pack은 authoritative `containsItems` relation을 우선해 contained canonical ammo로 resolve하며, authoritative relation이 비어 있을 때만 제한적인 name fallback을 사용한다.
11. 다른 사용자로부터 correction data를 네트워크 자동 수집하는 기능은 이번 범위에서 추가하지 않는다.

## Root cause / design findings

- **MiniMap first-open stale map**: active MiniMap window가 없을 때 desired Main Map selection이 registry에서 보존되지 않아 첫 overlay 생성 시 donor persisted selection이 먼저 노출될 수 있었다. Registry가 최신 map key를 window 유무와 무관하게 보존하고 Register 시 replay하도록 수정했다.
- **Extract checkbox 일시 누락**: marker settings bridge 초기화 시 donor의 extract checkbox가 아직 생성되지 않은 경우가 있으나 기존 late-row retry는 extract row를 다시 시도하지 않았다. Extract row도 retry 대상으로 포함하고 이미 이동된 row는 idempotent하게 유지한다.
- **Player marker size가 다른 표시 설정을 되돌림**: donor `UpdateMapView()`가 marker container transform을 다시 적용하므로 Player Marker Size 변경 뒤 Junhyun MiniMap marker scale presentation이 덮어써질 수 있었다. donor update 직후 전체 Junhyun marker presentation을 다시 projection한다.
- **marker 전체 소실**: donor marker refresh가 container를 clear한 뒤 다른 refresh에 의해 취소될 수 있는 lifecycle 경로가 확인됐다. 동일 map/floor에서 이전에 정상 marker가 존재했는데 0개 상태가 지속될 때만 1회 refresh를 재요청해 빈 layer를 복구하고 사용자 의도에 의한 전체 숨김에는 retry loop가 생기지 않게 했다.
- **Hideout FIR**: Tarkov hideout item requirement FIR 의미는 requirement `attributes.foundInRaid`에 존재하며 top-level만 읽으면 유실된다. importer가 attribute 값을 canonical requirement로 보존하도록 수정했다.
- **Ammo pack**: current Tarkov item schema가 `containsItems` relation을 제공하므로 이를 authoritative mapping으로 사용하고, 비어 있는 relation에서만 좁은 naming fallback을 허용한다.

## Completed

- MiniMap latest Main Map selection snapshot/replay 및 first-open lifecycle repair
- Extract checkbox late-load retry + empty extract presentation recovery
- Player Marker Size 변경 뒤 MiniMap marker/name presentation 재적용
- standard marker empty-layer one-shot recovery
- Scanner flea minimum 사용자 표시 제거, underlying data/model 보존
- Hideout `attributes.foundInRaid` import + FIR inventory/cleanup regressions
- configurable global `교정 데이터 추가` hotkey, no-evidence status, Saved Case evidence capture, no automatic Ground Truth, duplicate explicit saves, manager 종료 후 Scanner focus
- independent ammo pickup evaluator + exact ranking examples/boundaries
- Trader LL/direct-money/quest-unlock-aware purchase availability projection
- authoritative ammo pack contained-ammo mapping + empty-relation fallback tests
- Scanner/Mini Scanner ammo pickup presentation
- correction hotkey product smoke
- v1.11 Map maintenance source-contract regressions

## Validation completed before latest regression-contract commit

At commit `a8bc11070b232acf9ddd767eed963c7bc01ec742`:

- Documentation Consistency: **PASS**
- Shutdown Race CI: **PASS**
- CI: **PASS**
  - Windows Release desktop build: PASS
  - core tests: PASS
  - Windows x64 publish: PASS
  - startup + Product UI + Map + graceful shutdown smoke: PASS
  - release package build/verification: PASS
  - artifact upload: PASS
- produced CI artifact: `JunhyunHelper-win-x64`
- artifact digest: `sha256:f67299844e2a9ee9c167d8484f3acd3ae69306dfe390594e712b9c38fb875e5e`

Current live references also confirm the reported product semantic: Shooting Range Level 2 currently requires Construction measuring tape found in raid, while Security Level 1 uses the same item without FIR requirement.

## Current step

- `64bd4189...`에 추가한 first-open selection / Extract late-load / marker presentation+recovery 회귀 계약을 Windows CI에서 재검증한다.
- 통과 후 PR #225를 release-candidate 상태로 정리하고 v1.11.0 버전/상태 문서를 갱신한다.

## Remaining

- latest HEAD CI green 확인
- PR #225 설명/상태 정리 및 merge readiness 확인
- v1.11.0 version / `PROJECT_STATE` / `CURRENT_STATE` / `STATE` / README / release notes 갱신
- final branch CI
- main merge
- exact-main Windows CI + published EXE smoke 확인
- v1.11.0 tag/release/assets 생성 및 public readback/integrity 확인
- 작업 완료 후 `ACTIVE_WORK`를 NONE으로 닫기
