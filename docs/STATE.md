# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 필요한 공식 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2B — 실제 Desktop 핵심 흐름 구현**

상태: `IN PROGRESS — data pipeline + profile management + Quest desktop flow verified`

현재 사용자는 프로그램 안에서 게임 모드별 캐릭터 프로필을 직접 만들고 수정한 뒤, 그 사용자 사실과 최신 Game Content를 결합한 Quest 목록을 실제 WPF 화면에서 사용할 수 있습니다.

다음 제품 영역은 **Hideout 상세 동작**입니다. 구현을 확장하더라도 현재의 단순한 책임 경계는 유지합니다.

---

## 제품의 핵심

준현 헬퍼는:

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 안전한 로컬 콘텐츠 갱신 → 사용자 진행과 결합 → 명시 규칙으로 정보 계산`

을 수행하는 Windows 데스크톱 도구입니다.

일반적인 Tarkov 패치 때 GPT가 새 데이터를 다시 해석해 수작업 DB를 만들어야 하는 구조를 금지합니다.

### 최우선 철학 — DEC-018

런타임은 생각하거나 추론하는 AI가 아닙니다.

- 검증된 명시 규칙만 실행
- 동일 입력 → 동일 결과
- 모르는 의미를 추측하지 않음
- 필요한 입력이 없으면 임의 기본값을 넣지 않음
- 안전한 판정이 불가능하면 `Indeterminate` 또는 업데이트 실패
- 새 규칙은 `의미 검증 → 코드 → 테스트` 순으로 추가

---

## 책임 경계

1. **Game Content** — 온라인 원천에서 재생성 가능한 게임 사실
2. **User Progress** — 사용자의 실제 캐릭터 진행 사실
3. **Domain Logic** — 위 둘을 이용한 순수 계산
4. **Application** — 사용자 명령과 저장/재계산 흐름의 얇은 조정
5. **Desktop/UI** — 결과 표시와 사용자 입력 전달

금지:

- Game Content와 User Progress 혼합
- 파생 상태를 별도 진실의 원천으로 저장
- 같은 규칙을 여러 화면/서비스에서 중복 구현
- 기능 간 내부 저장소 직접 수정
- UI의 API JSON/SQL 직접 해석
- 미래를 위한 범용 프레임워크 남발
- 런타임 AI/GPT 의존

---

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

---

## 현재 데이터 원천

### 1차 원천 — `json.tarkov.dev`

- `tasks`
- `hideout`
- `items`
- `traders`
- `maps`
- `barters`
- `crafts`

지원 모드:

- `regular`
- `pve`
- `pvp-season`

한국어 `ko` 사용.

### 에디션 보조 원천

`json.tarkov.dev/tasks`에 없는 에디션별 Quest 허용/제외 규칙만 TarkovTracker `tarkov-data-overlay`의 **`editions` 섹션**에서 가져옵니다.

현재 소비 의미:

- Edition ID / 표시명
- `exclusiveTaskIds`
- `excludedTaskIds`

전체 community correction overlay를 자동 적용하지 않습니다.

---

## 기술 스택

- C# / .NET 10 LTS
- WPF
- SQLite / `Microsoft.Data.Sqlite`
- `HttpClient`
- `System.Text.Json`
- xUnit

현재 사용하지 않음:

- ORM
- DI container
- 별도 backend
- 범용 rule engine
- runtime AI

---

## 구현 상태

### Game Content

구현됨:

- Item
- Trader / Map 최소 참조
- Edition Quest rules
- Quest / prerequisite / objective / submit-item requirement
- Hideout station / level / material requirement
- Ammo performance / acquisition
- `GameContentCatalog`

관계는 표시 이름이 아니라 stable ID를 사용합니다.

### 콘텐츠 업데이트

모드별 저장소:

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

흐름:

`API + edition source → canonical import → semantic/reference validation → candidate DB → DB validation → active 교체`

- 실패한 candidate는 active를 건드리지 않음
- active 손상 시 같은 모드의 valid previous 복구 가능
- Game Content는 재생성 가능한 snapshot

### User Progress / `user.db`

프로필 하나 = 실제 Tarkov 캐릭터 하나.

현재 준현 헬퍼가 생성하는 프로필은 지원 게임 모드별 현재 캐릭터 하나를 표현하며 같은 모드의 중복 프로필을 자동 생성하지 않습니다.

저장하는 사용자 사실:

- game mode
- level
- faction
- edition
- prestige
- 선택적으로 입력한 trader loyalty / standing
- completed Quest IDs
- hideout current levels
- inventory FIR / Non-FIR

저장하지 않는 파생 상태:

- Current/Locked/Indeterminate
- Needed Items 결과
- 화면 필터/정렬/집계 결과

### Profile Application / UI

`ProfileApplicationService` 구현.

책임:

- game mode별 프로필 생성
- 사용자 설정값 수정
- `user.db` 저장
- profile 목록 조회

프로필 설정 수정 시 **보존**:

- completed Quest IDs
- hideout levels
- inventory

WPF profile UI:

- `새 프로필`
- 게임 모드 선택 (`PvP / PvE / 시즌`)
- 해당 모드 콘텐츠가 없으면 안전한 온라인 구축
- level 입력
- faction 선택
- edition 선택
- prestige 입력
- trader별 입력 여부 + LL + standing
- 기존 profile 수정

Trader progress의 중요한 원칙:

- 입력하지 않은 trader는 dictionary에 넣지 않음
- 미입력을 평판/LL 0으로 추측하지 않음
- 그 값이 필요한 Quest는 evaluator가 `Indeterminate`로 진단

로그나 게임 화면에서 profile 값을 자동 추측하지 않습니다.

### Quest availability

정상 상태:

- `Completed`
- `Current`
- `Locked`

진단 상태:

- `Indeterminate`

현재 명시적으로 계산:

- player level
- faction
- edition exclusive/excluded rules
- prestige
- trader reputation
- trader loyalty
- prerequisite `Complete`
- prerequisite `Active`
- disabled

의도적으로 추측하지 않는 것:

- 미입력 profile value
- Failed-only prerequisite
- dependency cycle
- `dialogue` 등 미지원 additional requirement

시간 지연 해금은 제품 결정에 따라 계산하지 않습니다.

### Quest Application

`QuestApplicationService` 구현.

- profile + Game Content로 Quest workspace 조회
- 수동 `완료`
- `완료 취소`
- user.db 저장
- 저장 후 전체 deterministic Quest 재계산
- `Indeterminate`를 `Problems`로 별도 반환

완료/취소가 다른 기능의 저장 상태를 직접 수정하지 않습니다.

### WPF Desktop / Quest 화면

현재 실제 흐름:

1. profile 생성 또는 기존 profile 선택
2. profile 모드의 Game Content 로드/복구/최초 구축
3. Quest evaluator 실행
4. Quest 화면 표시
5. 사용자 완료/완료 취소 → user fact 수정 → 재계산

Quest 화면:

- 검색
- 상태 dropdown (`진행 중 / 전체 / 잠김 / 완료`)
- 상인 dropdown
- 지도 dropdown
- 왼쪽 Quest 목록 / 오른쪽 상세
- Wiki
- 진행 중 Quest `완료`
- 완료 Quest `완료 취소`
- 별도 `판정 문제 N` 진단 진입점
- 목표
- 제출 아이템
- 해금 조건
- 선행 Quest
- 판정 이유
- XP

UI는 `QuestCatalogEntry.Availability` 결과를 소비하며 Quest 조건을 다시 판정하지 않습니다.

현재 의도적으로 없음:

- 로그 기반 자동 완료
- objective 수동 진행 체크
- 추천 Quest 로직
- Quest 화면 내부 faction/edition 임시 토글

### Quest → Needed Items

- `giveItem`만 제출 requirement로 변환
- `findItem / collect`는 제출 합계에서 제외
- `sellItem`은 제출 합계에서 제외
- quest item은 일반 stash 집계에 자동 포함하지 않음
- 대체 가능한 제출 아이템은 한 항목을 임의 선택하지 않음
- FIR / Non-FIR 이중 계산 방지
- Quest/Hideout 출처 보존

### Ammo

실제 탄약은 `ItemPropertiesAmmo`로 식별합니다.

구현된 수급 관계:

- TraderPurchase
- TraderBarter
- HideoutCraft

---

## 실제 검증

### 온라인 데이터

2026-08-08 현재 실제 `json.tarkov.dev + editions-only overlay`로 세 모드 모두:

`live sources → canonical build → candidate.db → validation → content.db activation → read-back`

성공:

- regular
- pve
- pvp-season

### 최신 Windows CI

Profile management checkpoint:

- Windows Server 2025
- .NET SDK 10.0.302
- Desktop restore/build 성공
- **0 warnings / 0 errors**
- Core/Infrastructure/Application/Test build 성공
- **83 passed / 0 failed / 0 skipped**

회귀 테스트가 명시적으로 확인하는 항목:

- same game mode 중복 profile 생성 방지
- profile settings 수정 시 completed quests 보존
- profile settings 수정 시 hideout levels 보존
- profile settings 수정 시 inventory 보존
- Quest 완료/취소 후 후속 Quest 재계산
- `Indeterminate`가 Current에 섞이지 않음

---

## 기존 Tarkov-Helper에서 UI 참고한 범위

사용성만 참고:

- 적은 수의 상위 탭
- 검색 + ComboBox 필터
- 목록 / 우측 detail split view
- 상태 badge
- 명확한 주 행동 버튼
- section화된 detail
- Hideout +/- level control
- Ammo caliber dropdown + table

가져오지 않음:

- 기존 code-behind/service/event 구조
- 추천 Quest panel
- Kappa 특수 로직을 기본 흐름에 혼합
- objective 수동 체크를 핵심 진행도로 사용
- 의미가 모호한 `초기화`

---

## 아직 제품 결정/구현이 남은 것

- profile 삭제/reset UX
- Quest 실패/분기 상태를 사용자가 어떻게 기록할지
- Quest reward 전체 canonical model
- Item 자동 차감 여부
- Hideout Needed Items 기본 범위
- 대체 제출 아이템 자동 배분 여부
- 지도
- Scanner
- 기존 Tarkov-Helper 호환 migration

---

## 다음 순서

1. 현재 profile → Quest end-to-end 흐름을 계속 회귀 테스트로 보호
2. Quest에 남은 필수 정보가 실제 사용을 막는 수준인지 최소 점검
3. **Hideout 제품 상세 동작 설계/구현**
4. Hideout 상태 변경을 user.db와 연결
5. Quest + Hideout → Needed Items 실제 Desktop 흐름으로 확장
6. 그 후 Ammo 화면

새 기능은 현재 Core/Application/UI 경계를 깨지 않는 범위에서만 추가합니다.

## 마지막 갱신

2026-08-08 — 명시적 profile 생성/수정 UI와 `ProfileApplicationService`를 구현. 미입력 trader 상태를 0으로 추측하지 않으며, profile 설정 변경 시 완료 Quest·Hideout·Inventory를 보존하도록 고정. Desktop 빌드 0 warning/0 error, 전체 테스트 83/83 통과 후 main 반영. 다음 제품 영역은 Hideout.
