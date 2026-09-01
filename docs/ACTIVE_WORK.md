# ACTIVE WORK

Status: **NONE**

## Current task

None.

## Current stable baseline

```text
public stable: v1.16.0
exact product source/tag target: f1c00b0ac9ea0b70f81991d30be9a04128253d48
merge PR: #273
release id: 380701728
```

## Latest completed work

**v1.16.0 Farming Guide deterministic rulebook / weight / stack quantity / MiniMap hotkey cleanup**

Completed scope:

- Farming Guide 판단을 weighted score가 아닌 **제약 → 중요도 → 상황 대처**의 결정적 rulebook으로 정비.
- 보호 상태를 **잠긴 아이템 + 예약 칸**으로 단순화.
- Found in Raid가 실제 필요한 아이템만 특별 우선순위 적용.
- 경제 가치를 평균 Flea Market 가격으로 통일하고, destructive replacement는 실제 희생되는 전체 물품 가치와 비교.
- 장비별 단순 대표 우월 기준 적용; 총기/권총은 자동 우월 비교하지 않음.
- storage-bearing equipment 교체 시 locked item과 reserved-cell shape/capacity 승계.
- stack quantity state/schema v3, Mini Scanner quantity 입력, Farming Guide quantity 표시/수정 지원.
- Strength 기반 운반 중량 설정/계산 및 final-state weight guard 적용.
- MiniMap bare NumPad 0~5 직접 층 선택 제거, 기존 사용자 지정 위/아래 층 단축키 유지.
- v1.16 UI의 `LayoutUpdated` feedback loop를 제거하여 Dispatcher ContextIdle starvation 및 Map smoke 회귀 수정.

## Validation

```text
validated PR head: bbc8dc25ec35ba24a64df00445b4454bbd7f66d8
PR CI / Shutdown / Docs:
33537853686 / 33537853397 / 33537853539 — SUCCESS

exact-main source:
f1c00b0ac9ea0b70f81991d30be9a04128253d48
exact-main CI / Shutdown / Docs:
33538397901 / 33538397873 / 33538397904 — SUCCESS
Release workflow: 33538760085 — SUCCESS
610 passed / 0 failed / 0 skipped
```

Public v1.16.0 release/tag/assets were verified against the exact product source. Actual user-PC/Tarkov real-play validation remains separately `PENDING` and does not change the public stable identity.
