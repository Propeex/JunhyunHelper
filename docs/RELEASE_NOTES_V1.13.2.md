# 준현 헬퍼 v1.13.2

Date: **2026-08-31 KST**  
Status: **RELEASE CANDIDATE**

v1.13.2는 v1.13.1 Farming Guide의 실사용 장착·수납·프리셋·내부 정보 UX를 보완하는 PATCH 릴리즈다. 기존 Loadout / Inventory Editor의 제품 의미를 유지하면서 사용자가 보고한 실제 사용 문제와 누락된 interaction을 수정한다.

## Farming Guide

- 권총/리볼버/handgun 계열을 전용 Holster 슬롯에 장착하고 Primary Weapon 1/2에서는 제외한다.
- 방탄복, 리그, 가방, 보안 컨테이너 장착 판정을 current Tarkov `propertiesType`뿐 아니라 canonical type/category 의미로 보강한다.
- 활성 프로필의 edition과 Old Patterns 완료 상태를 사용해 주머니 geometry를 결정한다.
  - 일반: `1×1 / 1×1 / 1×1 / 1×1`
  - 확장: `1×1 / 1×2 / 1×2 / 1×1`
- resolved pocket geometry는 화면 표시뿐 아니라 placement와 persisted-state sanitization에도 동일하게 사용한다.
- 수납 영역을 `Rig → Pockets + Special Slots → Backpack → Secure Container` 순서로 정리한다.
- Pockets는 왼쪽, Special Slots는 오른쪽에 같은 행으로 표시한다.
- 장착 장비를 더블클릭하면 current item structure를 연다.
  - 총기/부착 가능 장비: attachment slots
  - 헬멧/방탄 장비: armor plate 및 기타 장착 slots
  - 리그/가방/보안 컨테이너: 실제 storage grid 구조
- 장착 전 검색 결과도 더블클릭하면 같은 내부 구조를 read-only로 확인할 수 있다.
- 선택한 프리셋을 삭제하는 휴지통 버튼을 추가한다. 프리셋을 삭제해도 현재 working loadout은 버리지 않는다.
- 프리셋 이름 입력 창은 content-sized height를 사용해 DPI/theme에 따른 하단 clipping을 방지한다.
- 근접무기와 PMC 인식표의 fixed-setting lifecycle은 그대로 유지하고 화면의 `고정` 문구만 제거한다.

## Compatibility / persistence

- `farming-guide.json` schema는 **v1 유지**.
- Game Content write schema는 **v9 유지**.
- mandatory user-data migration 없음.
- 과거 preset은 current item/grid/filter와 현재 profile의 pocket geometry를 authority로 fail-closed sanitize한다.
- filled carrier destructive replacement, item dimension, rotation, bounds/overlap/filter 검증 등 기존 v1.13.0/v1.13.1 안전 계약을 유지한다.

## Regression coverage

구현 검증 HEAD `ea73fff8f97eddf6e4411d6b4a85482b59c08344`는 다음을 통과했다.

- **504 passed / 0 failed / 0 skipped** deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- Shutdown Race CI
- Documentation Consistency
- release package/checksum audit

v1.13.2 버전/배포 메타데이터까지 포함한 최종 release-candidate HEAD는 병합 전에 같은 exact-head gate를 다시 통과해야 한다.

## Preserved non-goals

v1.13.2에도 다음은 추가하지 않는다.

- loot 가치 판단
- pickup / discard / replace 추천
- Scanner 실시간 recommendation
- 실제 raid inventory 좌표의 지속적인 1:1 동기화
