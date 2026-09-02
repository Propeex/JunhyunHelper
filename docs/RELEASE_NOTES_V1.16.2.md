# 준현 헬퍼 v1.16.2

상태: **PUBLIC STABLE / VERIFIED**  
기준일: **2026-09-02 KST**

v1.16.2는 v1.16.1에서 확인된 두 가지 Farming Guide 실사용 회귀를 수정하고, Farming Guide의 상태·판단·렌더링·저장·published runtime 경계를 다시 검증한 PATCH 유지보수 릴리즈다.

## 파밍한 가치 표시 복구

기존 파밍 가이드 하단의 파밍 가치 영역은 실제 계산 결과에 연결되지 않고 `—`를 고정 표시하고 있었다.

v1.16.2에서는 활성 레이드의 시작 snapshot을 기준으로 현재 snapshot에서 실제로 순증가해 **현재 보유 중인** 아이템만 가치에 포함한다.

- 레이드 시작 시 이미 보유하던 아이템은 포함하지 않는다.
- 현재까지 순수하게 획득해 보유 중인 수량만 포함한다.
- 탄약·화폐 등 stack quantity를 실제 수량대로 반영한다.
- 획득 후 다시 버린 아이템은 현재 snapshot에서 빠진 만큼 가치에서도 제거한다.
- 시작 아이템을 잃어도 파밍 가치를 음수로 만들지 않는다.
- 경제 기준은 기존 Farming Guide 계약과 동일하게 평균 Flea Market 가격만 사용한다.
- 중첩 컨테이너와 complete-equipment snapshot의 재귀 inventory count도 동일한 기준으로 처리한다.
- 가격을 확인할 수 없는 항목은 추측하지 않고 0으로 취급한다.

이를 위해 raid-start baseline과 현재 snapshot 사이의 획득량을 계산하는 전용 가치 정책을 추가하고, 스캔 중 확보한 Flea 가격과 Scanner snapshot resolver를 이용해 현재 retained loot의 값을 표시하도록 연결했다.

## 예약/고정 빈칸 수동 배치 표시 수정

자동 파밍 로직에서 사용하지 않도록 고정한 빈칸은 reservation overlay로 표시된다. 기존 구현에서는 이 overlay가 아이템 카드보다 높은 Z-index에 있어, 사용자가 해당 칸에 직접 아이템을 놓으면 상태에는 정상 저장되지만 화면에서는 아이템이 가려졌다.

v1.16.2에서는 reservation marker를 아이템 카드보다 뒤쪽 렌더링 계층에 둔다.

- 예약 칸은 계속 자동 배치 대상으로 사용되지 않는다.
- 사용자가 직접 드래그해서 넣는 동작은 기존처럼 허용된다.
- 직접 넣은 아이템은 reservation marker보다 위에서 정상 표시된다.
- 잠긴 실제 아이템의 accent-border 계약은 바꾸지 않는다.

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

확인된 두 회귀 외에 기존 deterministic rulebook을 바꿔야 할 추가 결함은 재현되지 않았다. 근거 없는 규칙 변경이나 구조 변경은 하지 않았다.

## 추가 회귀 검증

v1.16.2에는 다음 검증을 추가했다.

- baseline item exclusion
- stack quantity value contribution
- lost baseline item non-negative behavior
- acquired-then-discarded loot removal
- nested inventory counting
- unknown/non-positive Flea price behavior
- value-summary source contract
- reserved-overlay layering source contract
- published WPF smoke에서 실제 파밍 가치 렌더링 확인
- published WPF smoke에서 reservation marker가 item card 아래에 있는지 Z-index 직접 확인

## 공개 릴리즈 검증

```text
exact product source/tag target:
81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
validated PR head:
119b47c406058ed422afdb17bace54db0f7e68f5
merge PR: #279

PR CI / Shutdown / Docs:
33601684251 / 33601684206 / 33601684210 — SUCCESS

exact-main CI / Shutdown / Docs:
33602013494 / 33602013351 / 33602013617 — SUCCESS

Release workflow:
33602299729 — SUCCESS

619 passed / 0 failed / 0 skipped
release id: 381041582
published UTC: 2026-09-02T07:11:21Z
```

Exact-main CI는 Release build, 619개 결정적 테스트, Windows x64 self-contained publish, 실제 published EXE의 Product UI / Map / Farming Guide / graceful-shutdown smoke, 패키지 및 checksum 검증을 통과했다.

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9835631036
bytes: 242,089,986
SHA-256:
efcfb965a2a64cb7f7e3916ae3ed1c96d8eba5c0f77e1cd6090d41f6f9a5564c
```

Public release assets:

```text
Junhyun-Helper.zip
asset id: 540776589
bytes: 80,718,992
SHA-256:
8396a7810ac95a7118f88f68914038332e9876cdfd7b59247d32c4d44c22c7a7

SHA256SUMS.txt
asset id: 540776588
bytes: 86
asset SHA-256:
0fb2eb4894acc0e37b0f3c72633b1d5d37ef8a134ece1829158414c3652da805
```

Release workflow는 exact-main commit `81ce1dc93fefd633502e62cb5fdde54c2f61ce8c`를 직접 checkout하고 exact-main artifact ID `9835631036`을 기대 digest와 함께 다시 내려받았다. `Junhyun-Helper.zip`의 실제 SHA-256 `8396a7810ac95a7118f88f68914038332e9876cdfd7b59247d32c4d44c22c7a7`가 `SHA256SUMS.txt`와 일치한 뒤에만 stable release를 공개했다.

`v1.16.2`는 `draft=false`, `prerelease=false`이며 공개 릴리즈 target은 위 exact product source다. 이후 문서-only main commit은 v1.16.2 제품 소스가 아니며 이미 공개된 stable assets를 교체하지 않는다.
