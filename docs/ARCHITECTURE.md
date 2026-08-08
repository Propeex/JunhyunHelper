# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

## 현재 상태

`CONFIRMED — Phase 2B desktop/core architecture implemented`

기술 스택:

- .NET 10
- C#
- WPF Desktop (`net10.0-windows`)
- SQLite (`Microsoft.Data.Sqlite` + bundled e_sqlite3)
- 별도 backend 없음
- runtime AI/GPT 없음

핵심 프로젝트 경계:

```text
JunhyunHelper.Core
JunhyunHelper.Infrastructure
JunhyunHelper.Application
JunhyunHelper.Desktop
```

## 1. 최상위 데이터 아키텍처

```text
온라인 Tarkov 데이터
→ 다운로드
→ 원본 형식/필수 의미 검증
→ canonical Game Content 변환
→ optional verified enrichment
→ candidate SQLite DB 작성
→ DB/read-back 검증
→ active content 원자적 교체
→ User Progress와 결합
→ Core 순수 규칙 계산
→ Application 조정
→ WPF Desktop 표시
```

목표는 패치로 데이터 값과 내용이 바뀌더라도 프로그램이 최신 데이터를 다시 받아 같은 변환 규칙으로 Game Content를 재구축하는 것입니다.

GPT/개발자는 importer를 설계할 때 필요할 수 있지만 일반적인 데이터 업데이트 실행에는 개입하지 않습니다.

## 2. 계층 책임

### Core

- canonical Game Content / User Progress 타입
- Quest 상태/미래 도달성
- Hideout 미래 upgrade 범위
- Needed Items / FIR / cleanup / flexible hand-in
- Ammo canonical 모델과 optional Armor Class effectiveness 값

금지:

- HTTP
- SQLite
- WPF
- 화면 상태

### Infrastructure

- `json.tarkov.dev` 및 허용된 보조 원천 읽기
- 외부 형식 → canonical model import
- optional Wiki Ballistics effectiveness enrichment
- 스키마/관계 검증
- `content.db` / `user.db` SQLite 저장
- candidate/previous/active 교체 및 복구
- 실제 update progress 보고

Infrastructure는 외부 스키마 불확실성을 Core나 Desktop에 누출하지 않습니다.

### Application

- 사용자 명령 저장
- Core 결과 재계산
- Quest / Hideout / Items workspace
- Profile CRUD orchestration

파생 결과를 별도 사실로 저장하지 않습니다.

### Desktop

- WPF 표시
- 사용자 입력
- Application 호출
- Core/Application 결과 표시
- Infrastructure update progress 표시
- 비권위 image cache
- canonical ID를 변경하지 않는 filter order/grouping

Desktop은 Quest/Needed Items/Ammo source parsing 규칙을 다시 구현하지 않습니다.

## 3. Game Content와 User Progress 분리

기본 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
```

### User Progress

```text
user.db
```

사실로 저장:

- profile / game mode
- level / faction / edition / prestige
- trader LL / 필요한 standing
- completed quest IDs
- explicit permanent failed quest IDs
- hideout station level
- FIR / Non-FIR inventory

저장하지 않음:

- Quest Current/Locked/Unavailable
- Needed Items
- cleanup
- flexible hand-in progress
- next hideout upgrade

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

Game Content update 실패는 `user.db`를 변경하지 않습니다.

## 4. 외부 데이터와 canonical model 경계

제품 로직과 UI는 외부 API 원본 JSON/HTML을 직접 사용하지 않습니다.

현재 import/enrichment 영역:

- Quest
- Hideout
- Item
- Trader
- Map 최소 메타데이터
- Barter / Craft
- Ammo raw facts
- edition-only rule source
- Wiki Ballistics Armor Class 1~6 effectiveness

내용 변화는 importer가 이해할 수 있으면 흡수합니다.

핵심 필드/의미 변화로 안전하게 해석할 수 없으면 해당 source를 실패 처리하고, source 역할에 따라 전체 candidate를 거부하거나 optional enrichment만 unknown으로 둡니다.

## 5. 데이터 원천

### Primary

`json.tarkov.dev`

- tasks
- hideout
- items
- traders
- maps
- barters
- crafts
- Ammo raw stats

GameMode:

- regular
- pve
- pvp-season

### Auxiliary — editions

TarkovTracker `tarkov-data-overlay`의 editions 정보만 허용합니다.

전체 community correction overlay는 적용하지 않습니다.

### Auxiliary — Ammo Armor Class effectiveness

Escape from Tarkov Wiki `Ballistics`의 명시적 Class 1~6 0~6 값만 optional enrichment로 사용합니다.

흐름:

```text
MediaWiki Action API action=parse
→ rendered Ballistics table
→ row/cell extraction
→ rightmost Class 1~6 값 검증(각 0..6)
→ canonical GameItem.NameEn 정규화
→ 정확히 하나의 Ammo와 매칭
→ AmmoDefinition.ArmorEffectiveness
```

안전 규칙:

- raw Ammo fact는 계속 tarkov.dev가 기준
- 자체 ratio/threshold heuristic 금지
- canonical 영문명이 중복이면 매칭 제외
- 같은 Ammo에 서로 다른 Wiki rating이 연결되면 제외
- unmatched/ambiguous = null
- 전체 matching이 비정상적으로 적으면 Wiki schema 변화로 보고 그 build의 모든 effectiveness 적용 거부
- Wiki HTTP/parse 실패는 core Game Content update를 실패시키지 않음
- optional Wiki 요청은 20초 timeout

현재 snapshot은 `GameContentCatalog` JSON 전체를 SQLite에 저장하므로 `AmmoDefinition`의 optional init property가 그대로 round-trip됩니다. 기존 snapshot에는 값이 없어도 `null`로 호환됩니다.

근거: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

## 6. Game Content update progress

Infrastructure contract:

```text
ContentUpdateProgress
- Stage
- Message
- Percent
- CompletedUnits?
- TotalUnits?
```

현재 source 다운로드 단위:

### primary 8

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

### optional 1

```text
Wiki Ballistics effectiveness
```

Desktop production build에서는 총 9 source task가 실제 완료될 때 완료 수를 증가시킵니다. Wiki가 unavailable로 정상 fallback되어도 source task 자체는 완료된 것으로 간주합니다.

이후 실제 pipeline 단계:

```text
Preparing
Downloading
Importing
Validating
WritingCandidate
Activating
Completed / Failed
```

Desktop은 `IProgress<ContentUpdateProgress>`만 표시합니다. Infrastructure는 WPF를 참조하지 않습니다.

## 7. Hideout 진행 표현

제품 의미상 `미입력`과 `Lv.0`은 같습니다.

- row 없음 = Lv.0
- 모든 0을 명시 저장할 필요 없음
- UI `- / 현재 레벨 / +`
- Needed Items는 현재 레벨 이후 모든 미래 upgrade material 포함

nullable boundary가 남아 있어도 호환 경계일 뿐 별도 제품 상태가 아닙니다.

## 8. Desktop 이미지 cache

Item / Hideout / Ammo 이미지는 canonical Game Content URL을 재사용합니다.

```text
%LocalAppData%/JunhyunHelper/image-cache
```

- stable ID + source URL hash key
- URL 변경 시 새 entry
- 최대 6개 병렬 다운로드
- 이미지당 최대 8 MiB
- `BitmapImage` memory load + freeze
- 네트워크/디코딩 실패 non-fatal
- invalid cached payload는 삭제 후 다음 요청에서 재다운로드

image cache는 권위 데이터가 아닙니다.

## 9. Desktop 데이터 흐름

### Profile 선택

```text
Profile selector
→ ProfileApplicationService
→ user.db
→ 해당 GameMode active content 읽기/복구
→ Quest/Hideout/Items workspace
→ UI
```

### Quest/Hideout/Inventory 변경

```text
사용자 입력
→ Application service fact 저장
→ workspace 재계산
→ Needed/Cleanup 변화
→ UI
```

### Ammo

Ammo는 active Game Content read-only 비교 화면이며 User Progress와 결합하지 않습니다.

## 10. Item 화면

행:

- 아이콘
- 이름
- 필요 출처 요약
- 미래 필요량
- 보유량
- 추가 필요 / 충분 / 정리 / 판단 보류

상세:

- FIR 최소 요구
- FIR / Non-FIR 보유 입력
- 전체 출처
- cleanup 보호 이유
- 해당 Item이 후보일 때 flexible hand-in 그룹

미보유 flexible candidate도 목록에서 접근 가능해야 합니다.

## 11. Ammo effectiveness UI

Desktop은 canonical `AmmoDefinition.ArmorEffectiveness`만 표시합니다.

- 표: Class 1~6 여섯 colored cells
- 상세: 표 열을 숨겨도 항상 여섯 cells 표시
- 숫자 0~6을 항상 표시
- 색상은 보조 신호
- null/invalid는 중립색 `?`

Desktop은 값을 계산하거나 Wiki를 직접 읽지 않습니다.

## 12. Quest reference UI order/grouping

Trader/Map dropdown 순서는 Desktop presentation policy입니다.

`UiReferenceOrder` 한 곳에서 관리합니다.

- canonical ID 변경 금지
- 원본 data order 변경 금지
- 알려진 값만 fixed rank
- unknown은 제거하지 않고 뒤에 fallback

Ground Zero / Ground Zero 21+는 canonical ID를 유지하고 Quest filter key에서만 `group:groundzero`로 묶습니다.

## 13. Map / Scanner placeholder

상단 navigation에 `지도`, `스캐너`가 있습니다.

실제 기능은 아직 없으며 `준비 중` placeholder만 표시합니다.

- Map source를 임의 확정하지 않음
- Scanner 인식 방식을 임의 구현하지 않음
- 숨은 runtime 시작하지 않음

## 14. Content activation 안전성

1. candidate 작성
2. 스키마/관계/행 수 검증
3. candidate read-back 의미 검증
4. 성공 후 active 교체
5. previous 보존
6. 실패 시 기존 active + user.db 유지

잘못된 새 데이터로 정상 데이터를 덮는 것보다 update 실패가 낫습니다.

## 15. 의도적으로 사용하지 않는 구조

- ORM / EF Core
- DI container
- 별도 backend
- 범용 rule engine
- runtime AI/GPT
- 거대한 event bus
- 기능별 중복 DB
- 외부 API JSON/HTML의 UI 직접 소비
- 검증되지 않은 Ammo effectiveness heuristic

## 16. 후속 기술 과제

- 첫 실사용 피드백 통합 Windows build 회귀/실사용 검증
- Map 실제 기능 데이터 공급원/레이어 모델 결정
- Scanner 실제 기능 요구사항/인식 경계 확정

새 외부 source를 추가할 때도 source 역할, failure semantics, canonical mapping 경계를 먼저 정의합니다.
