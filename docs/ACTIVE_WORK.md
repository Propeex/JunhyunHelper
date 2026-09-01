# ACTIVE WORK

Status: **ACTIVE**

## Current task

**v1.16.0 Farming Guide deterministic rulebook / weight / stack quantity / minimap hotkey cleanup**

## Base / branch

```text
base main: 555c0f841e06c170dd356ea88a11bdcafd6a479b
public stable: v1.15.5
working branch: feature/v1.16.0-farming-guide-rulebook-weight-stack-2026-09-02
PR: not created yet
```

## Confirmed scope

- Farming Guide 판단을 weighted/scored optimization이 아니라 **제약 → 중요도 → 상황 대처**의 deterministic manual/rulebook로 정비한다.
- 보호 대상은 **잠긴 아이템 + 예약 칸**만이다. 음식/음료/필수 탄약/필수 탄창을 별도 보호 클래스로 만들지 않는다.
- Found in Raid가 필요한 아이템만 loot priority 최상위로 취급한다. 비-FIR 필요 아이템은 일반 경제 loot과 동일하게 취급한다.
- 경제 가치는 **평균 Flea Market 가격**을 기준으로 한다. 공간 부족 시 incoming item과 실제로 희생해야 하는 전체 물품의 결과를 비교한다.
- 장비 자동 우월 판단은 단순 대표 기준만 사용한다: 방탄복/헬멧=방탄 등급, 헤드셋=청취 성능, 일반 리그/가방/보안 컨테이너=수납 능력, 방탄 리그=방탄 등급 우선 후 동급이면 수납 능력. 총기/권총은 자동 우월 비교하지 않는다. 내구도/실제 총기 조립 상태 등 Scanner가 알 수 없는 정보는 판단하지 않는다.
- 수납 장비 교체는 좌표가 아니라 **잠긴 item role + reserved-cell shape/capacity**를 새 장비에 승계해야 하며 승계 불가능하면 교체 금지한다.
- Farming Guide 우측 하단 정보 영역에 무게 기능을 추가한다. 필요한 캐릭터 skill level을 내부 popup에서 입력하고 바깥 클릭으로 닫는다. 별도 저장 버튼은 두지 않는다.
- ammo/currency처럼 stack quantity가 판단에 필요한 scan은 Mini Scanner Farming Guide 지시 대신 quantity input을 먼저 표시하고 Enter 후 동일 recommendation path를 실행한다.
- Farming Guide 탭에서도 ammo/currency item double-click으로 quantity를 수정할 수 있고 item에 quantity를 표시한다.
- quantity는 state/preset/raid transition에서 보존되고 가치·무게·needed count 계산에 반영한다.
- Mini Scanner quantity 입력 대기 중에는 기존 accept 동작을 실행하지 않으며 새 scan은 이전 quantity pending을 취소한다.
- Minimap numeric keypad direct-floor hotkeys를 제거하고 hotkey 설정/충돌 점유에서도 해제한다. 기존 위/아래 층 이동 단축키는 유지한다.

## Completed

- v1.15.5 stable recovery 완료.
- 제품 로직/예외 분석 및 사용자 확정 완료.
- v1.16.0 MINOR 범위 확정.
- 작업 branch 생성 및 checkpoint 시작.

## Current step

관련 Core state/priority/equipment policy, Desktop Farming Guide/Mini Scanner UI, persisted state, minimap hotkey 구현과 테스트 위치를 조사한 뒤 변경한다.

## Remaining

1. 관련 code/test/data contracts 조사
2. deterministic rulebook + quantity/weight state 구현
3. equipment lock/reservation inheritance 구현
4. Mini Scanner quantity interaction + Farming Guide quantity editing/weight popup 구현
5. minimap numpad floor hotkey 제거
6. schema/version/documentation 정합성 반영
7. deterministic tests / Windows Release build / publish-smoke / shutdown 검증
8. PR / CI / main merge / exact-main 검증
9. v1.16.0 release/tag/assets 검증 및 ACTIVE_WORK close
