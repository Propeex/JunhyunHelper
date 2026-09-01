# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.15.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.15.3
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
validated PR head: db82512e6e723f2d85ed0ddf3f3c7c9b0e3a70af
merge PR: #265 — MERGED
PR CI / Shutdown / Docs:
33487099126 / 33487099119 / 33487099201 — SUCCESS
exact-main CI / Shutdown / Docs:
33487466031 / 33487466005 / 33487465946 — SUCCESS
Release workflow: 33487795730 — SUCCESS
release id: 380333729
published UTC: 2026-09-01T08:35:55Z
563 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539249489
bytes: 80,659,355
SHA-256:
a22a426de32aa20a4c158018d98a6eec96b39d460d367d33d9d970d7e2581d99

SHA256SUMS.txt
asset id: 539249490
bytes: 86
asset SHA-256:
286e27a9db1394d1a4487c5b26598f08998bb03e07e21fa116dc4fca5844fdde
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9792459273
archive bytes: 241,909,375
archive SHA-256:
c0aba02d6a465734c841b044776dfcf087bab9b29141b23c71ffb5a0a65c6cb2
```

GitHub `/releases/latest`, the release target and `refs/tags/v1.15.3` all resolve to `c35204da66eb0af454b50550c830b071a0897835`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.15.3 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_1.15.3.md`
- `docs/.release-v1.15.3-status.json`
- `docs/RELEASE_NOTES_V1.15.3.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`

## v1.15.3 Farming Guide

`파밍 가이드`는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 함께 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

### Source-backed nested storage

- 실제 current Tarkov Game Content에 `StorageGrids`가 있는 stored item은 nested storage를 열 수 있습니다.
- Key tool, 문서/돈/카드/주사기 계열 전용 컨테이너 등 특정 이름을 하드코딩하지 않습니다.
- 각 grid의 실제 width/height 및 allowed/excluded category/item filter를 그대로 사용합니다.
- Secure Container 안 특수 컨테이너, 컨테이너 안 컨테이너도 `ParentInstanceId`로 재귀적으로 관리합니다.
- source positive allow-list가 스캔 아이템을 허용하는 전용 nested grid는 일반 Secure Container/Pockets/Rig/Backpack 빈칸보다 먼저 추천합니다.
- unrestricted nested bag/rig는 기존 일반 수납 순서를 유지합니다.

### Stored-item lock 표시

- 일반 보관 아이템: neutral border
- `F`로 잠근 아이템: accent/노란색 border
- 잠금 해제: neutral border로 즉시 복귀
- 빈 칸 reservation 및 기존 장비/carrier lock 의미는 유지

### 검색 결과 + T 테스트 스캔

- 검색 결과 위에 마우스를 올린 상태에서 `T`를 누르면 Search TextBox 포커스가 남아 있어도 simulated scan이 실행됩니다.
- hover된 결과가 없으면 `T`는 정상 검색 문자 입력입니다.
- active raid session에서는 실제 Scanner-confirmed item과 같은 Farming Guide recommendation path를 사용합니다.
- Scanner capture가 꺼져 있거나 재시작 후 아직 in-memory catalog가 없더라도 verified same-mode local catalog를 필요 시 로드합니다.
- 테스트 snapshot 준비 실패는 silent no-op 대신 명시적 실패 상태로 표시됩니다.

### 장비 완제품 모델 유지

- 총기·헬멧·방탄복 등 장비는 내부 부품을 별도 관리하지 않는 opaque complete item입니다.
- 총기/헬멧 attachment, armor plate 편집과 equipment-internal raid recommendation은 없습니다.
- Primary Weapon, Pistol/Holster, Helmet, Body Armor, Rig, Backpack, Secure Container 등 최상위 장비 칸 판단은 유지됩니다.
- authoritative default-preset/source complete image를 우선하고 임의 assembly 이미지는 만들지 않습니다.

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

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout / Needed Items / Inventory / cleanup
- Items / Ammo 조회와 profile-aware 판단
- Map / MiniMap
- Scanner / Mini Scanner
- Farming Guide loadout / source-backed nested storage / raid advisor
- Game Content update with validation + Last Known Good
- Program Update
- opt-in diagnostics

## 안전 경계

Scanner와 준현 헬퍼는 외부 화면 픽셀과 사용자 입력을 기반으로 동작합니다.

다음은 제품 범위가 아닙니다.

- Tarkov game memory read
- DLL/code injection
- process/game hook
- kernel/driver access
- packet/network manipulation
- anti-cheat bypass
- 자동 loot / 게임 입력 자동화

## 개발 / 복구

새 작업은 `AGENTS.md`와 `docs/PROJECT_STATE.json`을 먼저 확인합니다. `docs/ACTIVE_WORK.md`가 `ACTIVE`이면 기록된 지점에서 이어가고, `NONE`이면 현재 public stable 상태에서 새 요청을 시작합니다.
