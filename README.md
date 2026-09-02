# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.16.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.16.3
exact product source/tag target:
89fae2e07b721b1dfd4922642412fcebf01b275d
validated PR head: 1c223a696e896e1af2ec1c35ec727eb3c70aa44d
merge PR: #282 — MERGED
PR CI / Shutdown / Docs:
33618363995 / 33618364028 / 33618363996 — SUCCESS
exact-main CI / Shutdown / Docs:
33618724736 / 33618724737 / 33618725069 — SUCCESS
Release workflow: 33619033186 — SUCCESS
release id: 381157194
published UTC: 2026-09-02T10:21:57Z
623 passed / 0 failed / 0 skipped
```

Public package:

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

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9842117423
archive bytes: 242,138,760
archive SHA-256:
cda8d29a6dfa3499df8ba23522ed7faeb11475e726c6b8ed66566bb29eda55eb
```

GitHub release `v1.16.3` targets `89fae2e07b721b1dfd4922642412fcebf01b275d`, is neither draft nor prerelease, and was published only after the Release workflow checked out that exact main commit, re-downloaded the exact-main artifact with digest verification, verified ProductVersion/FIRST_RUN identity, and independently matched `SHA256SUMS.txt` against the actual release ZIP hash. Later documentation-only commits are not v1.16.3 product sources and may not replace these stable assets.

Release evidence:

- `docs/.release-v1.16.3-status.json`
- `docs/RELEASE_NOTES_V1.16.3.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.16.3 Farming Guide 판단 안전성 강화

### 보안 컨테이너 우선 보호

LEDX처럼 보안 컨테이너에 들어갈 수 있는 고가치 loot은 일반 빈칸을 먼저 발견했다고 곧바로 거기에 보관하지 않습니다. 먼저 보안 컨테이너 내부의 더 낮은 우선순위 물품을 다른 합법적인 공간으로 **버리지 않고** 옮길 수 있는지 검사한 뒤, 가능한 경우 고가치 incoming item을 보안 컨테이너로 승격합니다.

안전한 승격이 불가능하거나 Tarkov source filter상 보안 컨테이너에 들어갈 수 없는 아이템이면 기존 ordinary placement / destructive-placement 규칙으로 정상적으로 넘어갑니다.

### 실제 희생 가치와 조합

- 탄약·화폐 같은 stack은 stored `Quantity` 전체를 가치·무게에 반영합니다.
- 공간 확보를 위해 여러 물품을 희생해야 하면 실제 필요한 bounded subset을 탐색합니다.
- 단순 가치순 prefix 때문에 공간 확보와 무관한 싼 물품까지 함께 버리지 않습니다.
- incoming 총 Flea 가치가 실제 전체 희생 가치보다 엄격히 커야 한다는 기존 계약을 유지합니다.

### 잠금·예약·확장 주머니

- 잠긴 가방·리그·보안 컨테이너의 root는 자동 교체하지 않지만 내부 합법 수납 공간은 계속 사용할 수 있습니다.
- 잠긴 item instance는 동일 `InstanceId`가 보존되는 비파괴 재배치라면 이동할 수 있습니다.
- reserved cells는 자동 배치 금지 계약을 계속 유지합니다.
- 모든 관련 transition/repacking 경로는 현재 프로필의 실제 pocket geometry를 사용하므로 expanded pockets도 그대로 반영됩니다.

### 생존 자원과 현재 총기 탄약

자동 destructive recommendation은 최소 음식·음료와 현재 휴대한 총기에 맞는 loose ammunition을 희생하지 않습니다. 이 분류는 아이템 이름 추측이 아니라 Tarkov source의 `energy`, `hydration`, ammo/weapon `caliber`, weapon `allowedAmmo` 사실을 사용합니다.

### 최종 fail-closed 안전 경계

파괴적 recommendation은 최종 지시 전에 다시 다음을 검증합니다.

- explicit locks
- reserved/protected state
- modeled food/drink reserve
- current-weapon compatible loose ammo
- stack quantity
- complete sacrificed Flea value
- modeled carry-weight constraint

안전성을 증명할 수 없으면 recommendation을 자동으로 거부하고 현재 상태를 유지합니다.

### FIR 우선순위

특별 needed 우선순위는 incoming과 existing loot 모두 **실제 Found in Raid 필요량**에만 적용합니다. 일반 필요량만 있는 non-FIR item은 기존 계약대로 ordinary economic loot과 동일하게 평균 Flea 가치로 판단합니다.

### Content schema v12

v1.16.3은 위 판단에 필요한 source-backed item/weapon facts를 보존하기 위해 Content write schema를 v12로 올렸습니다. v3-v12를 읽을 수 있으며 구버전 cache는 정상적으로 읽은 뒤 current update path에서 v12로 갱신됩니다.

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

### 중첩 수납

실제 Tarkov source-backed `StorageGrids`와 필터가 nested storage의 권위 있는 구조입니다. Key tool, money/document/card/injector 계열 등 specialized containers도 별도 이름 allowlist가 아니라 source filter를 따릅니다.

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

현재 v1.16.3은 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` 상태이며 공개 릴리즈 identity나 완료된 개발 상태를 변경하지 않습니다.
