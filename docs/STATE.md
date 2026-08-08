# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 필요한 설계 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2A — 핵심 데이터 기반 구현 + Quest UX 확정**

상태: `IN PROGRESS — live pipeline, deterministic query boundary, edition rules verified`

UI보다 먼저 Game Content / User Progress / Domain Logic을 작고 독립적인 구성으로 구현합니다. 퀘스트는 이제 실제 사용자 행동과 탐색 방식까지 큰 틀이 확정되어 최소 WPF 화면으로 내려갈 준비가 되고 있습니다.

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
- `docs/QUEST_EXPERIENCE.md`
- `docs/SYSTEM_DESIGN.md`
- `docs/MAINTENANCE_PHILOSOPHY.md`
- `docs/DATA_MODEL.md`
- `docs/DATA_VALIDATION.md`
- `docs/DATA_SOURCE_AUDIT.md`
- `docs/TECH_STACK.md`
- `docs/CONTENT_STORAGE.md`
- `docs/LEGACY_SALVAGE_AUDIT.md`
- `docs/LEGACY_UI_REFERENCE.md`
- `docs/DECISIONS.md`

## 현재 데이터 원천

### 1차 원천 — `json.tarkov.dev`

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

### 에디션 보조 원천

`json.tarkov.dev/tasks`에는 에디션별 Quest 허용/제외 규칙이 직접 존재하지 않습니다.

따라서 TarkovTracker의 `tarkov-data-overlay` 중 **`editions` 섹션만** 별도 명시적 원천으로 사용합니다.

준현 헬퍼가 현재 소비하는 의미:

- Edition ID / 표시명
- `exclusiveTaskIds`
- `excludedTaskIds`

전체 community correction overlay를 자동 적용하는 결정은 하지 않았습니다.

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
- Edition quest rules
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
- edition exclusive/excluded quest rules
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

미입력 edition/trader/prestige, Failed-only prerequisite, dependency cycle, 미지원 additional requirement는 추측하지 않고 `Indeterminate`입니다.

현재 live non-empty `otherRequirements`는 일부 Quest의 `dialogue` 유형으로 확인했으며 canonical model에 보존 후 `Indeterminate` 처리합니다.

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

UI가 Quest 판정 규칙을 다시 구현하지 않도록 `GameContentCatalog + GameProfileSnapshot`을 받아 `QuestDefinition + QuestAvailabilityResult`를 묶어 반환합니다.

Edition rules도 이 경계를 통해 자동으로 적용되며 UI가 별도로 잊을 수 없습니다.

`Current()`는 오직 실제 evaluator 결과가 `Current`인 Quest만 반환하며 `Indeterminate`를 Current로 섞지 않습니다.

### Quest 제품 동작 — 최신 확정

세부 기준은 `docs/QUEST_EXPERIENCE.md`.

- 게임 로그 기반 자동 완료 추적 사용 안 함
- 사용자가 실제 게임 완료 후 수동으로 Quest 완료 처리
- 완료 취소 가능
- 일반 탐색 상태: `진행 중 / 잠김 / 완료`
- 모든 Quest를 상태별로 탐색 가능
- 상태 / 상인 / 지도는 dropdown filter로 조합
- 검색 제공
- `Indeterminate`는 정상 상태가 아니라 별도 **판정 문제**로 취급
- Quest detail은 기본 정보 / 목표 / 제출 아이템 / 해금 조건 / 선행 Quest / 분기 정보 / 보상 / Wiki를 중심으로 설계
- 개별 objective 수동 진행 체크는 현재 핵심 진행 모델에서 제외

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

`API + edition source → canonical import → validation → candidate DB → DB validation → active 교체`

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
- **78 passed / 0 failed / 0 skipped**

### live canonical build / 전체 업데이트 흐름

실제 현재 `json.tarkov.dev + edition overlay(editions only)`로 세 모드 각각:

`live sources → canonical build → candidate.db → validation → content.db activation → read-back`

까지 성공했습니다.

- regular 성공
- pve 성공
- pvp-season 성공

해당 임시 live probe 포함 실행은 **79 passed / 0 failed**였으며 검증용 probe 코드는 main에 남기지 않았습니다.

### 실제 개발 중 잡힌 주요 문제

- 취약 SQLite native dependency → 안전 버전 고정
- Windows SQLite file lock → 불필요 pooling 제거
- 모드별 콘텐츠 덮어쓰기 위험 → 저장소 완전 분리
- trader comparison 부호 추측 → live compareMethod 기반으로 수정
- `dialogue` 조건 무시 위험 → Indeterminate
- 수류탄/탄약 상자 ammo 혼입 위험 → `ItemPropertiesAmmo`만 인정
- 대체 제출 아이템 임의 선택 위험 → alternative group 분리
- snapshot 중복 직렬화 가능성 → 단일 stored field + JsonIgnore convenience view
- Edition rules snapshot `IReadOnlySet` 역직렬화 실패 → 구체적 `HashSet` 저장 계약으로 수정

## 기존 Tarkov-Helper UI 참고

`docs/LEGACY_UI_REFERENCE.md`에 UI만 별도 검토했습니다.

적극 참고:

- 적은 수의 상위 탭
- 검색 + ComboBox filter bar
- 목록 / 우측 detail split view
- 상태 badge
- 행의 작은 주 행동 버튼
- section화된 detail
- 선행 Quest 클릭 탐색
- Item icon / FIR badge
- Hideout +/- level control
- Ammo caliber dropdown + table

가져오지 않음:

- Quest recommendation panel
- Kappa 특수 UI를 Core 흐름에 혼합
- Quest 화면의 faction/profile 중복 토글
- objective 수동 진행 체크를 기본 진행 모델로 사용
- `초기화`처럼 의미가 모호한 Quest action
- code-behind가 상태 판정/서비스/데이터 로딩을 모두 담당하는 구조

## 아직 결정/구현하지 않는 것

- WPF 실제 화면 스타일/픽셀 배치
- Quest 실패/분기 상태를 사용자가 어떻게 입력할지
- 시간 지연 해금
- Item 자동 차감
- Hideout Needed Items 기본 범위
- 대체 제출 아이템 자동 배분
- 지도
- Scanner
- 기존 Tarkov-Helper 호환 migration

## 다음 순서

1. 확정된 Quest UX를 지원하는 최소 Application command/query 경계 구현
2. `완료 / 완료 취소`를 user.db와 deterministic recalculation에 연결
3. Quest 상태/상인/지도 filter view model은 UI 전용 파생값으로 구성
4. `Indeterminate` 진단 조회를 별도 query로 제공
5. 그 다음 최소 WPF shell + Quest 화면 구현
6. Quest UI가 안정되면 Hideout 제품 세부 동작으로 이동

## 마지막 갱신

2026-08-08 — 수동 Quest 완료 방식, 완료 취소, 전체 상태 탐색, dropdown filter, Indeterminate 별도 판정 문제 취급을 확정. 기존 Tarkov-Helper UI를 별도 검토해 사용성이 좋은 상호작용 패턴만 회수하고 구현 구조는 승계하지 않기로 정리. Edition 보조 원천까지 실제 온라인 세 모드 전체 update flow에서 검증 완료.
