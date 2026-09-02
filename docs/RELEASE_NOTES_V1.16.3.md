# 준현 헬퍼 v1.16.3

상태: **RELEASE CANDIDATE / NOT YET PUBLIC STABLE**  
기준일: **2026-09-02 KST**

v1.16.3은 v1.16.2 이후 실제 레이드 사용 전에 Farming Guide의 자동 판단 경계를 선제적으로 감사하고, 고가치 loot 보호·희생 판단·잠금·생존 예비량·현재 총기 탄약·확장 주머니와 MiniMap 검증 안정성을 강화하는 PATCH 유지보수 릴리즈다.

공개 stable은 이 문서가 작성된 후보 단계에서는 여전히 v1.16.2이며, v1.16.3은 PR 검증·main 병합·exact-main 검증·공개 릴리즈 검증까지 완료된 뒤에만 stable로 전환한다.

## 보안 컨테이너 보호 우선순위

기존 경로는 LEDX처럼 보안 컨테이너에 넣을 수 있는 고가치 아이템을 스캔했을 때 빈 주머니나 일반 수납 공간을 먼저 발견하면 즉시 그 위치에 보관하도록 끝날 수 있었다. 그러면 보안 컨테이너의 낮은 우선순위 물품을 안전하게 밖으로 옮기고 새 고가치 물품을 보호할 수 있는 상황을 놓쳤다.

v1.16.3에서는 일반 free storage를 확정하기 전에 별도의 비파괴 secure-promotion 경계를 실행한다.

- incoming item이 실제 Tarkov 수납 규칙상 보안 컨테이너에 들어갈 수 있어야 한다.
- 보안 컨테이너의 기존 물품 중 incoming보다 우선순위가 엄격히 낮은 leaf item만 보호 영역 밖으로 내보낼 수 있다.
- 이동되는 기존 물품은 버리지 않고 다른 합법적인 free storage에 보존한다.
- 동급/상위 우선순위 물품, 잠긴 보호 물품, 내부에 다른 물품을 가진 컨테이너는 단순 parent 가격만으로 강등하지 않는다.
- 안전한 승격 계획이 없으면 기존 ordinary storage/destructive rulebook으로 정상적으로 넘어간다.

## 스택 총가치와 실제 희생 조합

탄약·화폐 같은 stored stack은 이제 destructive candidate 단계에서도 실제 `Quantity`를 포함한다. 60발 스택을 1발 가치로 평가해 버리는 식의 오판을 막는다.

또한 과거의 다중 희생 탐색은 가치순으로 정렬한 뒤 `{A}`, `{A+B}`, `{A+B+C}` 같은 prefix 조합만 검사할 수 있었다. 따라서 A가 싸지만 공간 확보에는 무관하고 B 하나만 버리면 되는 경우에도 A까지 잃거나, 유효한 B 단독 해법을 놓칠 수 있었다.

v1.16.3은 bounded deterministic subset search를 사용한다.

- 모든 단일 후보를 독립적으로 검사한다.
- 총 Flea 손실이 적은 조합부터 탐색한다.
- 동일 조건은 희생 개수, footprint, instance ID 순으로 결정적으로 정렬한다.
- 탐색 노드와 최대 희생 수를 제한해 raid-time 비용을 유한하게 유지한다.
- incoming 총 Flea 가치가 실제 전체 희생 가치보다 엄격히 커야 한다는 기존 계약은 유지한다.

## 잠금·주머니·생존 자원 경계

잠금의 의미를 자동 삭제/교체 보호와 위치 고정으로 구분했다.

- 잠긴 가방·리그·보안 컨테이너의 root는 교체하지 않지만 내부 합법 수납 공간은 계속 사용할 수 있다.
- 잠긴 stored item은 동일 `InstanceId`가 살아 있는 안전한 재배치라면 움직일 수 있다.
- reserved cell은 계속 자동 배치 금지 영역으로 보존한다.
- 모든 v1.16.3 전환/repacking 경로는 현재 프로필에서 실제로 계산된 확장 주머니 geometry를 사용한다.

추가로 자동 희생에서 레이드 생존에 필요한 최소 자원을 보호한다.

- 현재 보유 중인 마지막 modeled 음식 provider
- 현재 보유 중인 마지막 modeled 음료 provider
- 현재 휴대한 PrimaryWeapon1 / PrimaryWeapon2 / Holster에 맞는 loose ammunition

이 분류는 이름 추측이 아니라 Tarkov source의 `energy`, `hydration`, ammo/weapon `caliber`, weapon `allowedAmmo`를 사용한다.

## FIR 우선순위 일관성

FIR 특별 우선순위는 incoming뿐 아니라 기존 보유 물품에도 **실제 `CurrentNeededFir`** 만 사용한다.

- FIR이 실제로 필요한 기존 물품은 경제 가치와 별개로 보호한다.
- 일반 필요량만 있고 FIR 요구가 없는 기존 물품은 다른 ordinary economic loot과 동일하게 평균 Flea 가치 기준으로 판단한다.

## 최종 fail-closed 검증

여러 planner를 통과한 추천은 최종 지시 직전에 다시 검증된다.

- explicit equipment/carrier/item locks 보존
- modeled food/drink reserve 보존
- 현재 총기용 loose ammo 수량 보존
- 실제 removed victim 전체의 해석 가능 여부
- stack quantity를 포함한 전체 희생 Flea 가치

안전성을 증명할 수 없는 파괴적 추천은 자동으로 거부하고 현재 상태를 유지하는 `버리기` 결과로 fail closed 한다.

## Content schema v12

새 판단에 필요한 Tarkov source facts를 content snapshot에 보존하기 위해 write schema를 v12로 올렸다.

추가 보존 사실:

- `Energy`
- `Hydration`
- `AmmoCaliber`
- `WeaponCaliber`
- `AllowedAmmoItemIds`

v11을 포함한 기존 readable snapshot은 계속 읽을 수 있지만 current schema가 아니므로 정상 update path에서 v12로 갱신된다.

## MiniMap smoke 안정화

Player Marker Size 제품 코드는 실제로 player marker만 변경하고 있었다. 간헐 실패 원인은 donor marker가 비동기로 재생성되는 순간 smoke가 transient visual instance를 비교하던 데 있었다.

검증은 독립 Player Marker Size setting을 즉시 확인한 뒤 standard-marker rendering이 수렴할 때까지 bounded wait하도록 수정했다. 제품의 Player Marker Size 동작 계약 자체는 변경하지 않았다.

## 회귀 검증

v1.16.3 published WPF smoke에는 실제 page/planner 경계를 실행하는 synthetic raid 시나리오를 추가했다.

- 고가치 secure-eligible loot의 secure promotion before free pocket
- locked carrier root의 내부 storage 사용
- expanded pocket geometry
- stored stack total value
- prefix-only 탐색으로 놓치던 실제 geometric victim 단독 선택
- 마지막 food/drink reserve 보호
- 현재 총기 compatible loose ammo 보호
- locked exact instance의 안전한 이동 허용
- non-FIR general need와 true FIR need의 우선순위 구분

## 현재 후보 검증 증거

릴리즈 식별자 v1.16.3으로 올리기 직전의 동일 제품 로직/새 published decision smoke head:

```text
head: 1e20dd97338ad56048071766a85539c29fe8f4ba
PR: #281 (draft candidate)
CI: 33616912770 — SUCCESS
Shutdown Race: 33616912788 — SUCCESS
Documentation Consistency: 33616912777 — SUCCESS
623 passed / 0 failed / 0 skipped
Windows x64 self-contained publish — SUCCESS
published EXE Product UI / Map / Farming Guide decision smoke — SUCCESS
graceful shutdown / clean portable root — SUCCESS
package/checksum verification — SUCCESS
```

위 pre-identity candidate release package는 검증용이며 공개 릴리즈 asset이 아니다. v1.16.3 최종 PR 후보, exact-main, 공개 release의 immutable evidence는 실제 릴리즈 완료 후 이 문서와 canonical project state에 기록한다.
