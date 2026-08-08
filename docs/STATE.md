# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 필요한 설계 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2A — 핵심 데이터 기반 구현**

상태: `IN PROGRESS — live pipeline + deterministic query boundary verified`

UI보다 먼저 Game Content / User Progress / Domain Logic을 작고 독립적인 구성으로 구현합니다.

## 제품의 핵심

준현 헬퍼는:

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 안전한 로컬 콘텐츠 갱신 → 사용자 진행과 결합 → 명시 규칙으로 정보 계산`

을 수행하는 Windows 데스크톱 도구입니다.

일반적인 패치 때 GPT가 새 데이터를 다시 해석해 수작업 DB를 만들어야 하는 구조를 금지합니다.

## 최우선 철학 — DEC-018

런타임은 생각하거나 추론하는 AI가 아닙니다.

- 검증된 명시 규칙만 실행
- 동일 입력 → 동일 결과
- 모르는 의미를 추측하지 않음
- 필요한 입력이 없으면 임의 기본값을 넣지 않음
- 안전한 판정이 불가능하면 `Indeterminate` 또는 업데이트 실패
- 새 규칙은 `의미 검증 → 코드 → 테스트` 순으로 추가

## 책임 경계

1. **Game Content** — API에서 재생성 가능한 게임 사실
2. **User Progress** — 사용자의 실제 캐릭터 진행 사실
3. **Domain Logic** — 위 둘을 이용한 순수 계산
4. **Application/UI** — 결과 표시와 사용자 명령

금지:

- Game Content / User Progress 혼합
- 파생 상태의 진실의 원천화
- 동일 규칙 중복 구현
- 기능 간 내부 저장소 직접 수정
- UI의 API JSON/SQL 직접 해석
- 미래를 위한 범용 프레임워크 남발
- 런타임 AI/GPT 의존

## 공식 문서

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

## 현재 데이터 원천

`json.tarkov.dev`:

- `tasks`
- `hideout`
- `items`
- `traders`
- `maps`
- `barters`
- `crafts`

모드:

- `regular`
- `pve`
- `pvp-season`

한국어 `ko` 지원.

2026-08-08 actual raw probe:

- Items 5,310
- Tasks 510
- Barters 789
- Crafts 214
- `ItemPropertiesAmmo` 200

이 숫자는 고정 정상 임계값으로 사용하지 않습니다.

### 열린 원천 문제

에디션별 Quest 허용/제외 규칙은 현재 `tasks` raw에 직접 존재하지 않습니다.

`EditionId`는 사용자 사실로 저장하지만 신뢰 가능한 원천이 확정되기 전까지 Quest를 edition 기준으로 추측해 필터링하지 않습니다.

## 기술 스택

- C# / .NET 10 LTS
- WPF — UI 단계에서 추가
- SQLite / `Microsoft.Data.Sqlite`
- `HttpClient`
- `System.Text.Json`
- xUnit

초기에는 ORM, DI container, 별도 backend, 범용 rule engine을 사용하지 않습니다.

## 구현 상태

### Game Content canonical model

구현됨:

- Item
- Trader / Map 최소 참조
- Quest / prerequisite / objective / item requirement
- Hideout station / level / material requirement
- Ammo performance / acquisition
- `GameContentCatalog`

관계는 이름이 아니라 stable ID를 사용합니다.

### Quest availability

결과:

- `Completed`
- `Current`
- `Locked`
- `Indeterminate`

현재 판정:

- player level
- faction
- prestige
- trader reputation
- trader loyalty level
- prerequisite `Complete`
- prerequisite `Active`
- disabled

준현 헬퍼는 수주 가능한 Quest를 이미 수락한 것으로 간주하므로 해금된 선행 Quest는 `Active` 조건에 사용할 수 있습니다.

live `traderRequirements`의 `requirementType + compareMethod` 의미를 직접 보존합니다.

- reputation `>=` → AtLeast
- reputation `<=` → AtMost
- reputation `<` → LessThan
- level `>=` → loyalty requirement

미입력 trader/prestige, Failed-only prerequisite, dependency cycle, 미지원 additional requirement는 추측하지 않고 `Indeterminate`입니다.

현재 live non-empty `otherRequirements`는 3개 Quest의 `dialogue` 유형으로 확인했으며 canonical model에 보존 후 `Indeterminate` 처리합니다.

시간 지연은 제품 결정대로 무시합니다.

### Quest objective / Needed Items

현재 의미 변환:

- `giveItem` → 제출 requirement
- `findItem / collect` → 획득 목표, 제출 합계 제외
- `sellItem` → 판매 목표, 제출 합계 제외
- quest item 관련 목표 → 일반 stash 합계 제외
- 기타 → 의미 보존, 재료로 추측하지 않음

`NeededItemCalculator`:

- Quest + Hideout 고정 요구량 Item ID별 집계
- 출처 보존
- FIR / Non-FIR 이중 계산 방지

`NeededItemRequirementBuilder`:

- accepted item 하나 → 확정 requirement
- accepted item 여러 개 → 프로그램이 임의 선택하지 않고 `AlternativeQuestRequirements`로 분리

`NeededItemsQuery`는 호출자가 명시적으로 넘긴 Quest/Hideout 요구만 계산합니다.

따라서 아직 확정하지 않은 Hideout 범위 정책이나 대체 아이템 배분 정책이 Core에 숨어 있지 않습니다.

### Quest 조회 경계

`QuestCatalogQuery` 구현.

UI가 Quest 판정 규칙을 다시 구현하지 않도록 `QuestDefinition + QuestAvailabilityResult`를 묶어 반환합니다.

`Current()`는 오직 실제 evaluator 결과가 `Current`인 Quest만 반환하며 `Indeterminate`를 Current로 섞지 않습니다.

### Ammo

탄약 식별:

`properties.propertiesType == ItemPropertiesAmmo`

성능:

- caliber / ammoType / projectileCount
- damage / armorDamage / penetrationPower
- fragmentation / ricochet
- accuracy / recoil
- initial speed
- bleed modifiers
- tracer

수급 관계:

- TraderPurchase
- TraderBarter
- HideoutCraft

가격/화폐, 상인 레벨, Quest unlock, barter/craft 요구 재료, craft tool 여부 등을 원천 의미로 보존합니다.

### User Progress / user.db

프로필 하나 = 실제 Tarkov 캐릭터 하나.

저장:

- game mode
- level / faction / edition / prestige
- trader progress
- completed Quest IDs
- hideout current levels
- inventory FIR / Non-FIR

저장하지 않음:

- Current/Locked/Indeterminate 결과
- Needed Items 결과
- 화면 집계/정렬 결과

SQLite pooling은 Windows file lock을 남겨 이 단일 로컬 DB에서는 비활성화했습니다.

### json.tarkov.dev infrastructure

구현됨:

- mode/endpoint path
- HTTP client
- envelope validation
- Korean + English fallback
- 번역은 표시 문자열에만 적용
- Item / Trader / Map / Quest / Hideout / Ammo importer
- canonical build
- reference/semantic validation

### Game Content 저장

모드별 독립:

```text
content/
  regular/
    content.db
    content.candidate.db
    content.previous.db
  pve/
    ...
  pvp-season/
    ...
```

업데이트:

`API → canonical import → validation → candidate DB → DB validation → active 교체`

실패한 candidate는 active를 건드리지 않습니다.

active 손상 시 같은 모드의 valid previous를 복구합니다.

snapshot convenience getter는 `[JsonIgnore]` 처리하여 같은 사실을 중복 저장하지 않습니다.

## 실제 검증

### 최신 normal CI

2026-08-08 Windows Server 2025 + .NET 10 SDK 10.0.302:

- restore 성공
- Core build 성공
- Infrastructure build 성공
- Tests build 성공
- **64 passed / 0 failed / 0 skipped**

### live canonical build

실제 현재 API로:

- regular 성공
- pve 성공
- pvp-season 성공

### live 전체 업데이트 흐름

세 모드 각각 실제로:

`live API → canonical build → candidate.db → validation → content.db activation → read-back`

까지 성공했습니다.

해당 임시 live probe 실행은 **58 passed / 0 failed**였으며 검증용 코드는 main에 남기지 않았습니다.

### 실제 개발 중 잡힌 주요 문제

- 취약 SQLite native dependency → 안전 버전 고정
- Windows SQLite file lock → 불필요 pooling 제거
- 모드별 콘텐츠 덮어쓰기 위험 → 저장소 완전 분리
- trader comparison 부호 추측 → live compareMethod 기반으로 수정
- `dialogue` 조건 무시 위험 → Indeterminate
- 수류탄/탄약 상자 ammo 혼입 위험 → `ItemPropertiesAmmo`만 인정
- 대체 제출 아이템 임의 선택 위험 → alternative group 분리
- snapshot 중복 직렬화 가능성 → 단일 stored field + JsonIgnore convenience view

## 아직 결정/구현하지 않는 것

- WPF UI
- Quest 실패/분기 UX
- Quest 완료 취소 UX
- 시간 지연 해금
- Item 자동 차감
- Hideout Needed Items 기본 범위
- 대체 제출 아이템 자동 배분
- edition 기반 Quest filtering
- 지도
- Scanner
- 기존 Tarkov-Helper 호환 migration

## 다음 순서

1. 현재 Core query들을 실제 사용자 행동과 연결하는 최소 application orchestration 검토
2. 미확정 제품 정책이 필요 없는 범위만 구현
3. 에디션 Quest 제한의 신뢰 가능한 원천은 별도 조사 가능하되 하드코딩 금지
4. 핵심 조회 흐름을 계속 회귀 테스트로 고정
5. Core/Application 경계가 충분히 단순한 상태에서 최소 WPF shell 시작

## 마지막 갱신

2026-08-08 — actual raw 계약 및 세 모드 live update 파이프라인 검증 완료. Ammo와 Quest 조건을 실제 원천 의미로 구현. Quest 조회와 Needed Items 입력 변환에 얇은 결정론적 query boundary를 추가하고, 대체 아이템/Hideout 범위 같은 미확정 정책은 Core가 임의 결정하지 않도록 분리. 최신 Windows/.NET 10 CI 64/64 통과.
