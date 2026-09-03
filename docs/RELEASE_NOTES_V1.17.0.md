# 준현 헬퍼 v1.17.0

상태: **PUBLIC STABLE**  
기준일: **2026-09-03 KST**

v1.17.0은 파밍 가이드의 판단 기준과 전체 배치 방식을 사용자와 확정한 규칙으로 다시 구축한 MINOR 릴리즈다.

기존 v1.16.x에서 존재하던 음식·음료·탄약·장비 등의 자동 전술 보호, 장비별 별도 우열 기준, 로컬 삽입식 판단은 v1.17.0의 authoritative raid decision에서 제거됐다. 사용자가 보호하려는 물품과 칸은 기존 고정 기능으로 직접 지정한다.

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

대상에는 일반 stored item, top-level equipment, Rig / Backpack / Secure Container, container 안 container, complete equipment state와 incoming item이 포함된다. 전용 컨테이너는 보관 우선순위가 아니라 합법적 placement 후보다.

## Tarkov legality / fail closed

최종 상태는 실제 데이터와 상태를 기준으로 item geometry/rotation/collision, storage grid/filter, nested ownership/cycle, equipment slot, attachment/plate filters, item conflict와 `ConflictingSlotIds`, body armor/armored rig, helmet/headset, stack quantity, fixed state와 final carry weight를 검증한다.

부착물과 armor plate도 retained Flea value와 weight에 포함한다. 필요한 가격·무게·크기·호환 사실을 증명할 수 없으면 0/1x1 등의 임의 기본값으로 파괴적 recommendation을 만들지 않는다.

## 고정 제약

고정 item/cell은 value가 아니라 hard constraint다. fixed descendant를 담은 ancestor나 root carrier를 이동해 간접적으로 고정 위치를 바꾸는 계획도 허용하지 않는다. fixed carrier 내부의 독립적으로 고정되지 않은 합법적 free storage는 계속 사용할 수 있다.

같은 storage area 안에서 global solve가 이동/회전을 요구하면 `내부 재배치`로 표시한다.

## 수량 / 무게

Ammo와 Currency의 사용자 입력 수량은 하나의 실제 관측 stack instance 수량으로 취급하고 FIR 충족량, Flea value와 weight에 반영한다. 자동 split/merge 규칙은 추가하지 않았다.

## 공개 릴리즈 검증

```text
exact product source/tag target:
8b0e1f8f46fa3822f4cff05b7be3223d40ad7435
merge PR: #288
validated PR head: a01d61cd9957db94a7475734c1e8df66ce71f53d
PR CI / Shutdown / Docs:
33746966753 / 33746966804 / 33746966771 — SUCCESS
exact-main CI / Shutdown / Docs:
33748900315 / 33748900348 / 33748900377 — SUCCESS
Release workflow: 33749193376 — SUCCESS
649 passed / 0 failed / 0 skipped
release id: 381959220
published UTC: 2026-09-03T11:21:35Z
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
id: 9890816795
bytes: 242,234,759
SHA-256: d9115f24968804fc5b4e65fa7bbaaf008f4af516e044f3b00e0ee6b4525a15dd
```

Public assets:

```text
Junhyun-Helper.zip
id: 542663027
bytes: 80,766,362
SHA-256: 6ecc3a61d0b492f6b475e18f309e55790776911e5496fc704d12ffd611c629cb

SHA256SUMS.txt
id: 542663026
bytes: 86
asset SHA-256: 7a2fb4f7ebcb333eafd8cad6f9acbf532549118e608776786666014a24875bdf
```

Release workflow는 exact-main commit을 checkout하고 해당 CI artifact를 다시 내려받은 뒤, ProductVersion/FIRST_RUN identity와 실제 ZIP SHA-256을 `SHA256SUMS.txt`에 대조하고 stable `v1.17.0`을 공개했다. 공개 release는 `draft=false`, `prerelease=false`이며 tag target은 exact product source와 일치한다.
