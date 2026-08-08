# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 관련 설계 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2A — 핵심 데이터 기반 구현**

상태: `IN PROGRESS`

제품의 큰 틀, 유지보수 철학, canonical 데이터 모델을 확정했고 **UI보다 Game Content / User Progress / 순수 계산 기반을 먼저 실제 코드로 구현하고 있습니다.**

## 프로젝트

- 제품명: **준현 헬퍼**
- 저장소: `Propeex/JunhyunHelper`

## 최상위 원칙

준현 헬퍼는:

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 검증된 콘텐츠 저장 → 사용자 진행과 결합 → 정보 계산`

으로 동작합니다.

일반적인 Tarkov 패치 때:

`새 데이터 → GPT 재해석 → 수작업 DB 수정`

이 필요하지 않아야 합니다.

### 결정론적 도구 원칙

`CONFIRMED — DEC-018`

준현 헬퍼는 런타임에서 생각하거나 추론하는 AI가 아닙니다.

- 개발 단계에서 검증한 명시적 규칙만 실행
- 동일한 입력에는 동일한 결과
- 새/불명확한 의미를 비슷해 보인다는 이유로 추측하지 않음
- 필요한 사용자 입력이 없으면 임의 기본값을 넣지 않음
- 안전하게 판정할 수 없으면 `Indeterminate`/업데이트 실패로 명시
- 새 규칙은 개발 단계에서 의미 검증 → 코드 → 테스트 순으로 추가

## 유지보수 경계

1. **Game Content** — API에서 다시 만들 수 있는 게임 사실
2. **User Progress** — 사용자의 실제 캐릭터 진행
3. **Domain Logic** — 두 데이터를 이용한 순수 계산
4. **Application/UI** — 표시와 사용자 명령

규칙:

- Game Content와 User Progress를 섞지 않음
- 계산 가능한 파생 상태를 진실의 원천으로 저장하지 않음
- 한 규칙을 여러 화면/서비스가 중복 구현하지 않음
- 기능끼리 다른 기능의 내부 저장소를 직접 수정하지 않음
- UI는 API JSON/SQL을 직접 해석하지 않음
- 미래를 예상한 범용 프레임워크/규칙 엔진을 만들지 않음
- 모르는 값을 0/false 등으로 조용히 가정하지 않음

## 주요 공식 문서

- `docs/PRODUCT.md`
- `docs/SYSTEM_DESIGN.md`
- `docs/MAINTENANCE_PHILOSOPHY.md`
- `docs/DATA_MODEL.md`
- `docs/DATA_VALIDATION.md`
- `docs/DATA_SOURCE_AUDIT.md`
- `docs/TECH_STACK.md`
- `docs/CONTENT_STORAGE.md`
- `docs/LEGACY_SALVAGE_AUDIT.md`
- `docs/DECISIONS.md`

## 데이터 공급원

2026-08-08 현재 `json.tarkov.dev`에서 확인한 핵심 원천:

- `tasks`
- `hideout`
- `items`
- `traders`
- `maps`
- `barters`
- `crafts`

게임 모드:

- `regular`
- `pve`
- `pvp-season`

한국어 `ko` 지원.

현재 방향:

- Quest → `tasks`
- Hideout → `hideout`
- Items/common refs → `items/traders/maps`
- Ammo → `items + traders/barters/crafts/hideout`

추가 검증 필요:

- 에디션별 Quest 허용/제외 규칙의 안정적인 원천
- Ammo `properties`의 실제 current raw contract
- `barters` / `crafts` 실제 raw shape

외부 참고로 현재 Tarkov ammo 데이터에서 사용하는 핵심 의미 필드(`caliber`, `damage`, `penetrationPower`, `armorDamage`, `fragmentationChance`, `accuracyModifier`, `recoilModifier`, `initialSpeed`, bleed modifiers 등)는 확인했으나, 준현 헬퍼 importer는 실제 `json.tarkov.dev/items` raw fixture로 계약을 고정한 뒤 확정합니다.

## 기술 스택

초기 핵심 구현:

- C# / .NET 10 LTS
- WPF (UI 단계에서 사용)
- SQLite via `Microsoft.Data.Sqlite`
- `HttpClient`
- `System.Text.Json`
- xUnit

초기에는 ORM, DI container, 범용 rule engine, 별도 backend, 런타임 AI를 사용하지 않습니다.

프로젝트 경계:

```text
src/
  JunhyunHelper.Core/
  JunhyunHelper.Infrastructure/
  JunhyunHelper.Desktop/   # UI 단계에서 추가

tests/
  JunhyunHelper.Tests/
```

## 현재 구현 상태

### Core

구현됨:

- Item 식별/표시 모델
- FIR / Non-FIR 보유량
- Quest/Hideout item requirement source 모델
- Needed Item 결과 모델 및 계산기
- GameMode / PMC faction / TraderProgress
- GameProfileSnapshot
- Quest 기본 정의 및 조건 모델
  - prerequisite status: Complete / Active / Failed
  - level / faction / trader standing / trader loyalty / prestige
  - trader standing 비교 방향 `AtLeast / AtMost`
- Quest Objective 모델
- Quest Item Requirement 의미 모델
- Hideout station/level/item requirement 모델
- Trader / Map 최소 참조 모델
- GameContentCatalog

중요:

- 현재 퀘스트/필요 아이템 등 파생 결과는 프로필에 저장하지 않음
- Quest objective ID는 `(questId, objectiveId)` 범위로 취급
- 대체 가능한 Quest 제출 아이템은 하나의 requirement group으로 보존
- 평판 요구는 숫자만 저장하지 않고 비교 방향까지 canonical 의미로 보존

### Quest availability — 순수 계산 구현

`QuestAvailabilityEvaluator` 구현됨.

내부 상태:

- `Completed` — 프로필에 완료 사실이 저장됨
- `Current` — 확인 가능한 모든 조건 충족 + 미완료
- `Locked` — 알려진 조건 중 하나 이상이 명확히 미충족
- `Indeterminate` — 필요한 사실이 없거나 아직 제품이 표현하지 않는 상태 때문에 안전한 판정 불가

현재 판정:

- level
- faction
- prestige
- trader standing
- trader loyalty
- prerequisite Complete
- prerequisite Active
  - 준현 헬퍼의 자동 수락 원칙에 따라 현재 수주 가능한 선행 퀘스트는 Active로 간주
- disabled

의도적으로 추측하지 않는 것:

- 필요한 trader progress가 프로필에 없으면 0으로 가정하지 않고 `Indeterminate`
- prestige가 필요한데 값이 없으면 `Indeterminate`
- Failed-only prerequisite는 실패 진행 UX가 아직 정의되지 않았으므로 `Indeterminate`
- 순환 prerequisite가 발견되면 임의로 풀지 않고 `Indeterminate`

시간 지연은 확정 결정대로 판정하지 않습니다.

### Needed Items — 순수 계산 기반 구현

구현됨:

- Quest + Hideout requirement를 Item ID별 집계
- 출처 보존
- FIR/일반 이중 계산 방지
- 관련 회귀 테스트 작성

아직 확정하지 않은 부분:

- Hideout을 다음 레벨만 계산할지 전체 미래 레벨까지 계산할지 기본 UX
- 대체 아이템 requirement를 사용자에게 어떻게 표현/계산할지

### User Progress / user.db — 최소 저장 구현

`UserProfileStore` 구현됨.

저장 단위:

- 프로필 ID
- game mode
- level / faction / edition / prestige
- trader progress
- completed quest IDs
- hideout current levels
- inventory FIR / non-FIR

저장하지 않는 것:

- Current/Locked quest 결과
- Needed Items 결과
- 화면 정렬/집계 결과

`user.db`는 프로필별 versioned JSON payload를 작은 SQLite 테이블에 저장합니다.

Game Content처럼 삭제 후 재생성할 수 없는 사용자 데이터이므로 schema version이 달라지면 자동 추측하지 않고 향후 명시적 migration을 추가합니다.

현재 저장 검증은 음수 레벨/프레스티지/로열티/보유량 등 명백히 잘못된 값을 조용히 보정하지 않고 거부합니다.

### json.tarkov.dev Infrastructure

구현됨:

- `regular / pve / pvp-season` source path 매핑
- game mode data key를 Core에서 단일 정의
- endpoint enum
- HTTP client
- `data` / `translations` envelope 검증
- 한국어 + 영어 fallback catalog
- 번역은 표시 문자열에만 적용하고 ID/관계에는 적용하지 않음
- 번역 endpoint 실패는 warning으로 낮추고 본문 데이터는 유지
- Item importer
- Trader importer
- Map reference importer
- Quest rule importer
- Quest objective importer
- Hideout material importer
- 각 importer를 GameContentCatalog로 조립하는 `TarkovGameContentImporter`

### 의미 기반 Quest material 변환

현재 분류:

- `giveItem` → 실제 제출 requirement
- `findItem / collect` → 획득/발견 목표
- `sellItem` → 판매 목표
- 기타 → Objective에는 보존하되 Needed Items에 자동 합산하지 않음

Optional submit objective도 기본 필요 재료 합계에는 넣지 않습니다.

이 분류는 live fixture 검증 단계에서 현재 실제 objective type 전체와 대조합니다.

### 콘텐츠 참조 검증

`GameContentValidator`가 현재 다음 핵심 관계를 검사합니다.

- Quest → prerequisite Quest
- Quest → Trader
- Quest → Map
- Quest trader condition → Trader
- Quest Item Requirement → Quest / Item
- Hideout Item Requirement → Item

핵심 참조 누락은 Fatal이며 후보 콘텐츠를 활성화할 수 없습니다.

### 콘텐츠 빌드 흐름

`TarkovContentBuildService`:

```text
Items/Traders/Maps/Tasks/Hideout 다운로드
→ localized source 준비
→ canonical import
→ reference validation
→ build result
```

### 콘텐츠 저장 — 게임 모드별 독립

검증된 `GameContentCatalog` 전체를 **versioned canonical snapshot**으로 SQLite에 저장합니다.

게임 모드별 파일은 서로 완전히 분리합니다.

```text
content/
  regular/
    content.db
    content.candidate.db
    content.previous.db
  pve/
    content.db
    content.candidate.db
    content.previous.db
  pvp-season/
    content.db
    content.candidate.db
    content.previous.db
```

PvP 데이터 업데이트가 PvE/시즌 active 파일을 수정할 수 없습니다.

candidate snapshot의 game mode가 저장 경로와 다르면 활성화를 거부합니다.

### 안전한 데이터 업데이트

`TarkovContentUpdateService` 흐름:

```text
해당 mode old candidate 폐기
→ 해당 mode 온라인 build
→ semantic/reference validation
→ 해당 mode candidate.db 생성
→ SQLite integrity / deserialize / canonical validation
→ 같은 mode active 교체
→ 이전 active는 previous로 보존
```

candidate 실패 시 같은 모드의 active를 건드리지 않습니다.

active 읽기 실패 시 같은 모드의 previous가 정상이라면 복구합니다.

## 테스트 상태

작성된 테스트 범위:

- Needed Items FIR/일반 계산
- Quest + Hideout source 집계
- Quest availability: level/faction/completed/prerequisite active/unknown input/failed-only/cycle
- trader standing 최소/최대 비교
- API game mode/path 계약
- JSON envelope 계약
- 한국어/영어 fallback
- Item importer
- Hideout item importer
- Quest prerequisite/status/trader/prestige importer
- 음수 trader standing 비교 방향 보존
- `findItem/sellItem`이 제출 재료로 유출되지 않는지
- 다른 Quest에서 같은 objective ID 사용 가능
- 같은 Quest 내부 objective ID 중복 차단
- canonical reference validation
- content.db snapshot roundtrip
- candidate → active 교체 / invalid candidate 보호
- game mode별 content active 분리
- 잘못된 mode candidate 활성화 거부
- user.db profile roundtrip
- PvP/PvE 프로필 독립 저장
- 동일 profile 저장 시 최신 사실로 교체
- 잘못된 음수 inventory 저장 거부

### 검증 주의

현재 실행 환경에는 .NET SDK가 없어 로컬 `dotnet test`를 실행하지 못했습니다.

GitHub CI는 `.NET 10 + Windows`에서 test project를 실행하도록 구성했지만, 현재 연결 도구에서는 push check-run 결과를 직접 확인하지 못했습니다.

따라서 **테스트 코드는 작성되었지만 CI 통과를 아직 공식 확인한 상태는 아닙니다.**

컴파일/CI를 확인 가능한 경로가 생기면 실패를 가장 먼저 수정합니다.

## 아직 구현하지 않는 것

- WPF UI
- Quest 실패/분기 사용자 UX
- Quest 완료 취소 UX
- 시간 지연 해금
- Item 자동 차감
- Hideout 필요량 기본 범위 UX
- Ammo canonical importer / acquisition 관계
- 지도
- Scanner
- 기존 Tarkov-Helper 호환 migration

## 바로 다음 구현 순서

1. 실제 `json.tarkov.dev` raw fixture 확보 및 importer contract 고정
2. 실제 objective type 전체를 대조해 Quest material semantic 분류 검증
3. Ammo `properties` raw contract 고정
4. Ammo 성능 canonical model/importer 구현
5. `barters` / `crafts` raw contract 검증 후 acquisition 관계 추가
6. 현재 Core/Infrastructure의 실제 컴파일·테스트 실패가 확인되면 즉시 우선 수정
7. Game Content + User Progress + Domain 계산 기반이 검증된 뒤 Desktop/WPF 시작

## 마지막 갱신

2026-08-08 — DEC-018 결정론적 런타임 원칙 확정. Quest availability evaluator를 명시적 규칙만으로 구현하고 미입력/미확정 조건을 `Indeterminate`로 분리. trader standing 비교 방향을 canonical 의미에 추가. `user.db`에 사용자 입력 사실만 저장하는 최소 Profile store 구현. Game Content active/candidate/previous를 `regular / pve / pvp-season`별로 분리하여 모드 간 덮어쓰기를 제거. UI는 아직 시작하지 않음.
