# 준현 헬퍼 v1.16.0

상태: **RELEASE CANDIDATE / VALIDATION IN PROGRESS**  
기준일: **2026-09-02 KST**

v1.16.0은 Farming Guide의 핵심 판단을 가중치·암묵적 최적화가 아닌 **제약 → 중요도 → 상황 대처**의 명시적 규칙으로 정비하고, 스택 수량과 Strength 기반 무게 제약을 실제 raid state에 포함하는 MINOR 릴리즈다.

## 결정적 파밍 중요도

- 특별 필요 우선순위는 남은 **Found in Raid 필요량**이 있는 아이템에만 적용한다.
- 비-FIR 필요품은 돈으로 대체 구매할 수 있으므로 일반 경제 loot과 동일하게 판단한다.
- 경제 가치는 **평균 Flea Market 가격**만 사용한다. Trader 판매가는 Farming Guide 경제 비교에 개입하지 않는다.
- 일반 loot은 총 Flea 가치로 비교하고, 동가치이며 양쪽 무게를 모두 아는 경우 더 가벼운 물품을 우선한다. 마지막 동률은 더 작은 footprint를 우선한다.
- 공간 부족 시 전역적인 ₽/slot 점수를 사용하지 않고 incoming item과 실제로 버려야 하는 **전체 victim set의 합산 Flea 가치**를 비교한다.
- FIR 필요 item은 자동 희생하지 않는다.

## 장비 자동 교체 규칙

장비는 가격만 높다는 이유로 자동 교체하지 않는다.

- 방탄복·헬멧: source-backed ArmorClass
- 헤드셋: source-backed 청취거리
- 일반 리그·가방·보안 컨테이너: source-backed storage capacity
- 방탄 리그: ArmorClass 우선, 동급에서만 storage capacity
- 총기·권총: Scanner가 실제 조립·성능 상태를 알 수 없으므로 자동 우월 비교 없음

빈 호환 장비칸은 정상 장착 후보가 되지만, 이미 차 있는 칸은 위의 명시적 우월 기준을 통과할 때만 자동 교체한다.

## 잠금·예약 역할 승계

수납 장비를 교체할 때 보호 상태는 기존 좌표가 아니라 의미를 보존한다.

- 잠긴 item instance는 제거하지 않으며 새 장비 내부의 합법적인 위치로 이동할 수 있다.
- 예약 칸은 grid 안에서 연결된 shape/capacity로 취급하고 새 장비에서 동등한 shape를 다시 확보한다.
- 기존 내용물, 잠긴 item, 예약 shape를 모두 만족하는 합법적 배치를 만들 수 없으면 해당 장비 교체는 금지한다.
- 성공한 장비 교체의 snapshot과 이동된 lock/reservation state는 한 번의 사용자 수락으로 원자적으로 commit한다.

## 탄약·화폐 수량

- Scanner가 Item ID만으로 실제 개수를 알 수 없는 탄약과 canonical 화폐는 Mini Scanner에서 숫자 입력을 먼저 요구한다.
- Enter 제출 후 같은 Farming Guide planner가 수량을 포함해 판단한다.
- quantity 입력 대기 중 accept hotkey는 상태를 반영하지 않으며 새 scan은 이전 quantity pending을 폐기한다.
- Farming Guide의 스택 item은 개수를 표시하며 더블클릭하여 개수를 수정할 수 있다.
- quantity는 snapshot count, FIR 필요량, stack Flea 가치, 무게 계산에 반영한다.
- Farming Guide persisted state schema는 v3이며 v1/v2의 수량 없는 기존 item은 1개로 호환 로딩한다.

## Strength 기반 무게 제약

현재 확인한 Tarkov 규칙을 사용한다.

- 기본 최대 운반 중량: 77 kg
- Strength level당 최대 운반 중량: +0.6%
- Strength Elite: 약 100 kg
- Elite에서는 sling/back/holster에 해당하는 주무기 1·2와 권총 슬롯 무게를 제외한다. 근접무기는 제외하지 않는다.
- Scanner가 알 수 없는 stimulant 등의 일시 효과는 추측하지 않는다.

Farming Guide 우측 하단 무게 표시를 클릭하면 profile별 Strength level을 입력할 수 있다. recommendation은 **최종 ProposedSnapshot**의 무게를 검사하며, 현재 사용자가 반영한 상태가 이미 한계 초과라면 무게를 더 늘리는 전환을 허용하지 않는다.

## MiniMap NumPad 정리

- bare NumPad 0~5 직접 층 선택 기능을 제거했다.
- 사용자가 설정하는 기존 위/아래 층 이동 단축키는 유지한다.
- transplanted Map/MiniMap이 기대하는 keyboard-hook lifecycle과 source-compatible endpoint는 유지하되 direct-floor event는 더 이상 dispatch하지 않는다.

## 검증 목표

릴리즈 전 다음을 모두 통과해야 한다.

```text
Release build
610 deterministic tests / 0 failed / 0 skipped
self-contained win-x64 publish
published EXE Product UI + Map/MiniMap + Farming Guide runtime smoke
graceful shutdown / Shutdown Race
package/checksum verification
PR CI / exact-main CI / Documentation Consistency
```

Canonical decision:

- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`

최종 exact product source, CI run, release/tag/asset 정보는 공개 릴리즈 검증 후 `docs/PROJECT_STATE.json`, `docs/STATE.md`, `docs/CURRENT_STATE.md`에 기록한다.
