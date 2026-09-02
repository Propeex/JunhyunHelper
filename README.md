# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.16.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.16.4
exact product source/tag target:
5886d8f97abd060d398d4c50d3dd3b720e4ace09
validated PR head: d55e138c962e87dc8691f82c81d36a516db52941
merge PR: #285 — MERGED
PR CI / Shutdown / Docs:
33623459284 / 33623459290 / 33623459267 — SUCCESS
exact-main CI / Shutdown / Docs:
33623824030 / 33623824052 / 33623824027 — SUCCESS
Release workflow: 33624248788 — SUCCESS
release id: 381192920
published UTC: 2026-09-02T11:22:47Z
623 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 541072599
bytes: 80,738,891
SHA-256:
2ceddbd3cc805bc8de2cdb5eddcef72c2001a6724a43ec7fdd993781af649fb4

SHA256SUMS.txt
asset id: 541072598
bytes: 86
asset SHA-256:
2a07506d6c84048940a35beb7aa637de9e27dd51bea25600a9b62a5a93f6017f
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9844117414
archive bytes: 242,151,516
archive SHA-256:
f2aea11845611012d26bc135f8d6386200ea5007382d441b652ef6d1b3f86477
```

GitHub release `v1.16.4` targets exact product source `5886d8f97abd060d398d4c50d3dd3b720e4ace09`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact with digest verification, verified ProductVersion/FIRST_RUN identity, and independently matched the actual release ZIP hash against `SHA256SUMS.txt`. Later documentation-only commits are not v1.16.4 product sources and may not replace these stable assets.

Release evidence:

- `docs/.release-v1.16.4-status.json`
- `docs/RELEASE_NOTES_V1.16.4.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.16.4 Farming Guide 잠금 아이템 회귀 수정

v1.16.3에서 exact item lock을 동일 `InstanceId` 보존으로만 해석하여, 잠긴 아이템도 안전한 재배치라면 자동으로 움직일 수 있었던 동작을 바로잡았습니다.

현재 계약은 다음과 같습니다.

- 명시적으로 잠근 stored item은 자동 Farming Guide 판단에서 **물리적 위치까지 고정**됩니다.
- 자동 지시는 잠긴 아이템을 버리기·교체·이동·회전·재부모화할 수 없습니다.
- 잠긴 descendant를 담은 상위 stored container 이동이나 root carrier 교체로 잠금 아이템을 간접 이동시키는 것도 금지합니다.
- 보안 컨테이너 승격이나 일반 repacking이 잠금 위치를 바꿔야만 성립하면 그 계획을 채택하지 않습니다.
- 최종 fail-closed 검증에서 storage kind, grid, X/Y, rotation, parent, quantity, ancestor chain, root carrier identity를 다시 검사합니다.
- 사용자가 직접 편집하는 것은 계속 허용됩니다.
- 잠긴 리그·가방·보안 컨테이너 root 자체는 자동 교체하지 않지만, 그 내부의 합법적인 빈 공간은 계속 사용할 수 있습니다.

사용자가 보고한 `Wires 전선` 판단 중 잠금 상태 `Grizzly 응급 치료 키트` 이동 지시 형태를 published EXE 회귀 시나리오로 고정했습니다.

## v1.16.3에서 유지되는 판단 안전성

v1.16.4는 v1.16.3의 나머지 안전성 개선을 그대로 유지합니다.

- secure-container-eligible 고가치 loot은 ordinary free storage보다 먼저 비파괴 secure promotion 가능성을 검사합니다.
- 탄약·화폐 같은 stack은 실제 `Quantity` 전체를 가치·무게에 반영합니다.
- 여러 물품을 희생해야 할 때 실제 필요한 bounded subset을 탐색하고, incoming 총 Flea 가치가 실제 희생 전체 가치보다 엄격히 큰 경우만 허용합니다.
- reserved cells는 자동 배치 금지 계약을 유지합니다.
- expanded pockets는 실제 프로필 geometry를 사용합니다.
- 최소 음식·음료와 현재 휴대한 총기에 맞는 loose ammunition을 자동 희생하지 않습니다.
- 특별 needed 우선순위는 실제 Found in Raid 필요량에만 적용합니다.
- Content schema v12는 위 판단에 필요한 source-backed energy/hydration/caliber/allowed-ammo facts를 보존합니다.

## Farming Guide current contract

Farming Guide는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

판단 순서는 **제약 확인 → 중요도 비교 → 상황 대처 결정**입니다. weighted score나 임의 가중합을 사용하지 않습니다.

### 우선순위 / 경제성

- 특별 needed 우선순위는 **Found in Raid가 실제 필요한 아이템**에만 적용합니다.
- 비-FIR needed는 일반 경제 loot과 동일하게 판단합니다.
- 경제 가치는 **평균 Flea Market 가격**을 사용합니다.
- 공간 확보를 위해 기존 물품을 희생해야 하면 incoming item과 실제 희생되는 전체 item set의 총 Flea 가치를 비교합니다.
- 동일 가치일 때는 양쪽의 무게가 알려진 경우 더 가벼운 상태를 선호하고, 이후 ordinary footprint를 tie-break로 사용합니다.

### 장비 우위

- 방탄복 / 헬멧: 방탄 등급
- 헤드셋: 청취 거리
- 일반 리그 / 가방 / 보안 컨테이너: 수납량
- 방탄 리그: 방탄 등급 우선, 동급일 때 수납량
- 총기 / 권총: 자동 우월 교체 없음

Scanner가 관측할 수 없는 내구도, 남은 사용 횟수, 실제 총기 조립 상태는 추정하지 않습니다.

### 중첩 수납 / 잠금

실제 Tarkov source-backed `StorageGrids`와 필터가 nested storage의 권위 있는 구조입니다. Key tool, money/document/card/injector 계열 등 specialized containers도 별도 이름 allowlist가 아니라 source filter를 따릅니다.

명시적 item lock은 자동 판단에서 exact placement를 보존하는 hard constraint입니다. carrier root lock은 carrier 자체의 자동 교체를 막지만 내부 legal storage까지 폐쇄하지 않습니다. Reserved cell은 자동 배치 금지 역할을 유지합니다.

### 스택 수량 / 무게

탄약·화폐처럼 수량이 판단에 필요한 아이템은 Mini Scanner에서 수량을 입력한 뒤 판단을 계속합니다. 수량은 상태 schema v3에 저장되고 card에 표시되며 더블클릭으로 수정할 수 있습니다. 필요 수량, 경제 가치와 무게에 반영됩니다.

Farming Guide 우측 하단에서는 현재 modeled weight와 Strength 기반 최대 운반 중량을 표시합니다. 최종 proposed state가 허용 중량을 넘는 지시는 차단합니다. 현재 state가 이미 한계를 넘었다면 무게를 유지하거나 줄이는 방향만 허용합니다.

## 검증 계약

중요한 제품 변경은 가능한 범위에서 다음을 통과해야 합니다.

- deterministic tests
- Release build
- Windows x64 self-contained publish
- 실제 published EXE Product UI / Map / Farming Guide runtime smoke
- graceful shutdown
- Shutdown Race CI
- package / SHA256SUMS 검증
- PR 및 exact-main CI
- 공개 tag / release / asset identity 및 digest 검증

현재 v1.16.4는 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` 상태이며 공개 릴리즈 identity나 완료된 개발 상태를 변경하지 않습니다.
