# 준현 헬퍼 v1.16.2

상태: **RELEASE CANDIDATE**  
기준일: **2026-09-02 KST**

v1.16.2는 공개 안정판 v1.16.1에서 확인된 두 가지 Farming Guide 실사용 회귀를 수정하고, 해당 기능의 상태·판단·렌더링 경계를 다시 검증하는 PATCH 유지보수 릴리즈다.

## 파밍한 가치 표시 복구

파밍 가이드 하단의 파밍 가치 영역은 기존 UI 코드에서 실제 계산 결과와 연결되지 않은 채 `—`를 고정 표시하고 있었다.

v1.16.2에서는 활성 레이드의 시작 snapshot을 기준으로 현재 snapshot에서 실제로 순증가한 아이템만 계산한다.

- 레이드 시작 시 이미 보유하던 아이템은 포함하지 않는다.
- 현재까지 순수하게 획득해 보유 중인 수량만 포함한다.
- 탄약·화폐 등 stack quantity를 실제 수량대로 반영한다.
- 획득 후 다시 버린 아이템은 현재 snapshot에서 빠진 만큼 가치에서도 제거한다.
- 시작 아이템을 잃어도 파밍 가치를 음수로 만들지 않는다.
- 경제 기준은 기존 Farming Guide 계약과 동일하게 평균 Flea Market 가격만 사용한다.
- 중첩 컨테이너와 complete-equipment snapshot의 재귀 inventory count도 동일한 기준으로 처리한다.
- 가격을 확인할 수 없는 항목은 추측 가격을 만들지 않고 0으로 취급한다.

이를 위해 raid-start baseline과 현재 snapshot 사이의 획득량을 계산하는 전용 가치 정책을 추가하고, 스캔 중 확보한 Flea 가격과 Scanner snapshot resolver를 이용해 현재 retained loot의 값을 계산하도록 UI를 연결했다.

## 예약/고정 빈칸 수동 배치 표시 수정

자동 파밍 로직에서 사용하지 않도록 고정한 빈칸은 반투명 reservation overlay로 표시된다. 기존 구현에서는 이 overlay가 Z-index 50으로 아이템 카드보다 위에 배치되어, 사용자가 해당 칸에 직접 아이템을 놓으면 상태에는 정상 저장되지만 화면에서는 아이템이 overlay 아래에 가려지는 문제가 있었다.

v1.16.2에서는 reservation marker를 아이템 카드보다 뒤쪽 렌더링 계층에 둔다.

- 예약 칸은 계속 자동 배치 대상으로 사용되지 않는다.
- 사용자가 직접 드래그해서 넣는 동작은 기존처럼 허용된다.
- 직접 넣은 아이템은 항상 reservation marker보다 위에서 정상 표시된다.
- 잠긴 실제 아이템의 accent border 계약은 바꾸지 않는다.

## Farming Guide 전체 점검

이번 수정과 함께 다음 경계를 집중적으로 재검토했다.

- raid baseline/current snapshot 및 instruction acceptance
- FIR 필요 우선순위와 일반 경제 loot 구분
- 평균 Flea Market 총가치 기반 비교
- 동일 가치의 무게/footprint tie-break
- 여러 희생 아이템이 필요한 destructive replacement의 전체 희생 가치
- 방탄복·헬멧·헤드셋·리그·가방·보안 컨테이너 대표 우위 기준
- 가격 때문에 장비를 자동 교체하지 않는 계약
- 잠긴 아이템과 reserved cells 보호
- carrier 교체 시 보호된 contents/reservation migration
- nested storage와 specialized container filters
- ammo/currency quantity 입력·표시·수정 및 가치/무게 반영
- Strength 기반 최대 운반 중량과 overweight recommendation 차단
- Farming Guide persistence normalization/recovery
- Scanner bridge와 Mini Scanner instruction/quantity lifecycle
- rendered WPF Farming Guide surface 및 published EXE smoke

현재 확인된 두 회귀 외에 기존 deterministic rulebook을 변경해야 할 추가 결함은 재현되지 않았으며, 재현 근거 없는 규칙 변경이나 구조 변경은 하지 않았다.

## 추가 회귀 검증

v1.16.2에는 다음 검증을 추가했다.

- baseline item exclusion
- stack quantity value contribution
- lost baseline item non-negative behavior
- acquired-then-discarded loot removal
- nested inventory counting
- unknown/non-positive Flea price behavior
- value summary source contract
- reserved overlay layering source contract
- published WPF smoke에서 실제 파밍 가치 렌더링 확인
- published WPF smoke에서 reservation marker가 item card 아래에 있는지 Z-index 직접 확인

최종 PR / exact-main / 공개 릴리즈 검증 번호와 공개 asset digest는 릴리즈 완료 후 canonical project state와 이 문서에 기록한다.
