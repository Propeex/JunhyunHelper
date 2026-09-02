# 준현 헬퍼 v1.16.3

상태: **PUBLIC STABLE / VERIFIED**  
기준일: **2026-09-02 KST**

v1.16.3은 v1.16.2 이후 실제 레이드 사용 전에 Farming Guide의 자동 판단 경계를 선제적으로 감사하고, 고가치 loot 보호·희생 판단·잠금·생존 예비량·현재 총기 탄약·확장 주머니와 MiniMap 검증 안정성을 강화한 PATCH 유지보수 릴리즈다.

## 보안 컨테이너 보호 우선순위

기존 경로는 LEDX처럼 보안 컨테이너에 넣을 수 있는 고가치 아이템을 스캔했을 때 빈 주머니나 일반 수납 공간을 먼저 발견하면 즉시 그 위치에 보관하도록 끝날 수 있었다. 그러면 보안 컨테이너의 낮은 우선순위 물품을 안전하게 밖으로 옮기고 새 고가치 물품을 보호할 수 있는 상황을 놓쳤다.

v1.16.3에서는 일반 free storage를 확정하기 전에 별도의 비파괴 secure-promotion 경계를 실행한다.

- incoming item이 실제 Tarkov 수납 규칙상 보안 컨테이너에 들어갈 수 있어야 한다.
- 보안 컨테이너의 기존 물품 중 incoming보다 우선순위가 엄격히 낮은 안전한 leaf item만 보호 영역 밖으로 내보낼 수 있다.
- 이동되는 기존 물품은 버리지 않고 다른 합법적인 free storage에 보존한다.
- 동급/상위 우선순위 물품, 잠긴 보호 물품, 내부에 다른 물품을 가진 컨테이너는 단순 parent 가격만으로 강등하지 않는다.
- 안전한 승격 계획이 없으면 기존 ordinary storage/destructive rulebook으로 정상적으로 넘어간다.

## 스택 총가치와 실제 희생 조합

탄약·화폐 같은 stored stack은 destructive candidate 단계에서도 실제 `Quantity`를 포함한다. 60발 스택을 1발 가치로 평가해 버리는 식의 오판을 막는다.

과거의 다중 희생 탐색은 가치순으로 정렬한 뒤 prefix 조합만 검사할 수 있었다. 따라서 공간 확보와 무관한 값싼 물품까지 함께 희생하거나, 실제로 더 나은 단독/부분 조합을 놓칠 수 있었다.

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

자동 희생에서는 레이드 생존에 필요한 최소 자원을 보호한다.

- 현재 보유 중인 마지막 modeled 음식 provider
- 현재 보유 중인 마지막 modeled 음료 provider
- 현재 휴대한 PrimaryWeapon1 / PrimaryWeapon2 / Holster에 맞는 loose ammunition

이 분류는 이름 추측이 아니라 Tarkov source의 `energy`, `hydration`, ammo/weapon `caliber`, weapon `allowedAmmo`를 사용한다.

## FIR 우선순위 일관성

FIR 특별 우선순위는 incoming뿐 아니라 기존 보유 물품에도 **실제 `CurrentNeededFir`** 만 사용한다.

- FIR이 실제로 필요한 기존 물품은 경제 가치와 별개로 보호한다.
- 일반 필요량만 있고 FIR 요구가 없는 기존 물품은 다른 ordinary economic loot과 동일하게 평균 Flea 가치 기준으로 판단한다.

## 최종 fail-closed 검증

여러 planner를 통과한 파괴적 추천은 최종 지시 직전에 다시 검증된다.

- explicit equipment/carrier/item locks 보존
- modeled food/drink reserve 보존
- 현재 총기용 loose ammo 수량 보존
- 실제 removed victim 전체의 해석 가능 여부
- stack quantity를 포함한 전체 희생 Flea 가치
- modeled carry-weight constraint

안전성을 증명할 수 없는 파괴적 추천은 자동으로 거부한다.

## Content schema v12

새 판단에 필요한 Tarkov source facts를 content snapshot에 보존하기 위해 write schema를 v12로 올렸다.

추가 보존 사실:

- `Energy`
- `Hydration`
- `AmmoCaliber`
- `WeaponCaliber`
- `AllowedAmmoItemIds`

v3-v12 snapshot을 읽을 수 있으며 구버전 cache는 정상적으로 읽은 뒤 current update path에서 v12로 갱신된다.

## MiniMap smoke 안정화

Player Marker Size 제품 코드는 실제로 player marker만 변경하고 있었다. 간헐 실패 원인은 donor marker가 비동기로 재생성되는 순간 smoke가 transient visual instance를 비교하던 데 있었다.

검증은 독립 Player Marker Size setting을 즉시 확인한 뒤 standard-marker rendering이 수렴할 때까지 bounded wait하도록 수정했다. 제품의 Player Marker Size 동작 계약 자체는 변경하지 않았다.

## 회귀 검증

v1.16.3 published WPF smoke에는 실제 page/planner 경계를 실행하는 synthetic raid 시나리오를 추가했다.

- 고가치 secure-eligible loot의 secure promotion before free pocket
- locked carrier root의 내부 storage 사용
- expanded pocket geometry
- stored stack total value
- prefix-only 탐색으로 놓치던 실제 geometric victim 선택
- 마지막 food/drink reserve 보호
- 현재 총기 compatible loose ammo 보호
- locked exact instance의 안전한 이동 허용
- non-FIR general need와 true FIR need의 우선순위 구분

## 공개 릴리즈 검증

```text
exact product source/tag target:
89fae2e07b721b1dfd4922642412fcebf01b275d
validated PR head:
1c223a696e896e1af2ec1c35ec727eb3c70aa44d
merge PR: #282

PR CI / Shutdown / Docs:
33618363995 / 33618364028 / 33618363996 — SUCCESS

exact-main CI / Shutdown / Docs:
33618724736 / 33618724737 / 33618725069 — SUCCESS

Release workflow:
33619033186 — SUCCESS

623 passed / 0 failed / 0 skipped
release id: 381157194
published UTC: 2026-09-02T10:21:57Z
```

Exact-main CI는 Release build, 623개 결정적 테스트, Windows x64 self-contained publish, 실제 published EXE의 Product UI / Map / Farming Guide decision / graceful-shutdown smoke, portable-root 검사와 패키지/checksum 검증을 통과했다.

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9842117423
bytes: 242,138,760
SHA-256:
cda8d29a6dfa3499df8ba23522ed7faeb11475e726c6b8ed66566bb29eda55eb
```

Public release assets:

```text
Junhyun-Helper.zip
asset id: 541000063
bytes: 80,735,580
SHA-256:
eabc7c162ea583f138fbeb3bd2567145bc28c6f305bde20e049175c56580f657

SHA256SUMS.txt
asset id: 541000067
bytes: 86
asset SHA-256:
c25ad9cb116c53143f1aece1a5035313d0a1176acff5b71c6366ea297d69dae5
```

Release workflow는 exact-main commit `89fae2e07b721b1dfd4922642412fcebf01b275d`를 직접 checkout하고 exact-main artifact ID `9842117423`을 기대 digest `cda8d29a6dfa3499df8ba23522ed7faeb11475e726c6b8ed66566bb29eda55eb`와 함께 다시 내려받았다. 실제 `Junhyun-Helper.zip`의 SHA-256 `eabc7c162ea583f138fbeb3bd2567145bc28c6f305bde20e049175c56580f657`가 `SHA256SUMS.txt`와 일치한 뒤 stable release를 공개했다.

`v1.16.3`은 `draft=false`, `prerelease=false`이며 공개 릴리즈 target은 위 exact product source다. 이후 문서-only main commit은 v1.16.3 제품 소스가 아니며 이미 공개된 stable assets를 교체하지 않는다.
