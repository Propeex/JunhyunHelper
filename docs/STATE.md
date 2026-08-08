# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고, 필요한 설계 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2A — 핵심 데이터 기반 구현**

상태: `IN PROGRESS — verified baseline`

UI보다 먼저 **Game Content / User Progress / Domain Logic**을 작고 독립적인 구성으로 구현하고 있습니다.

## 제품의 한 문장 정의

준현 헬퍼는:

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 안전한 로컬 콘텐츠 갱신 → 사용자 진행과 결합 → 정보 계산`

을 수행하는 Windows 데스크톱 도구입니다.

일반적인 Tarkov 패치 때 GPT가 데이터를 다시 해석하거나 수작업 DB를 만들어야 하는 구조를 금지합니다.

## 최우선 철학

### 결정론적 도구 — DEC-018

준현 헬퍼 런타임은 생각하거나 추론하는 AI가 아닙니다.

- 개발 시 검증해 구현한 명시적 규칙만 실행
- 동일 입력 → 동일 결과
- 모르는 데이터 의미를 비슷해 보인다는 이유로 추측하지 않음
- 필요한 사용자 입력이 없으면 0/false 등 임의 기본값을 넣지 않음
- 안전하게 계산할 수 없으면 `Indeterminate` 또는 업데이트 실패로 명시
- 새 규칙은 `의미 검증 → 코드 → 테스트` 순으로 추가

### 유지보수 경계

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

2026-08-08 기준 `json.tarkov.dev` 핵심 원천:

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

현재 사용 방향:

- Quest → `tasks`
- Hideout → `hideout`
- Items/common refs → `items / traders / maps`
- Ammo → `items + traders / barters / crafts / hideout`

아직 실제 raw fixture로 고정할 계약:

- 에디션별 Quest 허용/제외 원천
- Ammo `properties` 실제 key/type
- `barters` / `crafts` raw shape
- 현재 전체 Quest objective type 집합

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
- trader standing
- trader loyalty
- prerequisite `Complete`
- prerequisite `Active`
- disabled

준현 헬퍼는 수주 가능한 퀘스트를 자동 수락한 것으로 간주하므로, 해금된 선행 퀘스트는 `Active` 조건을 만족할 수 있습니다.

의도적으로 추측하지 않는 것:

- 필요한 trader progress 미입력 → `Indeterminate`
- 필요한 prestige 미입력 → `Indeterminate`
- `Failed` 전용 prerequisite → 실패 진행 UX가 확정될 때까지 `Indeterminate`
- dependency cycle → `Indeterminate`

시간 지연 해금은 제품 결정대로 무시합니다.

Trader standing은 값만 저장하지 않고 `AtLeast / AtMost` 비교 의미까지 canonical model에 보존합니다.

### Needed Items

구현:

- Quest + Hideout 요구량 Item ID별 집계
- 요구 출처 보존
- FIR / Non-FIR 이중 계산 방지

Quest objective 의미를 한 번만 정규화합니다.

- `giveItem` → 제출 재료
- `findItem / collect` → 획득 목표, 필요 재료 합계에서 제외
- `sellItem` → 판매 목표, 필요 재료 합계에서 제외
- 기타 → objective에는 보존, 자동 합산하지 않음

이 분류는 실제 live fixture로 다시 검증합니다.

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

- Current / Locked quest 결과
- Needed Items 결과
- 화면 집계/정렬 결과

`user.db`는 작은 versioned JSON payload를 SQLite에 저장합니다. 사용자 데이터는 재생성할 수 없으므로 알 수 없는 schema version을 자동 추측하지 않습니다.

SQLite connection pooling은 이 단일 로컬 DB 용도에 불필요하고 Windows 파일 잠금을 남겼으므로 비활성화했습니다.

### json.tarkov.dev Infrastructure

구현:

- `regular / pve / pvp-season` 경로
- endpoint enum / HTTP client
- `data` / `translations` envelope 검증
- 한국어 + 영어 fallback
- 번역은 표시 문자열에만 적용, ID/관계에는 적용하지 않음
- 번역 실패는 warning, 본문 데이터 실패는 fatal
- Item importer
- Trader importer
- Map reference importer
- Quest rule importer
- Quest objective importer
- Hideout material importer
- `TarkovGameContentImporter`
- `TarkovContentBuildService`

### Canonical reference validation

`GameContentValidator`가 최소 다음을 검증합니다.

- Quest → prerequisite Quest
- Quest → Trader
- Quest → Map
- Quest trader condition → Trader
- Quest Item Requirement → Quest / Item
- Hideout Item Requirement → Item

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

## 실제 CI 검증

`VERIFIED — 2026-08-08`

Windows Server 2025 + .NET 10 SDK `10.0.302`에서 실제 GitHub Actions 검증 완료.

결과:

- NuGet restore: 성공
- `JunhyunHelper.Core` build: 성공
- `JunhyunHelper.Infrastructure` build: 성공
- `JunhyunHelper.Tests` build: 성공
- tests: **50 passed / 0 failed / 0 skipped**

검증 과정에서 실제로 발견해 수정한 문제:

1. `SQLitePCLRaw.lib.e_sqlite3 2.1.11` 보안 취약점 경고
   - 경고 억제하지 않음
   - `SQLitePCLRaw.bundle_e_sqlite3 2.1.12`로 안전한 최소 버전 고정
2. 테스트의 잘못된 `IReadOnlySet` collection expression
   - 명시적 `HashSet`으로 수정
3. xUnit cancellation analyzer 경고
   - 경고 비활성화하지 않고 테스트에서 cancellation token 전달
4. `user.db` Windows file lock
   - SQLite connection pooling 비활성화

임시 CI 검증 PR #2는 성공 확인 후 닫았고 merge하지 않았습니다.

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

## 바로 다음 순서

1. 실제 `json.tarkov.dev` raw fixture 확보 및 importer contract 고정
2. 실제 Quest objective type 전체와 현재 의미 분류 대조
3. Ammo `properties` raw contract 고정
4. Ammo 성능 canonical model/importer 구현
5. `barters` / `crafts` raw contract 검증 후 acquisition 관계 구현
6. Game Content + User Progress + Domain 계산 통합 검증
7. 핵심 데이터/계산 기반이 충분히 검증된 뒤에만 WPF UI 시작

## 마지막 갱신

2026-08-08 — 결정론적 런타임 원칙을 코드에 반영하고 Quest availability, `user.db`, 게임 모드별 콘텐츠 저장을 구현. 의존성 보안 문제와 Windows SQLite 파일 잠금을 실제 CI에서 발견·수정. Windows/.NET 10에서 전체 50개 테스트 통과를 공식 확인. UI는 아직 시작하지 않음.
