# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고, 필요한 설계 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2A — 핵심 데이터 기반 구현**

상태: `IN PROGRESS — live data pipeline verified`

UI보다 먼저 **Game Content / User Progress / Domain Logic**을 작고 독립적인 구성으로 구현하고 있습니다.

## 제품의 한 문장 정의

준현 헬퍼는:

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 안전한 로컬 콘텐츠 갱신 → 사용자 진행과 결합 → 정보 계산`

을 수행하는 Windows 데스크톱 도구입니다.

일반적인 Tarkov 패치 때 GPT가 데이터를 다시 해석하거나 수작업 DB를 만들어야 하는 구조를 금지합니다.

## 최우선 철학 — DEC-018

준현 헬퍼 런타임은 생각하거나 추론하는 AI가 아닙니다.

- 개발 시 검증해 구현한 명시적 규칙만 실행
- 동일 입력 → 동일 결과
- 모르는 데이터 의미를 비슷해 보인다는 이유로 추측하지 않음
- 필요한 사용자 입력이 없으면 0/false 등 임의 기본값을 넣지 않음
- 안전하게 계산할 수 없으면 `Indeterminate` 또는 업데이트 실패로 명시
- 새 규칙은 `의미 검증 → 코드 → 테스트` 순으로 추가

## 유지보수 경계

1. **Game Content** — API에서 다시 만들 수 있는 게임 사실
2. **User Progress** — 사용자의 실제 캐릭터 진행 사실
3. **Domain Logic** — 위 둘을 이용한 순수 계산
4. **Application/UI** — 표시와 사용자 명령

금지:

- Game Content와 User Progress 혼합
- 계산 가능한 파생 상태의 영구 저장
- 동일 규칙의 여러 화면/서비스 중복 구현
- 기능 간 내부 저장소 직접 수정
- UI의 API JSON/SQL 직접 해석
- 미래를 예상한 범용 프레임워크/규칙 엔진
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

## 데이터 공급원

2026-08-08 현재 `json.tarkov.dev` 사용 원천:

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

### 실제 raw 계약 직접 검증 완료

2026-08-08 GitHub Actions runner에서 현재 `regular` raw JSON을 직접 다운로드하여 검사했습니다.

당시 개수:

- Items: **5,310**
- Tasks: **510**
- Barters: **789**
- Crafts: **214**
- 실제 탄약(`ItemPropertiesAmmo`): **200**

이 숫자는 패치 후 데이터 정상 여부를 판단하는 고정 임계값으로 사용하지 않습니다.

상세 계약은 `DATA_SOURCE_AUDIT.md` 참조.

### 아직 열린 데이터 원천 문제

**에디션별 Quest 허용/제외 조건**은 현재 `json.tarkov.dev/tasks` raw에 직접 존재하지 않습니다.

`EditionId`는 사용자 프로필 사실로 보존하지만 신뢰 가능한 규칙 원천을 확정하기 전까지 에디션 기준으로 Quest를 추측해 숨기지 않습니다.

## 기술 스택

- C# / .NET 10 LTS
- WPF — UI 단계에서 추가
- SQLite / `Microsoft.Data.Sqlite`
- `HttpClient`
- `System.Text.Json`
- xUnit

초기에는 ORM, DI container, 별도 backend, 범용 rule engine을 사용하지 않습니다.

```text
src/
  JunhyunHelper.Core/
  JunhyunHelper.Infrastructure/
  JunhyunHelper.Desktop/   # UI 단계에서 추가

tests/
  JunhyunHelper.Tests/
```

## 구현 완료된 기반

### Core models

- Item / FIR·Non-FIR inventory
- GameMode / PMC faction / TraderProgress
- GameProfileSnapshot
- Quest definition / prerequisite / objective / item requirement
- Hideout station / level / item requirement
- Ammo performance / acquisition
- Trader / Map 최소 참조
- GameContentCatalog

영구 식별은 이름이 아니라 stable ID를 사용합니다.

### Quest availability

`QuestAvailabilityEvaluator` 구현.

결과:

- `Completed`
- `Current`
- `Locked`
- `Indeterminate`

현재 명시적으로 계산하는 조건:

- player level
- faction
- prestige
- trader reputation
- trader loyalty level
- prerequisite `Complete`
- prerequisite `Active`
- disabled

준현 헬퍼는 수주 가능한 퀘스트를 자동 수락한 것으로 간주하므로, 해금된 선행 퀘스트는 `Active` 조건을 만족할 수 있습니다.

실제 live task 계약을 기준으로 `traderRequirements`를 다음처럼 해석합니다.

- `reputation + >=` → `AtLeast`
- `reputation + <=` → `AtMost`
- `reputation + <` → `LessThan`
- `level + >=` → loyalty level requirement

숫자의 부호로 비교 방향을 추측하지 않습니다.

의도적으로 `Indeterminate`로 남기는 것:

- 필요한 trader progress 미입력
- 필요한 prestige 미입력
- `Failed` 전용 prerequisite — 실패 진행 UX 미확정
- dependency cycle
- 아직 지원하지 않는 추가 availability requirement

현재 live data의 비어 있지 않은 `otherRequirements`는 3개 Quest에서 `dialogue` 유형으로 확인했습니다. 이를 무시해서 Current로 오판하지 않고 canonical data에 보존한 뒤 `Indeterminate`로 판정합니다.

시간 지연 해금은 제품 결정대로 무시합니다.

### Quest objective → Needed Items 의미 변환

현재 live regular에서 실제 objective type 집합을 직접 확인했습니다.

Needed Items 관련 기본 분류:

- `giveItem` → stash 제출 requirement
- `findItem` / `collect` → 획득 목표, 제출 재료 합계에서 제외
- `sellItem` → 판매 목표, 제출 재료 합계에서 제외
- `findQuestItem` / `giveQuestItem` → 일반 stash 아이템 집계에 자동 포함하지 않음
- 기타 objective → 원래 의미는 보존하되 재료로 추측하지 않음

`giveItem.items`의 여러 item ID는 대체 가능한 하나의 requirement group으로 보존합니다.

### Needed Items

구현:

- Quest + Hideout 요구량 Item ID별 집계
- 요구 출처 보존
- FIR / Non-FIR 이중 계산 방지

아직 제품 판단이 필요한 부분은 억지로 구현하지 않습니다.

- Hideout 필요량을 기본적으로 다음 레벨만 볼지 전체 미래 레벨까지 볼지
- 대체 가능한 Quest 제출 아이템을 보유량과 어떻게 배분할지

### Ammo

canonical Ammo importer 구현 완료.

탄약 식별:

`properties.propertiesType == ItemPropertiesAmmo`

`types`에 `ammo`가 있다는 이유만으로 포함하지 않아 grenade/ammo box가 탄약 표에 섞이지 않습니다.

현재 canonical 성능값:

- caliber / ammoType / projectileCount
- damage / armorDamage / penetrationPower
- fragmentationChance / ricochetChance
- accuracyModifier / recoilModifier
- initialSpeed
- heavyBleedModifier / lightBleedModifier
- tracer / tracerColor

수급 관계:

- `TraderPurchase` — item `buyFromTrader`
- `TraderBarter` — `barters`
- `HideoutCraft` — `crafts`

구매 가격/화폐, 상인 레벨, Quest unlock, barter 요구 아이템, craft 재료/도구/시간/시설 레벨 등 실제 원천 의미를 명시적으로 보존합니다.

### User Progress / `user.db`

`UserProfileStore` 구현.

프로필별 저장 사실:

- game mode
- level / faction / edition / prestige
- trader progress
- completed quest IDs
- hideout current levels
- inventory FIR / Non-FIR

저장하지 않는 것:

- Current / Locked / Indeterminate 결과
- Needed Items 결과
- 화면 집계/정렬 결과

`user.db`는 작은 versioned JSON payload를 SQLite에 저장합니다.

SQLite connection pooling은 이 단일 로컬 DB 용도에 불필요하고 Windows 파일 잠금을 남겼으므로 비활성화했습니다.

### json.tarkov.dev Infrastructure

구현:

- `regular / pve / pvp-season` 경로
- endpoint enum / HTTP client
- `data` / `translations` envelope 검증
- 한국어 + 영어 fallback
- 번역은 표시 문자열에만 적용, ID/관계에는 적용하지 않음
- 번역 실패는 warning, 본문 데이터 실패는 fatal
- Item / Trader / Map importer
- Quest rule / objective importer
- Hideout material importer
- Ammo importer
- `TarkovGameContentImporter`
- `TarkovContentBuildService`

### Canonical reference validation

`GameContentValidator`가 최소 다음을 검증합니다.

- Quest → prerequisite Quest
- Quest → Trader / Map
- Quest trader condition → Trader
- Quest Item Requirement → Quest / Item
- Hideout Item Requirement → Item
- Ammo → Item
- Ammo acquisition → Trader / Hideout station / unlock Quest / currency / required Item
- duplicate Ammo ID

핵심 참조 누락은 candidate 활성화를 막습니다.

### Game Content 저장 / 안전한 업데이트

Game Content는 수십 개 관계형 테이블로 다시 복제하지 않고 **versioned canonical snapshot**으로 SQLite에 저장합니다.

게임 모드별 완전 분리:

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

업데이트:

```text
해당 mode API download
→ canonical import
→ semantic/reference validation
→ content.candidate.db
→ SQLite integrity / deserialize / canonical validation
→ content.db 교체
→ 기존 active는 content.previous.db
```

- candidate 실패 → active 유지
- active 손상 + previous 정상 → previous 복구
- candidate의 저장 mode가 경로 mode와 다르면 거부
- PvP/PvE/season 콘텐츠는 서로 덮어쓸 수 없음
- convenience 계산 getter는 `[JsonIgnore]` 처리하여 snapshot에 같은 사실을 중복 저장하지 않음

## 실제 검증 결과

### 최신 main CI

`VERIFIED — 2026-08-08`

Windows Server 2025 + .NET 10 SDK `10.0.302`:

- NuGet restore: 성공
- Core build: 성공
- Infrastructure build: 성공
- Tests build: 성공
- **57 passed / 0 failed / 0 skipped**

snapshot round-trip 테스트는 Ammo와 미지원 Quest availability requirement(`dialogue`)가 저장/복원 후에도 보존되는지 확인합니다.

### live API canonical build

실제 현재 `json.tarkov.dev`를 사용한 임시 검증에서:

- `regular` canonical build: 성공
- `pve` canonical build: 성공
- `pvp-season` canonical build: 성공

각 모드에서 Item / Quest / Hideout / Ammo가 비어 있지 않고 canonical reference validation을 통과했습니다.

### live API 전체 업데이트 흐름

실제 세 모드 각각에 대해 다음 전체 흐름을 Windows CI에서 직접 실행했습니다.

```text
live API
→ canonical build
→ validation
→ content.candidate.db
→ DB validation
→ content.db activation
→ content.db read-back
```

결과: **모든 모드 성공**.

해당 임시 live probe를 포함한 실행은 **58 passed / 0 failed**였습니다. 임시 probe/PR은 검증 후 닫았고 제품 코드에 남기지 않았습니다.

### 검증 과정에서 실제 발견·수정한 문제

1. 취약한 SQLite native dependency가 선택됨
   - 경고 억제 대신 `SQLitePCLRaw.bundle_e_sqlite3 2.1.12`로 안전한 버전 고정
2. xUnit analyzer/collection test 오류
   - 경고 비활성화 없이 테스트 코드 수정
3. Windows에서 `user.db` file handle 유지
   - 불필요한 SQLite connection pooling 비활성화
4. 게임 모드별 콘텐츠가 단일 active DB를 공유할 위험
   - `regular / pve / pvp-season` 저장소 완전 분리
5. trader reputation 비교 방향을 값의 부호에서 추측하던 초기 모델
   - live raw의 `requirementType + compareMethod`를 직접 보존하도록 수정
6. `otherRequirements.dialogue`를 무시하면 일부 Quest가 잘못 Current가 될 위험
   - 미지원 availability requirement를 보존하고 `Indeterminate` 처리
7. convenience getter가 canonical snapshot에 같은 데이터를 이중 직렬화할 가능성
   - 저장 원본은 한 필드만 두고 convenience getter는 `[JsonIgnore]`

## 아직 구현하지 않는 것

- WPF UI
- Quest 실패/분기 사용자 UX
- Quest 완료 취소 UX
- 시간 지연 해금
- Item 자동 차감
- Hideout 필요량 기본 범위 UX
- 대체 아이템 자동 배분 정책
- 에디션별 Quest 필터 — 신뢰 가능한 원천 미확정
- 지도
- Scanner
- 기존 Tarkov-Helper 호환 migration

## 바로 다음 순서

1. Core 결과를 UI가 단순히 조회할 수 있는 얇은 application/query 경계 설계
2. Quest availability + Quest material + Hideout material + Inventory를 연결하되 미확정 Hideout 범위/대체 아이템 정책은 주입된 입력으로만 처리
3. 필요하면 에디션별 Quest 제한의 신뢰 가능한 데이터 원천 추가 조사
4. 핵심 query 흐름 회귀 테스트
5. 데이터/계산/application 경계가 안정된 뒤 최소 WPF shell 시작

## 마지막 갱신

2026-08-08 — 현재 `json.tarkov.dev` raw 계약을 직접 확보·검증하여 Quest 상인 조건과 objective 의미를 수정하고 Ammo 성능/구매/교환/제작 importer를 구현. 모든 지원 게임 모드에서 실제 API → canonical build → candidate DB → active DB → read-back 전체 흐름 성공을 검증. 최신 main은 Windows/.NET 10에서 57/57 테스트 통과. 런타임 추론 없이 명시 규칙만 실행한다는 유지보수 철학을 계속 유지.
