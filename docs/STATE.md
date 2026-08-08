# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 관련 설계 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2A — 핵심 데이터 기반 구현**

상태: `IN PROGRESS`

제품의 큰 틀, 유지보수 철학, canonical 데이터 모델을 확정했고 **UI보다 Game Content 기반을 먼저 실제 코드로 구현하기 시작했습니다.**

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
- Ammo → `items + traders/barters/crafts/hideout` (세부 raw 계약 검증 남음)

추가 검증 필요:

- 에디션별 Quest 허용/제외 규칙의 안정적인 원천
- Ammo `properties` raw key/type
- `barters` / `crafts` 실제 raw shape

## 기술 스택

초기 핵심 구현:

- C# / .NET 10 LTS
- WPF (UI 단계에서 사용)
- SQLite via `Microsoft.Data.Sqlite`
- `HttpClient`
- `System.Text.Json`
- xUnit

초기에는 ORM, DI container, 범용 rule engine, 별도 backend를 사용하지 않습니다.

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

### Core — 구현 시작

구현됨:

- Item 식별/표시 모델
- FIR / Non-FIR 보유량
- Quest/Hideout item requirement source 모델
- Needed Item 결과 모델 및 계산기
- GameMode / PMC faction / TraderProgress
- GameProfileSnapshot 큰 틀
- Quest 기본 정의 및 조건 모델
  - prerequisite status: Complete / Active / Failed
  - level / faction / trader standing / trader loyalty / prestige
- Quest Objective 모델
- Quest Item Requirement 의미 모델
- Hideout station/level/item requirement 모델
- Trader / Map 최소 참조 모델
- GameContentCatalog

중요:

- 현재 퀘스트/필요 아이템 등 파생 결과는 프로필에 저장하지 않음
- Quest objective ID는 `(questId, objectiveId)` 범위로 취급
- 대체 가능한 Quest 제출 아이템은 하나의 requirement group으로 보존

### Needed Items — 순수 계산 기반 구현

구현됨:

- Quest + Hideout requirement를 Item ID별 집계
- 출처 보존
- FIR/일반 이중 계산 방지
- 관련 회귀 테스트 작성

아직 확정하지 않은 부분:

- Hideout을 다음 레벨만 계산할지 전체 미래 레벨까지 계산할지 기본 UX
- 대체 아이템 requirement를 사용자에게 어떻게 표현/계산할지

### json.tarkov.dev Infrastructure — 구현 시작

구현됨:

- `regular / pve / pvp-season` source path 매핑
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

### 의미 기반 Quest material 변환 — 구현됨

현재 분류:

- `giveItem` → 실제 제출 requirement
- `findItem / collect` → 획득/발견 목표
- `sellItem` → 판매 목표
- 기타 → Objective에는 보존하되 Needed Items에 자동 합산하지 않음

Optional submit objective도 기본 필요 재료 합계에는 넣지 않습니다.

이 분류는 live fixture 검증 단계에서 현재 실제 objective type 전체와 대조합니다.

### 콘텐츠 참조 검증 — 구현됨

`GameContentValidator`가 현재 다음 핵심 관계를 검사합니다.

- Quest → prerequisite Quest
- Quest → Trader
- Quest → Map
- Quest trader condition → Trader
- Quest Item Requirement → Quest / Item
- Hideout Item Requirement → Item

핵심 참조 누락은 Fatal이며 후보 콘텐츠를 활성화할 수 없습니다.

### 콘텐츠 빌드 흐름 — 구현됨

`TarkovContentBuildService`:

```text
Items/Traders/Maps/Tasks/Hideout 다운로드
→ localized source 준비
→ canonical import
→ reference validation
→ build result
```

### 콘텐츠 저장 — 구현됨

`content.db`는 수십 개 관계 테이블로 Game Content 의미를 다시 복제하지 않습니다.

검증된 `GameContentCatalog` 전체를 **versioned canonical snapshot**으로 SQLite에 저장합니다.

이유:

- 현재 콘텐츠 규모는 시작 시 메모리 로딩에 충분히 작음
- Game Content는 온라인에서 통째로 재생성 가능
- C# model + SQLite relational schema라는 이중 스키마 유지 비용을 피함

파일:

- `content.db` — active
- `content.candidate.db` — 새 후보
- `content.previous.db` — 직전 정상본

### 안전한 데이터 업데이트 — 구현됨

`TarkovContentUpdateService` 흐름:

```text
old candidate 폐기
→ 온라인 build
→ semantic/reference validation
→ candidate.db 생성
→ SQLite integrity / deserialize / canonical validation
→ active 교체
→ 이전 active는 previous로 보존
```

candidate 실패 시 active를 건드리지 않습니다.

active 읽기 실패 시 previous가 정상이라면 복구하는 경로도 구현했습니다.

## 테스트 상태

작성된 테스트 범위:

- Needed Items FIR/일반 계산
- Quest + Hideout source 집계
- API game mode/path 계약
- JSON envelope 계약
- 한국어/영어 fallback
- Item importer
- Hideout item importer
- Quest prerequisite/status/trader/prestige importer
- `findItem/sellItem`이 제출 재료로 유출되지 않는지
- 다른 Quest에서 같은 objective ID 사용 가능
- 같은 Quest 내부 objective ID 중복 차단
- canonical reference validation
- content.db snapshot roundtrip
- candidate → active 교체 / invalid candidate 보호

### 검증 주의

현재 실행 환경에는 .NET SDK가 없어 로컬 `dotnet test`를 실행하지 못했습니다.

GitHub CI는 `.NET 10 + Windows`에서 test project를 실행하도록 구성했지만, 현재 연결 도구에서는 push check-run 결과를 직접 확인하지 못했습니다.

따라서 **테스트 코드는 작성되었지만 CI 통과를 아직 공식 확인한 상태는 아닙니다.**

CI 결과를 확인 가능한 경로가 생기면 실패를 먼저 수정하고 다음 구현으로 진행합니다.

## 아직 구현하지 않는 것

- WPF UI
- Quest 실패/분기 UX
- Quest 완료 취소 UX
- 시간 지연 해금
- Item 자동 차감
- Hideout 필요량 기본 범위 UX
- 지도
- Scanner
- 기존 Tarkov-Helper 호환 migration

## 바로 다음 구현 순서

1. 현재 작성된 Core/Infrastructure의 실제 컴파일·테스트 실패가 있으면 우선 수정
2. 실제 `json.tarkov.dev` 응답 fixture 확보 및 importer contract 고정
3. 실제 objective type 전체를 대조해 Quest material semantic 분류 검증
4. Ammo raw fields + trader/barter/craft 관계 importer 구현
5. Quest availability evaluator 구현
   - 현재 확정된 조건부터
   - Failed 분기는 사용자 UX 결정 전까지 별도 처리
6. `user.db` 최소 저장 구조 구현
7. Game Content + User Progress + Domain 계산이 모두 검증된 뒤 Desktop/WPF 시작

## 마지막 갱신

2026-08-08 — 핵심 데이터 기반 구현 시작. 최소 Core 모델/Needed Items 계산, json.tarkov.dev client와 canonical importer, 의미 기반 Quest material 분리, 콘텐츠 참조 검증, versioned snapshot SQLite 저장, candidate/active/previous 안전 교체 흐름을 구현. UI는 아직 시작하지 않음.
