# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.17.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.17.0
exact product source/tag target:
8b0e1f8f46fa3822f4cff05b7be3223d40ad7435
validated PR head: a01d61cd9957db94a7475734c1e8df66ce71f53d
merge PR: #288 — MERGED
PR CI / Shutdown / Docs:
33746966753 / 33746966804 / 33746966771 — SUCCESS
exact-main CI / Shutdown / Docs:
33748900315 / 33748900348 / 33748900377 — SUCCESS
Release workflow: 33749193376 — SUCCESS
release id: 381959220
published UTC: 2026-09-03T11:21:35Z
649 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 542663027
bytes: 80,766,362
SHA-256:
6ecc3a61d0b492f6b475e18f309e55790776911e5496fc704d12ffd611c629cb

SHA256SUMS.txt
asset id: 542663026
bytes: 86
asset SHA-256:
7a2fb4f7ebcb333eafd8cad6f9acbf532549118e608776786666014a24875bdf
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9890816795
archive bytes: 242,234,759
archive SHA-256:
d9115f24968804fc5b4e65fa7bbaaf008f4af516e044f3b00e0ee6b4525a15dd
```

GitHub release `v1.17.0` targets exact product source `8b0e1f8f46fa3822f4cff05b7be3223d40ad7435`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact, verified ProductVersion/FIRST_RUN identity, and matched the actual release ZIP hash against `SHA256SUMS.txt`. Later documentation-only commits are not v1.17.0 product sources and must not replace these stable assets.

Release evidence:

- `docs/.release-v1.17.0-status.json`
- `docs/RELEASE_NOTES_V1.17.0.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.17.0 Farming Guide current contract

Farming Guide는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

### FIR / Scanner 경계

활성 Farming Guide raid 중 Scanner가 새로 확인한 incoming item은 Farming Guide가 그 레이드에서 획득한 FIR item으로 취급합니다.

- Scanner가 FIR 아이콘·체크·색·문자를 판독하지 않습니다.
- 사용자에게 별도의 FIR 확인을 요구하지 않습니다.
- raid 획득 provenance는 세션 전용 `RaidAcquired` 상태이며 preset에 저장하지 않습니다.

### 판단 목표

매 스캔은 로컬 빈칸 삽입이 아니라 **현재 이동 가능한 전체 상태 + incoming item의 합법적인 최종 상태**를 다시 계산합니다.

판단 목표는 정확히 두 단계입니다.

1. 현재 퀘스트·은신처에 필요한 FIR 수량을 가능한 한 많이 충족합니다. 남은 필요 수량까지만 우선순위가 있습니다.
2. 1번 결과가 같다면 최종 보유하는 모든 아이템의 평균 Flea Market 가치 합계를 최대화합니다.

음식·음료·탄약·탄창·치료제·방어구·헤드셋 등 특정 종류에 자동 전술 우선순위를 부여하지 않습니다. 사용자가 보호하고 싶은 물품/칸은 기존 고정 기능으로 지정합니다.

무게는 아이템 우선순위가 아니라 Strength 설정 기준 최종 운반 가능 여부를 판정하는 제약입니다.

### 글로벌 배치 / Tarkov legality

글로벌 solve는 다음을 함께 고려합니다.

- 일반 stored item
- top-level equipment
- Rig / Backpack / Secure Container
- container 안 container
- 전용 storage grid
- incoming Scanner item

최종 상태는 실제 데이터와 현재 상태를 기준으로 다음을 검증합니다.

- width/height, rotation, collision
- storage grid/filter
- nested parent/child와 cycle 금지
- equipment slot compatibility
- attachment / armor plate slot filter
- item conflict / `ConflictingSlotIds`
- body armor / armored rig conflict
- helmet / headset compatibility
- stack quantity
- item/cell/root lock
- final carry weight

부착물과 armor plate도 retained Flea value와 weight에 포함됩니다. Melee/Dogtag은 자동 후보에서 제외되지만 최종 무게에는 포함됩니다.

전용 컨테이너는 보관 가치 우선순위를 만드는 기능이 아니라 합법적인 placement 후보입니다.

### 고정 / 잠금

고정 item/cell은 판단 점수가 아니라 hard constraint입니다.

- 고정 item은 버리기·교체·좌표 이동·회전·re-parenting할 수 없습니다.
- 고정 descendant를 담은 상위 container나 root carrier를 움직여 간접 이동시키는 것도 금지합니다.
- 고정 carrier 내부의 독립적으로 고정되지 않은 합법적 빈칸은 계속 사용할 수 있습니다.
- 같은 storage area 안에서 합법적 global solve가 기존 item 이동/회전을 요구하면 지시에 `내부 재배치`를 표시합니다.

### 스택 수량 / 무게

Ammo와 Currency는 기존 수량 입력 흐름을 사용합니다. 입력한 수량은 하나의 실제 관측 stack instance의 수량이며 FIR 충족량, Flea value, weight에 반영됩니다. v1.17.0은 자동 split/merge나 확인되지 않은 max-stack 사실을 새로 만들지 않습니다.

### Fail closed

파괴적 지시에 필요한 사실을 증명할 수 없으면 임의 기본값을 사용하지 않습니다.

- unknown weight → 0 kg으로 가정하지 않음
- unknown geometry → 1x1로 가정하지 않음
- unknown tradable Flea price → 0 ₽로 가정하지 않음
- bounded optimizer가 optimum을 증명하지 못함 → 파괴적 지시를 만들지 않음

## 검증 계약

중요한 제품 변경은 가능한 범위에서 다음을 통과해야 합니다.

- deterministic tests
- Release build
- Windows x64 self-contained publish
- 실제 published EXE Product UI / Map / Scanner / Farming Guide runtime smoke
- graceful shutdown
- Shutdown Race CI
- package / SHA256SUMS 검증
- PR 및 exact-main CI
- 공개 tag / release / asset identity 및 digest 검증

현재 v1.17.0은 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` 상태이며 공개 릴리즈 identity나 완료된 개발 상태를 변경하지 않습니다.
