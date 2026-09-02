# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.16.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.16.1
exact product source/tag target:
7fb148434d22fac823d57d88021f9615081c47cd
validated PR head: 7d7cf002aa4f1d61c891b340ff73c56781655d64
merge PR: #276 — MERGED
PR CI / Shutdown / Docs:
33589038565 / 33589038575 / 33589038576 — SUCCESS
exact-main CI / Shutdown / Docs:
33589274983 / 33589275133 / 33589275021 — SUCCESS
Release workflow: 33589497077 — SUCCESS
release id: 380969416
published UTC: 2026-09-02T04:06:31Z
612 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 540589667
bytes: 80,717,818
SHA-256:
8599645a2d0a38c6b74f4f79cab71120b26e378da254a98605610f1c7493b3c3

SHA256SUMS.txt
asset id: 540589668
bytes: 86
asset SHA-256:
c78b0be06dbcf3f5239591d796f3b6a94299445e45157012ee122972cbfcaeee
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9831224038
archive bytes: 242,086,160
archive SHA-256:
74435818344f94d6cd9d8fb918582dbdb3b047e789aa0f2f47c398facfbabd2a
```

GitHub release `v1.16.1` targets `7fb148434d22fac823d57d88021f9615081c47cd`. The release is neither draft nor prerelease. The Release workflow downloaded the exact-main artifact with digest verification, independently compared `SHA256SUMS.txt` against the actual release ZIP hash, and published only after they matched. Later documentation-only commits are not v1.16.1 product sources and may not replace these immutable stable assets.

Release evidence:

- `docs/.release-v1.16.1-status.json`
- `docs/RELEASE_NOTES_V1.16.1.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.16.1 maintenance hardening

v1.16.1 does not add a new user feature. It strengthens existing product-state recovery and asynchronous profile consistency while preserving the current Farming Guide contract introduced in v1.16.0.

- `farming-guide.json` now safely normalizes syntactically valid but semantically partial/null state instead of allowing null collections or nested item state to fail later during load. Salvageable presets, equipment and stored items are preserved; structurally unusable entries are discarded.
- persisted stack quantity, Strength settings, locks, fixed equipment, attachment and armor-plate subtrees are normalized within existing product contracts.
- opportunistic startup content-schema migration now pins `ProfileId + GameMode` and rechecks the active profile after asynchronous boundaries so an old profile refresh cannot be applied to a newly selected profile.
- deterministic regressions protect both recovery and stale-continuation behavior.
- the maintenance pass also reviewed MainWindow lifecycle/update paths, Scanner settings/UI state, Map/MiniMap settings/window state, atomic storage/content activation, image cache, updater/service disposal and existing rendered WPF smoke coverage.
- no additional reproduced UI defect justified speculative layout or behavior changes.

## Farming Guide current contract — introduced in v1.16.0, retained in v1.16.1

`파밍 가이드`는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

### 결정적 rulebook

판단 순서는 **제약 확인 → 중요도 비교 → 상황 대처 결정**입니다. weighted score나 여러 요소를 임의 가중합하지 않습니다.

- 보호 상태는 **잠긴 아이템 + 예약 칸**만 사용합니다.
- 특별 needed 우선순위는 **Found in Raid가 실제 필요한 아이템**에만 적용합니다.
- 비-FIR needed 아이템은 일반 경제 loot과 동일하게 취급합니다.
- 경제 가치는 **평균 Flea Market 가격**으로 통일합니다.
- 공간 부족으로 기존 물품을 버려야 할 때는 incoming item과 실제 희생되는 전체 물품의 총 Flea 가치를 비교합니다.

### 장비 우위 판단

장비 자동 교체는 단순 대표 기준만 사용합니다.

- 방탄복 / 헬멧: 방탄 등급
- 헤드셋: 청취 거리
- 일반 리그 / 가방 / 보안 컨테이너: 수납량
- 방탄 리그: 방탄 등급 우선, 동급일 때 수납량
- 총기 / 권총: 자동 우월 교체 없음

Scanner가 알 수 없는 내구도, 남은 사용 횟수, 실제 총기 조립 상태는 추정하지 않습니다.

### 잠금·예약 승계

수납 장비 교체 시 잠긴 item instance는 버리지 않고 새 장비에 합법적으로 재배치합니다. 예약 칸은 기존 좌표가 아니라 연결된 모양과 용량을 새 장비에 승계하며, 동일한 보호 상태를 만들 수 없으면 교체하지 않습니다.

### 스택 수량

탄약·화폐처럼 수량이 판단에 필요한 아이템은 Mini Scanner에서 수량을 먼저 입력한 뒤 Farming Guide 판단을 계속합니다. 저장된 스택 수량은 상태 schema v3에 보존되고, 가치·무게·needed count 계산에 반영됩니다. Farming Guide 화면에서도 수량을 표시하고 더블클릭으로 수정할 수 있습니다.

### 무게

Farming Guide 우측 하단에서 현재 modeled weight와 Strength 기반 최대 운반 중량을 표시합니다. Strength level은 profile에 저장되며, 최종 proposed state가 허용 중량을 넘는 지시는 차단합니다. 현재 상태가 이미 초과 중이면 무게를 유지하거나 줄이는 방향만 허용합니다.

### MiniMap hotkey

Bare NumPad 0–5 직접 층 선택 기능은 제거했습니다. 기존 사용자 지정 위/아래 층 이동 hotkey는 유지합니다.

## 데이터 / 호환성

```text
Desktop: 1.16.1
Game Content write/read: v11 / v3-v11
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## 설치 / 실행

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 / WPF
- self-contained single-file executable
- portable ZIP / installer 없음
- 일반 사용에 관리자 권한 불필요
- mutable user data는 `%LocalAppData%/JunhyunHelper`에 저장

## 안전 경계

Scanner와 준현 헬퍼는 외부 화면 픽셀과 사용자 입력을 기반으로 동작합니다. Tarkov game memory read, DLL/code injection, process/game hook, kernel/driver access, packet manipulation, anti-cheat bypass, 자동 loot/게임 입력 자동화는 제품 범위가 아닙니다.

## 개발 / 복구

새 작업은 `AGENTS.md`와 `docs/PROJECT_STATE.json`을 먼저 확인합니다. `docs/ACTIVE_WORK.md`가 `ACTIVE`이면 기록된 지점에서 이어가고, `NONE`이면 현재 public stable 상태에서 새 요청을 시작합니다.
