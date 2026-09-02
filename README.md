# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.16.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.16.2
exact product source/tag target:
81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
validated PR head: 119b47c406058ed422afdb17bace54db0f7e68f5
merge PR: #279 — MERGED
PR CI / Shutdown / Docs:
33601684251 / 33601684206 / 33601684210 — SUCCESS
exact-main CI / Shutdown / Docs:
33602013494 / 33602013351 / 33602013617 — SUCCESS
Release workflow: 33602299729 — SUCCESS
release id: 381041582
published UTC: 2026-09-02T07:11:21Z
619 passed / 0 failed / 0 skipped
```

Public package:

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

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9835631036
archive bytes: 242,089,986
archive SHA-256:
efcfb965a2a64cb7f7e3916ae3ed1c96d8eba5c0f77e1cd6090d41f6f9a5564c
```

GitHub release `v1.16.2` targets `81ce1dc93fefd633502e62cb5fdde54c2f61ce8c`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact with digest verification and independently matched `SHA256SUMS.txt` against the actual release ZIP hash. Later documentation-only commits are not v1.16.2 product sources and may not replace these stable assets.

Release evidence:

- `docs/.release-v1.16.2-status.json`
- `docs/RELEASE_NOTES_V1.16.2.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.16.2 Farming Guide regression fixes

### 파밍한 가치

Farming Guide가 실제 값을 계산하지 않고 `—`를 고정 표시하던 연결 누락을 수정했습니다.

활성 레이드의 파밍 가치는 **레이드 시작 snapshot 대비 현재 snapshot에서 순증가해 현재 보유 중인 수량 × 평균 Flea Market 가격**입니다.

- 원래 들고 들어간 아이템은 포함하지 않습니다.
- 획득 후 버린 아이템은 현재 보유량에서 빠진 만큼 가치에서도 제거됩니다.
- 시작 아이템을 잃어도 파밍 가치는 음수가 되지 않습니다.
- 탄약·화폐 등의 stack quantity를 반영합니다.
- nested storage와 snapshot inventory count도 같은 기준을 사용합니다.
- 가격이 확인되지 않는 아이템의 가격을 추정하지 않습니다.

### 예약/고정 빈칸 표시

예약 칸은 자동 배치 금지 제약을 표시하는 배경 marker입니다. 기존에는 marker가 높은 Z-index로 item card를 덮어, 사용자가 예약된 빈칸에 직접 아이템을 놓으면 상태는 정상인데 화면에서 아이템이 보이지 않았습니다.

v1.16.2에서는 reservation marker를 item card 뒤에 렌더링합니다. 자동 배치 보호는 그대로 유지되며 사용자의 직접 배치는 정상적으로 보입니다.

### 전체 Farming Guide 점검

이번 PATCH에서 다음 계약을 함께 재검토했습니다.

- raid baseline/current snapshot과 명시적 accept transaction
- FIR 필요 우선순위와 일반 경제 loot
- 평균 Flea Market 총가치와 destructive victim-set 비교
- 장비 대표 우위 기준과 가격 기반 장비 자동 교체 금지
- locked items / reserved cells / carrier migration
- nested storage, specialized filter, recursive repacking
- ammo/currency quantity 입력·표시·수정과 가치/무게 반영
- Strength 기반 최대 운반 중량과 overweight 차단
- Farming Guide persistence normalization/recovery
- Scanner/Mini Scanner bridge lifecycle
- rendered WPF와 published EXE runtime smoke

사용자가 보고한 두 회귀 외에 기존 deterministic rulebook을 변경해야 할 추가 결함은 재현되지 않았습니다. 근거 없는 규칙 변경이나 추측성 리팩터링은 하지 않았습니다.

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

### 잠금 / 예약 / 중첩 수납

수납 장비 교체 시 잠긴 item instance는 버리지 않고 새 장비에 합법적으로 재배치합니다. 예약 칸은 고정 좌표가 아니라 연결된 모양과 용량을 승계하며, 동일한 보호 상태를 만들 수 없으면 교체하지 않습니다.

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

현재 v1.16.2는 위 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` 상태이며 공개 릴리즈 identity나 완료된 자동 검증을 변경하지 않습니다.
