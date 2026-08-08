# STATE — 현재 프로젝트 상태

> 이 문서는 새 개발자가 가장 빠르게 현재 상태를 복구하기 위한 핸드오프 문서입니다.

## 현재 Phase

**Phase 1 — 제품 발견 및 핵심 시스템/데이터 설계**

상태: `IN PROGRESS — 핵심 시스템 + 내부 데이터 모델 큰 틀 완료`

제품 UI 코드는 아직 작성하지 않습니다.

## 프로젝트 이름

**준현 헬퍼**

저장소: `Propeex/JunhyunHelper`

## 프로젝트의 현재 핵심 정의

준현 헬퍼의 핵심은 **최신 Tarkov 데이터를 온라인에서 받아 프로그램 스스로 내부 데이터베이스로 변환·재구축하고, 그 데이터를 게임 진행 상태와 결합해 사용하는 것**입니다.

금지 구조:

`새 패치 데이터 → GPT가 다시 해석 → 수작업 데이터 갱신`

목표 구조:

`GPT/개발자가 변환 규칙 구현 → 프로그램이 최신 데이터 다운로드 → 검증 → 변환 → DB 재구축 → 기능이 최신 DB 사용`

일반적인 데이터 업데이트에는 GPT가 필요하지 않아야 합니다.

---

## 공식 설계 문서

- `docs/SYSTEM_DESIGN.md` — 핵심 시스템 경계와 데이터 흐름
- `docs/MAINTENANCE_PHILOSOPHY.md` — 복잡성 억제/유지보수 원칙
- `docs/DATA_MODEL.md` — 준현 헬퍼 canonical 내부 데이터 모델
- `docs/DATA_VALIDATION.md` — 데이터 계약/검증/안전한 활성화 규칙
- `docs/DATA_SOURCE_AUDIT.md` — 2026-08-08 최신 외부 데이터 원천 검증
- `docs/LEGACY_SALVAGE_AUDIT.md` — 기존 Tarkov-Helper 회수 후보/폐기 대상

---

## 독립 시스템 재설계

`COMPLETED — 큰 틀`

기존 `Propeex/Tarkov-Helper`를 리팩터링 출발점으로 사용하지 않고 현재 준현 헬퍼의 제품 의도에서 시스템을 새로 설계했습니다.

핵심 경계:

1. **Game Content** — 온라인에서 다시 만들 수 있는 게임 데이터
2. **User Progress** — 사용자의 실제 캐릭터 진행 상태
3. **Domain Logic** — Game Content + User Progress에서 결과 계산
4. **Application/UI** — 조회와 사용자 명령

핵심 유지보수 규칙:

- 게임 데이터와 사용자 진행 데이터를 섞지 않습니다.
- 계산 가능한 파생 상태는 가능한 한 영구 저장하지 않습니다.
- 같은 게임 규칙을 여러 화면/서비스에서 중복 판정하지 않습니다.
- 한 기능이 다른 기능의 내부 저장 구조를 직접 수정하지 않습니다.
- UI는 API JSON/SQL/게임 규칙을 직접 해석하지 않습니다.
- 긴 이벤트 연쇄보다 명시적 상태 변경 + 재계산을 선호합니다.
- 외부 API 변경은 Importer/Validator 경계에서 흡수합니다.
- 데이터 업데이트 실패 시 기존 정상 콘텐츠와 사용자 진행을 보호합니다.
- 미래를 예상한 범용 규칙 엔진/과도한 계층을 만들지 않습니다.

---

## 최신 데이터 공급원 검증

`COMPLETED — 큰 틀 / 구현 시 raw fixture 계약 고정 필요`

2026-08-08 현재 `json.tarkov.dev` endpoint catalog에서 확인:

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

TarkovTracker의 최신 실제 소비 코드와 문서를 대조해 다음 의미가 현재 원천에 존재함을 확인:

- Quest: min level, faction, prerequisite task/status, trader requirement, prestige, objective/fail condition/reward
- Hideout: station/level, item requirement, station/trader/skill requirement, craft
- Item: stable item id, display data, categories, properties
- Tasks payload: quest items, prestige

현재 결론:

- **퀘스트:** `json.tarkov.dev/tasks` 사용 방향 유지
- **은신처:** `json.tarkov.dev/hideout`를 주 원천으로 설계 가능
- **탄약:** `items` + `traders/barters/crafts/hideout` 조합으로 구축하는 방향이 적합
- **필요 아이템:** Quest/Hideout 내부 요구 모델에서 파생 가능

남은 원천 검증:

- 에디션별 quest 허용/제외 규칙의 안정적인 원천
- 탄약 `properties`의 필요한 raw key/type
- `barters`/`crafts`의 실제 raw shape

이 세 항목은 구현 시 실제 최신 응답을 고정 fixture로 저장해 contract test로 확정합니다.

---

## 내부 데이터 모델

`CONFIRMED — 큰 틀`

외부 API의 모든 필드를 DB에 복사하지 않습니다.

준현 헬퍼가 실제 계산/표시에 사용하는 의미만 canonical model로 변환합니다.

### 공통

- 영구 식별은 이름이 아니라 API/게임의 안정적인 ID 사용
- 한국어 표시 + 영어 fallback
- Map은 현재 Quest 분류용 최소 참조만 사용

### Quest

- 기본 정보
- 해금 판정 규칙
- Objective
- `QuestItemRequirement`를 별도 의미 모델로 정규화

Objective의 내부 식별은 `(questId, objectiveId)`.

필요 아이템 계산은 원본 objective type을 화면마다 해석하지 않고, Importer가 제출/획득/판매/기타 의미를 한 번 정규화합니다.

### Hideout

- Station
- Level
- Item Requirement
- 기타 station/trader/skill requirement

사용자 진행에는 station의 현재 level만 저장하는 것을 기본으로 합니다.

### Ammo

Ammo는 별도 아이템 체계가 아니라 ItemId를 참조하는 성능/수급 정보입니다.

수급처는 문자열이 아니라 `TraderPurchase / TraderBarter / HideoutCraft` 관계로 저장합니다.

### User Progress

프로필 하나 = 실제 Tarkov 캐릭터 하나.

- game mode
- level/faction/edition/prestige
- 필요한 trader progress
- completed quests
- hideout levels
- item inventory(FIR/non-FIR)

### Derived State

영구 저장하지 않는 기본 결과:

- Quest `Locked / Current`
- Needed Items
- 남은 수량/집계

---

## 데이터 검증/업데이트 원칙

`CONFIRMED — 큰 틀`

업데이트:

`download → envelope/schema validation → import → reference/semantic validation → candidate DB → DB validation → activation`

- 후보 DB는 active DB와 별도로 만듭니다.
- Fatal 오류 시 현재 정상 콘텐츠를 그대로 유지합니다.
- 한국어 누락 등 표시용 문제는 영어 fallback + warning 가능.
- 새로운 requirement/objective type이 핵심 계산에 영향을 줄 가능성이 있으면 자동 추측하지 않고 갱신을 막습니다.
- 대형 패치에서 실제 데이터 수가 크게 바뀔 수 있으므로 고정 row-count 임계치만으로 실패시키지 않습니다.
- 구조/참조/의미 무결성을 활성화 판단의 핵심으로 사용합니다.

테스트는 세 층으로 분리:

1. deterministic contract fixture
2. live source contract test
3. pure domain regression test

---

## 현재 핵심 기능 범위

### API 기반 데이터베이스 업데이트

`CONFIRMED`

Game Content를 온라인에서 받아 검증/변환하고 성공한 콘텐츠만 활성화합니다.

### 게임 진행 프로필

`CONFIRMED / 큰 틀`

게임 모드별 실제 캐릭터 진행을 독립 관리합니다.

### 퀘스트

`CONFIRMED / 큰 틀`

- 조건을 만족한 미완료 퀘스트 = `Current`
- 수주 가능은 자동 수락으로 간주
- `Completed`만 사용자 상태로 저장
- 시간 지연 해금 제외

### 은신처

`CONFIRMED / 큰 틀`

현재 시설 레벨 + 최신 Hideout 정의에서 요구 재료를 계산합니다.

### 필요 아이템

`CONFIRMED / 큰 틀`

`Quest Item Requirements + Hideout Item Requirements + User Inventory → Needed Items`

출처를 보존하고 FIR/일반을 이중 계산하지 않습니다.

### 탄약

`CONFIRMED / 큰 틀`

최신 성능값과 판매/교환/제작 수급처 관계를 제공합니다.

---

## 지도 / Scanner

핵심 시스템 이후의 독립 기능입니다.

- 지도/Scanner는 핵심 기능을 조회할 수 있음
- 핵심 Quest/Hideout/NeededItem/Ammo 시스템은 지도/Scanner 존재를 몰라야 함

---

## 지금 미루는 세부사항

- 구체적인 UI 레이아웃
- 완료 취소/오입력 복구
- 실패/분기형 퀘스트의 세부 UX
- Item 자동 차감 정책
- 은신처 필요 아이템 범위(다음 레벨/전체 미래)의 기본 UX
- 자동 갱신 주기
- 상세 프로필 입력 화면
- 지도/Scanner 구현 방식

---

## 바로 다음 행동

1. 데스크톱 앱 기술 스택을 유지보수 철학 기준으로 최소 구성으로 확정합니다.
2. 프로젝트 구조를 단순한 책임 경계에 맞춰 설계합니다.
3. 가장 먼저 `Game Content Import/Validation` 기반을 구현합니다.
4. 실제 `json.tarkov.dev` 응답을 fixture로 저장해 raw 계약을 고정합니다.
5. Quest/Hideout/Item의 canonical importer부터 구현하고 테스트합니다.
6. 그 위에 User Progress와 순수 Quest/NeededItem 계산을 추가합니다.
7. UI는 핵심 계산이 검증된 뒤 붙입니다.

## 마지막 갱신

2026-08-08 — 최신 `json.tarkov.dev` endpoint catalog 및 TarkovTracker 운영 소비 코드를 대조하여 퀘스트·은신처·아이템·탄약/수급 관계 원천의 큰 틀을 검증. `DATA_MODEL.md`, `DATA_VALIDATION.md`, `DATA_SOURCE_AUDIT.md`를 추가하고 최소 canonical model, 의미 기반 objective 변환, 안전한 candidate activation, deterministic/live/domain 3층 테스트 전략을 확정.
