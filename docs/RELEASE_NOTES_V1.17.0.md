# 준현 헬퍼 v1.17.0

상태: **RELEASE CANDIDATE / NOT YET PUBLIC**  
기준일: **2026-09-03 KST**

v1.17.0은 파밍 가이드의 판단 기준과 전체 배치 방식을 사용자와 확정한 규칙으로 다시 구축하는 MINOR 릴리즈다.

기존 v1.16.x에서 존재하던 음식·음료·탄약·장비 등의 자동 전술 보호, 장비별 별도 우열 기준, 로컬 삽입식 판단은 v1.17.0의 authoritative raid decision에서 제거된다. 사용자가 보호하려는 물품과 칸은 파밍 가이드의 고정 기능으로 직접 지정한다.

## 판단 목표

파밍 가이드는 매 Scanner 입력마다 다음 두 목표만 순서대로 최적화한다.

1. 현재 퀘스트·은신처에 필요한 FIR 수량을 가능한 한 많이 충족한다. 단, 남은 필요 수량까지만 가치가 있다.
2. 1번 결과가 같으면 최종적으로 보유하는 모든 아이템의 평균 플리마켓 가치 합계를 최대화한다.

무게는 아이템 우선순위가 아니라 사용자가 설정한 힘 레벨 기준 최종 운반 가능 여부를 판정하는 제약이다.

## 레이드 획득/FIR 경계

활성 파밍 가이드 레이드에서 Scanner로 새로 확인한 incoming item은 파밍 가이드가 그 레이드에서 획득한 FIR item으로 취급한다.

- Scanner가 FIR 아이콘, 체크, 색상, 문자 등을 판독하지 않는다.
- 사용자에게 별도의 FIR 확인을 요구하지 않는다.
- raid 획득 provenance는 세션 상태인 `RaidAcquired`로만 유지하고 preset/persistence에 저장하지 않는다.
- 레이드 시작 전부터 보유하던 baseline item은 동일 item ID라도 자동으로 FIR 획득분으로 간주하지 않는다.

## 글로벌 최종 상태 최적화

새 아이템을 현재 빈칸에 국소적으로 끼워 넣는 대신, 매 스캔마다 이동 가능한 현재 root들과 incoming item을 대상으로 합법적인 최종 상태를 다시 계산한다.

대상에는 다음이 포함된다.

- 일반 stored item
- top-level equipment
- Rig / Backpack / Secure Container
- container 안 container
- 장착물과 armor plate를 포함한 complete equipment state
- incoming Scanner item

전용 컨테이너는 보관 우선순위를 추가하는 것이 아니라, 해당 아이템을 넣을 수 있는 합법적인 최종 placement 후보로 사용된다.

## Tarkov legality

최종 상태는 다음을 실제 데이터와 상태를 기준으로 검증한다.

- item width/height와 rotation
- grid collision과 storage grid filter
- nested container parent/child 관계와 cycle 금지
- equipment slot compatibility
- attachment / armor plate slot filter
- item conflict와 `ConflictingSlotIds`
- body armor와 armored rig 충돌
- helmet의 headset 차단
- stack quantity
- item/cell/root lock
- 최종 carry weight

`ItemPropertiesHeadwear`도 head equipment slot의 합법적 headwear로 처리한다.

## 고정 제약

사용자가 고정한 item/cell은 가치 점수를 얻는 것이 아니라 최종 상태가 반드시 지켜야 하는 constraint다.

- fixed item은 버리기·교체·좌표 이동·회전·re-parenting을 할 수 없다.
- fixed descendant를 담은 stored ancestor를 움직여 간접 이동시키는 계획도 허용하지 않는다.
- 필요한 경우 해당 ancestor chain과 root Rig / Backpack / Secure Container까지 고정한다.
- fixed carrier 내부의 독립적으로 고정되지 않은 합법적 빈 공간은 계속 사용할 수 있다.

published Product Smoke에서 nested fixed item과 nested fixed cell이 parent chain과 root Secure Container까지 올바르게 고정되는지 검증한다.

## 완전한 가치·무게 계산

최종 보유 가치와 무게 계산은 root item만 보지 않는다.

- modeled attachment와 armor plate의 플리마켓 가치도 최종 retained value에 포함한다.
- attachment/plate 무게도 current/final carry weight에 포함한다.
- candidate pool 밖에서 항상 유지되는 Melee/Dogtag 상태도 최종 무게에 포함한다.
- 탄약·화폐처럼 수량을 입력하는 stack은 하나의 관측된 stack instance로 유지하며 해당 수량이 FIR 충족량, 플리 가치, 무게에 반영된다.
- v1.17.0에서 임의의 자동 split/merge 규칙은 추가하지 않는다.

## 불확실성 처리

파괴적 recommendation을 만들기 위해 필요한 사실을 증명할 수 없으면 보수적으로 실패한다.

- unknown weight를 0 kg으로 가정하지 않는다.
- unknown root geometry를 1x1로 가정하지 않는다.
- Flea 거래 가능한 retained item의 가격을 확인할 수 없으면 0원으로 가정하지 않는다.
- attachment/plate의 필요한 가격/FIR 사실도 기존 Scanner catalog/presentation resolver에서 확인하며 새로운 관찰 소스를 만들지 않는다.
- deterministic search budget 안에서 파괴적 optimum을 증명할 수 없으면 해당 파괴적 지시를 표시하지 않는다.

## 사용자 지시 표시

글로벌 solve가 동일 storage area 안에서 기존 item의 좌표나 rotation을 바꿔야 하는 경우 해당 물리적 변경을 숨기지 않고 `내부 재배치`로 지시에 표시한다. 회전이 필요한 경우 회전 포함 여부도 표시한다.

## 개발 권한 경계

`AGENTS.md`에 내부 구현 최적화와 제품 의미 변경의 경계를 명문화했다.

성능·캐시·자료구조·탐색 전략은 확정된 제품 의미를 보존하는 범위에서 개선할 수 있지만, 개발 편의를 이유로 새로운 판단 기준, 관찰 권한, 자동 추론, 사용자 확인 흐름, cross-feature 자동화 또는 visible failure semantic을 만들 수 없다.

## 회귀 검증

v1.17.0 release candidate gate는 다음을 포함한다.

- deterministic core tests
- Windows Release desktop build
- win-x64 self-contained single-file publish
- ProductVersion / FIRST_RUN identity 검증
- 실제 published EXE Product UI/runtime smoke
- FIR 우선순위와 quota cap
- 음식/음료에 전술적 특권이 없음을 검증
- complete assembly retained value / weight
- unknown price / weight / geometry fail-closed
- incoming container capacity
- equipment replacement/relocation 및 consecutive scan
- 같은 storage area 내부 재배치 instruction
- fixed item/fixed cell ancestry propagation
- global solver의 dedicated nested storage placement
- graceful Main Window shutdown과 process 종료
- clean portable root
- release package 구조와 SHA-256 manifest 검증
- 별도 Shutdown Race CI
- Documentation Consistency gate

## 릴리즈 상태

이 문서는 branch release candidate 단계에서 작성됐다. 실제 public stable source SHA, PR/CI run IDs, exact-main artifact, GitHub release/asset IDs와 SHA-256은 main 병합 및 공개 릴리즈 검증 뒤 canonical release-status 문서와 `PROJECT_STATE`, `CURRENT_STATE`, `STATE`에 기록한다.
