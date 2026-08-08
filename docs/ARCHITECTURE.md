# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

## 현재 상태

`CONFIRMED — Phase 2B desktop/core architecture implemented`

현재 기술 스택:

- .NET 10
- C#
- WPF Desktop (`net10.0-windows`)
- SQLite (`Microsoft.Data.Sqlite` + bundled e_sqlite3)
- 별도 backend 없음
- runtime AI/GPT 없음

솔루션의 핵심 프로젝트 경계:

```text
JunhyunHelper.Core
JunhyunHelper.Infrastructure
JunhyunHelper.Application
JunhyunHelper.Desktop
```

## 1. 최상위 데이터 아키텍처

`CONFIRMED`

```text
온라인 Tarkov 데이터
→ 다운로드
→ 원본 형식/필수 의미 검증
→ canonical Game Content 변환
→ candidate SQLite DB 작성
→ DB/read-back 검증
→ active content 원자적 교체
→ User Progress와 결합
→ Core 순수 규칙 계산
→ Application 조정
→ WPF Desktop 표시
```

핵심 목적은 게임 패치로 데이터의 값과 내용이 바뀌더라도 프로그램 자체가 최신 데이터를 다시 받아 내부 Game Content를 재구축하는 것입니다.

GPT/개발자는 importer와 변환 규칙을 설계할 때 필요할 수 있지만, 일반적인 데이터 업데이트 실행에는 GPT가 개입하지 않습니다.

## 2. 계층 책임

### Core

책임:

- canonical Game Content / User Progress 타입
- Quest 상태/미래 도달성 계산
- Hideout 미래 upgrade 범위 계산
- Needed Items / FIR / cleanup / flexible hand-in 계산
- Ammo canonical 모델

금지:

- HTTP
- SQLite
- WPF
- 화면 상태

### Infrastructure

책임:

- `json.tarkov.dev` 및 허용된 보조 원천 읽기
- 외부 JSON → canonical 모델 import
- 스키마/관계 검증
- `content.db` / `user.db` SQLite 저장
- candidate/previous/active content 교체 및 복구
- 실제 Game Content update 단계 진행 상황 보고

Infrastructure는 외부 형식의 불확실성을 Core로 누출하지 않습니다.

### Application

책임:

- 사용자 명령 1건을 저장하고 Core 결과를 다시 계산하는 얇은 orchestration
- Quest / Hideout / Items workspace 구성
- Profile CRUD 흐름 조정

파생 결과를 별도의 사실처럼 저장하지 않습니다.

### Desktop

책임:

- WPF 표시
- 사용자 입력 수집
- Application 명령 호출
- Core/Application 계산 결과 표시
- Infrastructure update progress 표시
- 사용자 편의를 위한 비권위적 UI cache
- canonical ID를 변경하지 않는 filter ordering/grouping

Desktop은 Quest/Needed Items 규칙을 다시 구현하지 않습니다.

## 3. Game Content와 User Progress 분리

`CONFIRMED`

기본 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
```

### User Progress

```text
user.db
```

저장하는 사용자 사실:

- profile / game mode
- level / faction / edition / prestige
- trader LL / 필요한 standing
- completed quest IDs
- 필요한 경우 explicit permanent failed quest IDs
- hideout station level
- FIR / Non-FIR inventory

저장하지 않는 파생 결과:

- Quest Current/Locked/Unavailable
- Needed Items
- cleanup
- flexible hand-in progress
- next hideout upgrade

### Game Content

모드별 독립 저장:

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

Game Content 업데이트 실패는 `user.db`를 변경하지 않습니다.

## 4. 외부 데이터와 canonical 모델의 경계

`CONFIRMED`

제품 로직과 UI는 외부 API 원본 JSON 구조를 직접 사용하지 않습니다.

현재 주요 importer 영역:

- Quest
- Hideout
- Item
- Trader
- Map 최소 메타데이터
- Barter / Craft
- Ammo
- edition-only rule source

내용 변화는 기존 importer가 이해할 수 있으면 자동 흡수합니다.

핵심 필드 삭제/타입 변경/의미 변경처럼 안전하게 이해할 수 없는 변화가 발생하면 candidate build를 실패시키고 마지막 정상 active DB를 유지합니다.

## 5. 데이터 원천

### 1차 원천

`json.tarkov.dev`

현재 사용하는 주요 영역:

- tasks
- hideout
- items
- traders
- maps — 현재 Quest 분류/참조용 최소 정보
- barters
- crafts

지원 GameMode:

- regular
- pve
- pvp-season

### 보조 원천

TarkovTracker `tarkov-data-overlay`의 editions 정보만 허용합니다.

전체 community correction overlay를 자동 적용하지 않습니다.

## 6. Game Content update progress

`CONFIRMED / IMPLEMENTED`

진행률은 UI 타이머가 임의로 증가시키지 않습니다.

Infrastructure에 typed contract를 둡니다.

```text
ContentUpdateProgress
- Stage
- Message
- Percent
- CompletedUnits?
- TotalUnits?
```

현재 실제 source 다운로드 단위는 8개입니다.

```text
items
traders
maps
tasks
hideout
barters
crafts
edition rules
```

각 source task가 실제로 끝날 때 `Interlocked` 완료 수를 증가시키고 1/8~8/8 상태를 보고합니다.

이후 실제 pipeline 경계를 보고합니다.

```text
Preparing
Downloading
Importing
Validating
WritingCandidate
Activating
Completed / Failed
```

Desktop은 `IProgress<ContentUpdateProgress>`를 받아 overlay progress bar와 stage message만 표시합니다.

Infrastructure는 WPF를 참조하지 않습니다.

최초 content 생성, active 복구 update, 사용자 수동 update는 모두 같은 progress 경로를 사용합니다.

## 7. Hideout 진행 표현

`CONFIRMED`

제품 의미상 `미입력`과 `Lv.0`을 구분하지 않습니다.

- 저장된 row가 없으면 Lv.0으로 계산
- Lv.0을 굳이 `user.db`에 모두 저장하지 않아도 됨
- UI는 `- / 현재 레벨 / +` 조작으로 정수 단계 변경
- Needed Items는 현재 레벨보다 높은 모든 미래 upgrade material을 포함

nullable boundary가 일부 남아 있더라도 호환 경계일 뿐 별도 제품 상태가 아닙니다.

## 8. Desktop 이미지 cache

`CONFIRMED`

Item / Hideout / Ammo 이미지는 canonical Game Content의 URL을 재사용합니다.

새로운 별도 이미지 데이터 원천을 만들지 않습니다.

Desktop 전용 cache:

```text
%LocalAppData%/JunhyunHelper/image-cache
```

구조 원칙:

- stable entity ID + source URL hash로 cache key 생성
- source URL이 바뀌면 새 cache entry 사용
- 최대 6개 병렬 다운로드
- 이미지 1개 최대 8 MiB
- WPF `BitmapImage`는 메모리 로드 후 freeze
- 네트워크/디코딩 실패는 non-fatal
- 잘못된 payload가 cache에 저장되어 디코딩에 실패하면 해당 cache 파일을 제거하여 다음 요청에서 재다운로드 가능

중요:

- 이미지 실패는 Game Content update 실패가 아님
- 이미지 실패는 User Progress에 영향을 주지 않음
- 이미지 cache는 권위 데이터가 아니므로 언제든 재생성 가능

## 9. Desktop 화면 데이터 흐름

### Profile 선택

```text
Profile selector
→ ProfileApplicationService
→ user.db profile fact
→ 해당 GameMode active content 읽기/복구
→ Quest/Hideout/Items workspace 계산
→ 화면 갱신
```

### Quest/Hideout/Inventory 변경

```text
사용자 입력
→ Application service에 fact 저장
→ workspace 재계산
→ Needed/Cleanup 변화 계산
→ Quest/Hideout/Item UI 갱신
```

### Ammo

Ammo는 선택된 GameMode active Game Content의 읽기 전용 비교 화면입니다.

사용자 진행 상태와 결합하지 않습니다.

## 10. Item 화면 구조

`CONFIRMED`

Item 화면의 1차 목록은 진단 dump가 아니라 사용자가 빠르게 판단할 수 있는 행 구조입니다.

행의 핵심 정보:

- 아이콘
- 이름
- 필요 출처 요약
- 미래 필요량
- 보유량
- `추가 필요 / 충분 / 정리 / 판단 보류` 상태

상세:

- FIR 최소 요구
- FIR / Non-FIR 보유 입력
- 전체 출처
- cleanup 보호 이유
- 해당 아이템이 후보인 경우에만 flexible hand-in 그룹 정보

flexible hand-in 후보는 아직 하나도 보유하지 않았더라도 모두 목록에서 접근 가능해야 합니다. 그래야 사용자가 첫 보유량을 입력할 수 있습니다.

## 11. Hideout / Ammo 이미지 표시

Hideout:

- 시설 목록 행에 station image
- 상세 header에 station image

Ammo:

- 표의 탄약 이름 영역에 item image
- 상세 header에 같은 image

이미지는 canonical `ImageUrl` / `IconUrl`만 사용합니다.

## 12. Quest reference UI ordering / grouping

`CONFIRMED / IMPLEMENTED`

Trader/Map dropdown 순서는 canonical 데이터 의미가 아니라 사용자 표시 정책입니다.

따라서 Desktop의 `UiReferenceOrder` 한 곳에서 관리합니다.

원칙:

- canonical trader/map ID 변경 금지
- Game Content 원본 순서 변경 금지
- 알려진 trader/map만 고정 UI rank 적용
- 미래 unknown 값은 제거하지 않고 알려진 값 뒤에 display-name fallback

### Trader

현재 고정 순서:

```text
Prapor
Therapist
Fence
Skier
Peacekeeper
Mechanic
Ragman
Jaeger
Ref
Lightkeeper
BTR Driver
```

### Ground Zero variants

Ground Zero와 Ground Zero 21+는 Game Content에서 별도 ID를 유지합니다.

Quest map filter key 생성에서만 둘을 `group:groundzero`로 정규화합니다.

```text
canonical quest.MapId
→ MapReference
→ Desktop MapFilterKey
→ Ground Zero variants only: group:groundzero
```

이 때문에 filter는 하나이지만 원본 데이터/Quest 관계는 손실되지 않습니다.

## 13. Map / Scanner placeholder

`CONFIRMED / IMPLEMENTED`

상단 navigation에는 `지도`, `스캐너`가 존재합니다.

실제 기능은 아직 구현하지 않습니다.

각 section은 `준비 중` placeholder이며:

- Map 데이터 공급원을 임의 확정하지 않음
- Scanner 인식 방법을 임의 구현하지 않음
- 숨은 background runtime을 시작하지 않음

나중에 기능이 확정되면 동일 section을 실제 page로 교체합니다.

## 14. 업데이트 안전성

Game Content 교체는 다음 순서를 지킵니다.

1. 새 데이터를 candidate 영역에 작성
2. 스키마/필수 관계/행 수 등 검증
3. candidate를 다시 읽어 canonical 의미 검증
4. 검증 성공 후에만 active 교체
5. 이전 active를 복구 가능하게 보존
6. 실패 시 기존 active와 user.db 유지

잘못된 새 데이터로 기존 정상 데이터를 덮어쓰는 것보다 업데이트 실패가 낫습니다.

## 15. 의도적으로 사용하지 않는 구조

현재 필요성이 증명되지 않아 사용하지 않습니다.

- ORM / EF Core
- DI container
- 별도 backend
- 범용 rule engine
- runtime AI/GPT
- 거대한 event bus
- 기능별 중복 데이터베이스
- 외부 API JSON을 UI에서 직접 소비

## 16. 후속 기술 과제

현재 남은 주요 과제:

- Wiki-equivalent Armor Class 1~6 0~6 rating의 정확한 source/derivation 검증
- Map 실제 기능용 데이터 공급원/레이어 모델 결정
- Scanner 실제 기능 요구사항/인식 경계 확정
- 첫 실사용 피드백 통합 Windows build 회귀 검증

Armor Class rating은 검증되지 않은 자체 휴리스틱을 만들지 않습니다. 조사 기준은 `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`입니다.
