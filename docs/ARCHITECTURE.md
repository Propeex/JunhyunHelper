# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

## 현재 상태

`CONFIRMED — Phase 2B desktop/core architecture implemented, usability iteration ongoing`

기술 스택:

- .NET 10
- C#
- WPF Desktop (`net10.0-windows`)
- SQLite (`Microsoft.Data.Sqlite` + bundled e_sqlite3)
- SkiaSharp — Desktop 이미지 decode/PNG normalize 전용
- 별도 backend 없음
- runtime AI/GPT 없음

솔루션 경계:

```text
JunhyunHelper.Core
JunhyunHelper.Infrastructure
JunhyunHelper.Application
JunhyunHelper.Desktop
```

## 1. 최상위 데이터 아키텍처

```text
온라인 Tarkov 데이터
→ source download
→ 외부 schema/필수 의미 검증
→ canonical Game Content import
→ candidate SQLite DB
→ read-back / 관계 검증
→ active content 원자적 교체
→ User Progress와 결합
→ Core 순수 계산
→ Application 제품 정책/orchestration
→ WPF Desktop 표시
```

게임 패치로 데이터 값이 바뀌더라도 외부 형식이 importer가 이해하는 범위라면 프로그램이 자체적으로 최신 Game Content를 재구축하는 것이 핵심입니다.

## 2. 계층 책임

### Core

책임:

- canonical Game Content / User Progress 타입
- Quest availability의 사실 기반 계산
- Quest failure/future reachability
- Hideout 미래 upgrade 범위
- Needed Items / FIR / cleanup / flexible hand-in 계산
- Ammo canonical 모델

금지:

- HTTP
- SQLite
- WPF
- 화면 상태
- 사용자 표시 편의를 위한 임의 정책

### Infrastructure

책임:

- `json.tarkov.dev` 및 허용된 보조 원천 읽기
- 외부 JSON → canonical model import
- Item category ID와 category metadata 연결
- 스키마/관계 검증
- `content.db` / `user.db` SQLite 저장
- candidate/previous/active 교체 및 복구
- 실제 Game Content update 진행 상황 보고

Infrastructure는 외부 형식의 불확실성을 UI에 직접 노출하지 않습니다.

### Application

책임:

- 사용자 명령 단위 orchestration
- Quest / Hideout / Items workspace 구성
- Profile CRUD
- Core 진단 사실을 보존하면서 확정된 **제품 정책** 적용

현재 중요한 제품 정책:

```text
Core QuestAvailabilityState.Indeterminate
→ Application에서 Current로 표시 승격
→ diagnostic reasons는 유지
```

확정된 Locked/Unavailable은 변경하지 않습니다.

파생 결과를 별도 사실로 저장하지 않습니다.

### Desktop

책임:

- WPF 표시/입력
- Application 명령 호출
- update progress 표시
- canonical ID 기반 UI navigation/grouping
- 비권위 이미지 cache
- Tarkov category metadata를 사용한 표시용 상위 Item 분류

Desktop은 Quest/Needed Items의 핵심 계산 규칙을 다시 구현하지 않습니다.

## 3. Game Content와 User Progress 분리

기본 root:

```text
%LocalAppData%/JunhyunHelper
```

### User Progress

```text
user.db
```

저장 사실:

- profile / game mode
- level / faction / edition / prestige
- trader LL / 필요한 standing
- completed quest IDs
- 필요한 explicit permanent failed quest IDs
- hideout station level
- FIR / Non-FIR inventory

### Game Content

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

Game Content update 실패 또는 schema 재구축은 `user.db`를 변경하지 않습니다.

## 4. Content snapshot schema

현재 schema: **v2**

v2 추가 의미:

- canonical `GameItem`에 원본 category IDs와 normalized category metadata 보존

이 데이터는 Item 종류 UI를 매 패치의 최신 API category에 맞춰 다시 만들기 위해 필요합니다.

v1 snapshot은 v2 build에서 호환되지 않는 것으로 감지하여 online source에서 재구축합니다.

User Progress migration은 필요하지 않습니다.

## 5. 외부 데이터와 canonical model 경계

제품 로직과 UI는 외부 raw JSON을 직접 사용하지 않습니다.

주요 importer:

- Quest
- Hideout
- Item
- Item category metadata
- Trader
- Map 최소 메타데이터
- Barter / Craft
- Ammo
- edition-only source

### Item category

현재 json.tarkov.dev 관계:

```text
item.categories[]
  → category ID
itemCategories[id]
  → id / name / normalizedName
```

Importer는 ID와 normalized key를 `GameItem`에 보존합니다.

Desktop은 이를 다음과 같은 상위 표시 그룹으로 정규화합니다.

```text
Weapons / Weapon Parts / Gear / Ammo / Medical / Provisions
Barter / Keys / Info / Special / Quest / Money / Maps / Other
```

알 수 없는 category는 삭제하지 않고 Other fallback으로 보존합니다.

## 6. Game Content update progress

Infrastructure typed contract:

```text
ContentUpdateProgress
- Stage
- Message
- Percent
- CompletedUnits?
- TotalUnits?
```

실제 다운로드 source와 pipeline 경계에 따라 보고합니다.

```text
Preparing
Downloading
Importing
Validating
WritingCandidate
Activating
Completed / Failed
```

UI timer로 가짜 progress를 만들지 않습니다.

## 7. Quest availability 제품 경계

Core는 가능한 한 실제 조건을 엄격하게 판정합니다.

정상 결과:

- Current
- Locked
- Unavailable
- Completed

진단 결과:

- Indeterminate + reasons

2차 실사용 결정:

```text
if Core == Indeterminate:
    Desktop에 전달하는 Application workspace에서는 Current
    reasons는 그대로 보존
```

목적:

- 시스템으로 끝까지 표현할 수 없는 특수 조건 때문에 실제 진행 가능한 Quest가 UI에서 빠지지 않게 함
- 동시에 원인 진단 정보는 잃지 않음

## 8. Needed Items / flexible hand-in

Core 계산은 기존 원칙을 유지합니다.

- future-reachable Quest
- current/future Hideout upgrade
- FIR 요구
- cleanup 보호
- alternative/flexible 제출 그룹

Desktop 표시만 분리합니다.

### 일반 Item view

- 확정 필요량
- 실제 inventory
- cleanup/deferred 상태
- fixed requirement와 관련된 flexible candidate

### Flexible view

- 모든 flexible group 후보에 접근 가능
- 아직 inventory 0이어도 입력 경로 유지

`flexible-only` 후보를 일반 목록에서 제거하는 것은 UI 밀도 개선이며 Core 계산 의미를 변경하지 않습니다.

## 9. Quest ↔ Item navigation

navigation identity는 표시 이름이 아니라 canonical ID입니다.

```text
Quest required Item
→ ItemId
→ Item page

Item requirement source
→ QuestId
→ Quest page

Quest prerequisite
→ RequiredQuestId
→ Quest page
```

Quest에서 누른 Item이 현재 Needed 목록에 없더라도 canonical Item이 존재하면 Desktop이 reference-only row를 임시 구성하여 상세를 열 수 있습니다.

이 row는 새로운 User Progress 사실이 아닙니다.

## 10. Desktop 이미지 pipeline

권위 원천:

- canonical `GameItem.IconUrl`
- canonical `HideoutStation.ImageUrl`

문제:

- source asset에 WebP 등이 존재
- WPF `BitmapImage` 직접 decode는 Windows codec 환경에 따라 실패 가능

현재 pipeline:

```text
canonical URL
→ HttpClient download
→ byte/size limit
→ SkiaSharp SKCodec decode
→ pixel dimension validation
→ PNG encode
→ %LocalAppData%/JunhyunHelper/image-cache
→ WPF BitmapImage
```

cache key:

```text
stable entity ID + source URL hash
```

원칙:

- source URL 변경 시 새 cache entry
- cache는 비권위 데이터
- network/decode 실패 non-fatal
- invalid cache entry 삭제 후 재시도 가능
- Item / Hideout / Ammo / Quest Item이 동일 cache service 사용

## 11. ScrollBar theme

WPF native ScrollBar 부분 스타일링은 사용하지 않습니다.

Desktop merged resources에서 별도 `DarkScrollBars.xaml`을 적용합니다.

- vertical/horizontal 전체 template
- native arrow chrome 없음
- rounded track/thumb
- hover/drag accent

## 12. Desktop 화면 데이터 흐름

### Profile 선택

```text
Profile selector
→ ProfileApplicationService
→ user.db
→ 해당 GameMode active content read/recover
→ Quest / Hideout / Items workspace
→ Ammo read-only view
→ Desktop
```

### Quest / Hideout / Inventory 변경

```text
사용자 입력
→ Application service
→ user.db fact 저장
→ Core 재계산
→ workspace refresh
→ Needed/Cleanup 변화 표시
```

### Content update

```text
Update command
→ online build
→ candidate validation
→ active swap
→ workspace recalculation
```

## 13. Ammo

Ammo는 선택 GameMode Game Content의 read-only 비교 화면입니다.

- User Progress와 결합하지 않음
- canonical item icon 사용
- acquisition source 구조화
- Wiki Armor effectiveness는 optional enrichment
- 임의 effectiveness heuristic 금지

상세: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

## 14. Map / Scanner

상단 navigation에는 `지도`, `스캐너`가 존재합니다.

현재는 `준비 중` placeholder입니다.

- Map 공급원을 임의 확정하지 않음
- Scanner 인식 방법을 임의 구현하지 않음
- 숨은 background runtime 없음

## 15. 업데이트 안전성

1. 새 데이터를 candidate에 작성
2. schema/관계/필수 의미 검증
3. candidate read-back
4. 성공 후 active 교체
5. 이전 active 복구 가능하게 보존
6. 실패 시 기존 active + user.db 유지

잘못된 새 데이터를 적용하는 것보다 update 실패가 낫습니다.

## 16. 의도적으로 사용하지 않는 구조

현재 필요성이 증명되지 않아 사용하지 않습니다.

- EF Core / 범용 ORM
- DI container
- 별도 backend
- 범용 rule engine
- runtime AI/GPT
- 거대한 event bus
- 외부 API raw JSON을 UI에서 직접 소비

## 17. 관련 문서

- `docs/PRODUCT.md`
- `docs/STATE.md`
- `docs/DECISIONS.md`
- `docs/SECOND_USABILITY_PASS.md`
- `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`
