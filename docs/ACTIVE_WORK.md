# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-30 KST**

## Goal

사용자 실사용에서 확인된 Map/MiniMap/marker 회귀를 수정하고, Scanner correction hotkey 및 ammo/ammo-pack pickup 판단을 추가하며 Hideout FIR requirement/cleanup 정확도를 보강한 v1.11.0을 구현·검증한다.

## Base / branch

```text
base main: fdbf321964035ed4b7eb5c793fc2418031d9f518
public stable: v1.10.1
working branch: feature/v1.11.0-scanner-ammo-map-maintenance-2026-08-30
PR: not opened yet
```

새 사용자 기능이 포함되므로 `docs/VERSIONING.md`에 따라 목표 버전은 **v1.11.0**이다.

## Confirmed scope

### Map / MiniMap maintenance

1. 프로그램 시작 뒤 Main Map을 A→B로 변경하고 MiniMap을 처음 켜도 첫 visible frame부터 현재 B와 동기화될 것. v1.10.0의 기존 fix가 실사용에서 실패한 이유를 root-cause 수준에서 확인한다.
2. 지도 marker 목록에서 extract 관련 checkbox가 일시적으로 사라질 수 있는 코드/데이터/lifecycle 경로가 있는지 조사한다. 증상만으로 결함을 단정하지 않고 재현 가능한 위험이 확인될 때만 수정한다.
3. Player marker size 변경이 MiniMap marker size/name size의 실제 렌더 상태를 초기값으로 되돌리지 않도록 설정값과 rendered state를 일관되게 유지한다.
4. Map marker가 깜박인 뒤 사라지고 MiniMap toggle 또는 marker toggle 후 복구되는 현상의 invalidate/refresh/lifecycle 원인을 조사·수정한다.

### Scanner / Needed Items maintenance

5. Scanner/Mini Scanner 표시에서 flea `최저가` 항목을 제거한다. 신뢰하지 않는 `lastLowPrice` presentation을 더 이상 사용자에게 표시하지 않는다.
6. Hideout requirement의 FIR 여부를 source semantics 그대로 보존한다. Quest=FIR, Hideout=non-FIR 같은 고정 가정을 금지한다. 사용자 제보 예시 `Shooting Range Lv.2 / Measuring tape`를 포함해 current live data 의미를 검증한다.
7. Content update로 requirement가 non-FIR→FIR 등으로 바뀌어 현재 inventory가 더 이상 해당 requirement를 만족하지 않으면 기존 non-FIR 보유분이 cleanup 대상이 될 수 있도록 Needed Items/cleanup derived state를 현재 canonical requirement에 맞춰 재계산한다.

### New features

8. Scanner correction-case capture global hotkey를 추가한다. Raid 중 실패 상황에서 hotkey를 누르면 최신 exact Scanner evidence를 correction data manager의 Saved Case로 저장하고 사용자는 나중에 해당 Case를 열어 Ground Truth를 교정한다. hotkey 사용 자체가 정답을 확정하지 않는다.
9. Scanner에 ammo pickup 판단을 추가한다. caliber별 ammo를 penetration 순으로 놓고 사용자의 현재 trader LL로 직접 **구매 가능한** ammo 구간을 계산한다. 구매 불가 ammo 중 구매 가능한 ammo들의 penetration 범위 내부에 끼어 있는 ammo는 제외하고, 그 범위 바깥의 구매 불가 ammo는 `주워야 함`으로 표시한다. 예: 1..5에서 구매 가능 2,4 → 1,5; 1..7에서 구매 가능 3,5,6 → 1,2,7. Barter/craft/raid-only와 현재 trader LL 미달 direct purchase는 구매 가능으로 보지 않는다.
10. Ammo pack을 canonical contained ammo에 대응해 pack scan에서도 같은 ammo pickup 판단을 사용한다. 한국어 `탄약 팩 (n발)` / 영어 `ammo pack (n pcs)` naming 변형을 포함하되 name-only 추측보다 remote item relationship/contained-ammo fact가 있으면 이를 우선한다. 대응 정보는 Game Content update에서 canonical/local data로 만든다.
11. 다른 사용자로부터 correction data를 네트워크 자동 수집하는 기능은 이번 범위에서 추가하지 않는다.

## Completed

- canonical state recovery 완료
- main `fdbf321...`, public stable v1.10.1, ACTIVE_WORK NONE 및 open PR 없음 확인
- v1.11.0 working branch 생성
- 사용자 요구사항을 본 체크포인트에 기록

## Current step

- 관련 Product/Decision/Architecture/Developer Reference와 실제 Map/MiniMap/Scanner/Needed Items/Game Content/Ammo 구현 및 테스트를 좁혀 조사한다.
- Tarkov external schema/meaning이 관련된 Hideout FIR, trader purchase, ammo-pack relation은 current live-data로 별도 검증한다.

## Remaining

- root cause 조사
- 설계/제품 결정 문서 정렬
- 구현 + deterministic regression tests
- live-data probe
- Windows Release/published EXE UI/runtime smoke
- PR/CI
- main merge + exact-main CI
- v1.11.0 release/tag/assets/public readback
- `PROJECT_STATE`/`CURRENT_STATE`/`STATE`/reference/release docs 갱신
- 작업 완료 후 ACTIVE_WORK NONE
