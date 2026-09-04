# SYSTEM DESIGN — 준현 헬퍼 독립 시스템 설계

이 문서는 기존 `Propeex/Tarkov-Helper`를 기반으로 리팩터링하지 않고, 현재 확정된 준현 헬퍼 제품 의도와 유지보수 철학에서 새로 도출한 시스템 설계입니다.

상태: `HISTORICAL DESIGN BASIS — NOT CURRENT IMPLEMENTATION AUTHORITY`

이 문서는 준현 헬퍼 초기 독립 설계의 근거를 보존하는 역사 기록이다. 현재 구현 구조·프로젝트 수·기술 경계는 `ARCHITECTURE.md`와 `DEVELOPER_REFERENCE.md`를 사용하며, 이 문서의 초기 계획이 현재 코드와 다를 경우 현재 authority를 우선한다.

관련 문서:

- `PRODUCT.md` — 무엇을 만들고 왜 만드는가
- `MAINTENANCE_PHILOSOPHY.md` — 복잡성을 통제하는 원칙
- `ARCHITECTURE.md` — 기술 구조의 상위 원칙
- `LEGACY_SALVAGE_AUDIT.md` — 기존 Tarkov-Helper에서 회수 가능한 경험/부품

---

# 1. 시스템 목표

준현 헬퍼의 핵심 데이터 기능은 다음 한 문장으로 설명할 수 있어야 합니다.

> 최신 Tarkov 데이터를 프로그램이 스스로 내려받아 준현 헬퍼 형식으로 변환하고, 별도로 저장한 사용자의 진행 상태와 결합해 퀘스트·은신처·필요 아이템·탄약 정보를 제공한다.

이 문장에 직접 필요하지 않은 구조는 기본적으로 추가하지 않습니다.

---

# 2. 최상위 경계

프로그램을 개념적으로 네 부분으로 나눕니다.

```text
[외부 Tarkov 데이터]
        │
        ▼
[1. Game Content]
다운로드 / 검증 / 변환 / 게임 DB
        │
        ├───────────────┐
        │               │
        ▼               ▼
[2. Domain Logic]   [3. User Progress]
퀘스트/은신처/      캐릭터 진행 상태
필요 아이템/탄약     완료/레벨/보유량
        │               │
        └───────┬───────┘
                ▼
          [4. Application/UI]
          조회 / 사용자 명령
```

각 경계의 책임은 겹치지 않게 합니다.

## 2.1 Game Content

게임 자체가 정하는 사실을 소유합니다.

- 아이템
- 퀘스트 정의와 조건
- 은신처 시설/레벨/요구사항
- 탄약 성능
- 상인/맵/기타 참조 데이터
- 탄약 판매/교환/제작 관계

이 영역은 온라인 데이터로 재생성 가능합니다.

## 2.2 User Progress

사용자의 실제 캐릭터 상태만 소유합니다.

- 독립 캐릭터 프로필
- 레벨
- 진영
- 에디션
- 프레스티지
- 필요한 상인 진행 값
- 완료한 퀘스트
- 은신처 현재 레벨
- 보유 아이템 수량

이 영역은 게임 콘텐츠 업데이트와 분리합니다.

## 2.3 Domain Logic

Game Content와 User Progress를 읽어 결과를 계산합니다.

- 현재 가능한 퀘스트
- 은신처의 다음/향후 요구사항
- 필요 아이템 집계
- 탄약 조회용 파생 정보

가능하면 순수 계산으로 유지하며 UI나 네트워크를 모르게 합니다.

## 2.4 Application/UI

사용자가 실제로 프로그램을 사용하는 경계입니다.

- 프로필 선택
- 진행 상태 수정
- 퀘스트 완료 처리
- 은신처 레벨 수정
- 보유 아이템 수정
- 퀘스트/필요 아이템/탄약 조회
- 데이터 업데이트 실행/상태 확인

UI는 계산 규칙이나 외부 API 형식을 직접 해석하지 않습니다.

---

# 3. 저장 데이터의 두 종류

준현 헬퍼는 저장 책임을 처음부터 분리합니다.

## 3.1 Game Content Store

특징:

- 온라인 데이터에서 재생성 가능
- 업데이트 실패 시 버려도 됨
- 사용자 고유 정보 없음
- 읽기 위주
- 콘텐츠 세트 단위로 검증/교체 가능

내용 예:

```text
Items
Quests
QuestConditions
QuestObjectives
QuestItemRequirements
Traders
Maps (참조용 데이터가 필요한 범위)
HideoutStations
HideoutLevels
HideoutRequirements
Ammo
AmmoAcquisitionSources
ContentMetadata
```

정확한 테이블/파일 구조는 저장 기술 결정 후 정하지만 의미 경계는 유지합니다.

## 3.2 User Progress Store

특징:

- 사용자가 만든 상태
- Game Content와 독립적인 수명주기
- 콘텐츠 DB 재구축/복구의 대상이 아님
- 캐릭터 프로필별로 상태 격리

내용 예:

```text
Profiles
ProfileCharacterState
CompletedQuests
HideoutProgress
ItemInventory
```

데이터베이스 엔진이나 파일 형식은 아직 미정입니다.

---

# 4. 식별자 원칙

게임 데이터와 사용자 진행 데이터를 안전하게 연결하려면 이름이 아니라 안정적인 원천 ID를 사용합니다.

예:

- Item ID
- Quest ID
- Trader ID
- Hideout Station ID

표시 이름은 번역/패치에 따라 바뀔 수 있으므로 식별자로 사용하지 않습니다.

퀘스트 목표처럼 원천 ID의 전역 유일성이 보장되지 않을 수 있는 데이터는 필요하면 부모 ID와 함께 복합 식별합니다.

예:

`QuestId + ObjectiveId`

---

# 5. 게임 데이터 업데이트 시스템

이 시스템은 준현 헬퍼의 기반 기능입니다.

## 5.1 책임

- 외부 원천에서 원본 다운로드
- 지원하는 형식인지 검증
- 내부 모델로 변환
- 데이터 관계 검증
- 새 콘텐츠 세트 생성
- 정상일 때만 활성 콘텐츠 교체
- 실패 시 기존 콘텐츠 유지

## 5.2 기본 흐름

```text
현재 정상 콘텐츠
      │
      ├──────────────────────────────┐
      │                              │
      ▼                              │
원본 API 다운로드                    │
      ▼                              │
Source Validator                     │
      ▼                              │
Importer                             │
      ▼                              │
Candidate Game Content               │
      ▼                              │
Content Validator                    │
      ▼                              │
정상? ── 아니오 ────────────────────┘
      │
      예
      ▼
새 콘텐츠 활성화
      ▼
기존 콘텐츠는 복구 후보로 보존 가능
```

## 5.3 Importer 경계

핵심 데이터 영역별로 외부 형식 해석 책임을 분리합니다.

- Quest Importer
- Hideout Importer
- Item Importer
- Ammo Importer

단, 이를 무조건 별도 거대 서비스 계층으로 만들겠다는 뜻은 아닙니다. 코드 단위는 실제 구현 언어/구조에 맞게 최소화하되 **외부 형식 해석 책임이 제품 계산 로직으로 새어 나오지 않게** 합니다.

## 5.4 검증 수준

### 형식 검증

- 필수 루트/필드 존재
- 필드 타입
- 지원하지 않는 구조 감지

### 의미 검증

예:

- Quest가 참조하는 Trader/Item이 존재하는가
- Hideout requirement가 유효한 Item을 참조하는가
- 같은 영구 ID가 충돌하지 않는가
- Ammo의 필수 성능값이 해석 가능한가

### 이상 변화 감지

예:

- 전체 퀘스트 수가 비정상적으로 급감
- 은신처 시설이 대량 소실
- 핵심 참조가 대량으로 끊김

단순 항목 수를 절대 규칙으로 고정하지 않고, 업데이트 실패를 조기에 발견하기 위한 방어 신호로 사용합니다.

## 5.5 콘텐츠 메타데이터

정상 콘텐츠 세트에는 최소한 다음 계열의 정보를 남깁니다.

- 생성 시각
- 원천
- 게임 모드/데이터 범위
- 데이터 개수 요약
- 콘텐츠 스키마 버전
- DB/콘텐츠 해시
- 경고 사항

이를 통해 현재 프로그램이 어떤 데이터로 동작하는지 추적할 수 있게 합니다.

---

# 6. 사용자 프로필 시스템

## 6.1 정의

프로필 하나는 실제 Tarkov의 독립 캐릭터 진행 상태 하나를 나타냅니다.

PvP/PvE/시즌 캐릭터는 서로 다른 프로필입니다.

## 6.2 책임

프로필 시스템은 사용자가 직접 소유하는 상태만 저장합니다.

현재 핵심 필드 후보:

- Profile ID
- 표시용 프로필 이름/게임 모드 식별
- 캐릭터 레벨
- 진영
- 게임 에디션
- Prestige
- 퀘스트 판정에 필요한 상인 진행 값
- 완료 Quest ID 집합
- 은신처 현재 레벨
- Item ID별 보유 수량

정확한 입력 필드는 실제 데이터 요구조건 검증 후 최소 범위로 정합니다.

## 6.3 하지 않는 일

프로필 시스템은 다음을 계산하지 않습니다.

- 어떤 퀘스트가 현재 가능한지
- 어떤 아이템이 필요한지
- 은신처 다음 업그레이드 요구량

프로필은 상태를 보관할 뿐입니다.

---

# 7. 퀘스트 시스템

## 7.1 사용자 목적

현재 선택한 캐릭터가 실제 게임에서 수행할 수 있는 퀘스트를 최대한 동일하게 확인하고, 게임에서 완료한 뒤 준현 헬퍼에서도 완료 처리합니다.

## 7.2 입력

Game Content:

- 퀘스트 정의
- 시작 조건
- 상인/맵
- 목표
- 요구 아이템
- Wiki/설명 등 정보

User Progress:

- 캐릭터 상태
- 완료 퀘스트
- 필요한 평판/Prestige/Edition 등

## 7.3 핵심 계산

```text
모든 Quest
   ↓
현재 프로필에 해당하는 데이터만 선택
   ↓
이미 완료? → 제외
   ↓
지원하는 해금 조건 검사
   ↓
충족 → Current Quest
미충족 → Locked Quest
```

준현 헬퍼는 수주 가능한 퀘스트를 이미 수락한 것으로 간주합니다.

따라서 핵심 상태는:

- Locked
- Current
- Completed

세 가지면 충분합니다.

Current/Locked는 기본적으로 저장하지 않고 계산합니다.
Completed만 사용자 진행 상태로 저장합니다.

## 7.4 조건 판정 설계

퀘스트 조건은 원본 API의 JSON 구조를 그대로 제품 로직에서 해석하지 않습니다.

Importer가 준현 헬퍼가 이해하는 조건으로 변환합니다.

지원 조건 예:

- Player Level
- Faction
- Required Quest State
- Trader Standing
- Trader Loyalty
- Edition
- Prestige
- 데이터 모드/전역 조건

시간 지연은 제품 결정에 따라 판정에서 제외합니다.

조건이 AND/OR 관계를 갖는 경우 그 관계는 내부 모델에서 보존해야 합니다.

새로운 조건 종류가 API에 추가되었는데 의미를 모르면 조용히 통과시키지 않고 데이터 업데이트 검증에서 경고 또는 실패 대상으로 다룹니다.

## 7.5 사용자 명령

핵심 명령은 최소화합니다.

- `CompleteQuest(profileId, questId)`

향후 정말 필요하면 완료 취소/실패 상태 등을 추가하되 현재 구조를 미리 복잡하게 만들지 않습니다.

## 7.6 출력

화면에 제공할 수 있는 결과:

- Current Quest 목록
- Locked/Completed 조회가 필요할 경우 해당 목록
- Trader/Map 분류 정보
- 퀘스트 상세
- Wiki 링크
- 요구 아이템

실제 시각 표현은 후속 UI 설계에서 결정합니다.

---

# 8. 은신처 시스템

## 8.1 사용자 목적

현재 실제 은신처 진행 상태를 준현 헬퍼에 반영하고, 앞으로 필요한 업그레이드 요구사항을 최신 데이터로 계산합니다.

## 8.2 입력

Game Content:

- 시설
- 레벨
- 각 레벨의 요구 아이템
- 시설 레벨 조건
- Trader/Skill 등 기타 조건

User Progress:

- 프로필별 현재 시설 레벨

## 8.3 사용자 상태

저장해야 할 핵심은 단순합니다.

`Profile + HideoutStationId → CurrentLevel`

업그레이드에 필요한 아이템 수량 같은 게임 사실을 사용자 DB에 복제하지 않습니다.

## 8.4 계산

현재 레벨을 기반으로 Game Content를 조회하여 다음을 계산할 수 있습니다.

- 다음 레벨 요구사항
- 앞으로 남은 레벨들의 요구사항
- 필요 아이템에 제공할 업그레이드 요구 항목

정확히 `다음 레벨만` 볼지 `최종 레벨까지` 볼지는 UI/제품 세부 설계에서 정합니다.

## 8.5 사용자 명령

핵심은:

- `SetHideoutLevel(profileId, stationId, level)`

이면 충분합니다.

은신처 시스템이 아이템 재고를 직접 차감하지 않습니다. 자동 차감 여부는 필요할 때 별도의 제품 결정으로 다룹니다.

---

# 9. 필요 아이템 시스템

## 9.1 성격

Need Items는 독립 게임 데이터 원천이 아니라 **파생 계산 기능**입니다.

## 9.2 입력

- Quest 시스템이 제공하는 관련 Item Requirement
- Hideout 시스템이 제공하는 관련 Item Requirement
- 사용자 Item Inventory

## 9.3 공통 요구사항 표현

퀘스트와 은신처가 같은 계산기에 데이터를 제공할 수 있도록 최소 공통 표현을 둡니다.

개념 예:

```text
ItemRequirement
- ItemId
- RequiredCount
- FoundInRaidRequired
- SourceType (Quest / Hideout)
- SourceId
- SourceLabel용 참조 정보
```

API별 세부 구조는 여기까지 새어 나오지 않습니다.

## 9.4 집계 원칙

같은 Item ID의 요구량을 합칠 수 있지만 **출처별 세부 항목은 잃지 않습니다.**

예:

```text
Bolts 총 12개 필요
- Quest A: 3
- Workbench 2→3: 6
- Heating 2→3: 3
```

사용자 진행이 바뀌면 원천 요구사항에서 다시 계산합니다.

## 9.5 FIR/일반 수량

FIR 요구는 별도로 보존합니다.

원칙:

- FIR 요구 수량은 FIR 보유량으로 충족해야 함
- FIR 초과 보유량은 일반 제한 없는 나머지 요구량도 충족할 수 있음
- 같은 FIR 아이템을 FIR 몫과 일반 몫에 이중 계산하지 않음

이 규칙은 기존 Tarkov-Helper에서 확인된 실제 실패 사례를 회귀 테스트로 가져올 가치가 있습니다.

## 9.6 사용자 상태

보유량 저장은 Item ID를 기준으로 합니다.

FIR 구분이 필요한 제품 범위에서는 최소한:

- FIR quantity
- unrestricted/non-FIR quantity

를 구분할 수 있어야 합니다.

정확한 입력 UX는 후속 설계입니다.

## 9.7 저장하지 않는 것

`현재 필요량 7개` 같은 결과값을 영구 상태로 저장하지 않습니다.

언제든 최신 게임 데이터 + 현재 프로필에서 다시 계산할 수 있어야 합니다.

---

# 10. 탄약 시스템

## 10.1 사용자 목적

최신 탄약 정보를 구경별로 빠르게 비교하고 실제 수급 방법을 확인합니다.

## 10.2 입력

Game Content만으로 대부분 동작합니다.

- Item/Ammo 정의
- 탄도/성능 속성
- Trader 판매 관계
- Barter 관계
- Craft 관계

## 10.3 내부 데이터

Ammo는 Item과 연결된 특수 데이터로 취급할 수 있습니다.

개념 예:

```text
AmmoDefinition
- ItemId
- Caliber
- Damage
- Penetration
- ArmorDamage
- Velocity
- Fragmentation
- Accuracy/Recoil modifiers
- 기타 실제 원천 속성
```

구체적인 필드는 최신 API가 제공하는 의미 있는 데이터와 실제 UI 필요성을 기준으로 확정합니다.

## 10.4 수급처

수급처는 사람이 작성한 고정 문자열이 아니라 원천 관계에서 파생합니다.

예:

- Trader direct purchase + loyalty requirement
- Trader barter + loyalty requirement
- Hideout craft + station level

수급처가 없다고 확인되는 경우에만 별도의 표현을 사용합니다.

## 10.5 사용자 진행과의 관계

기본 탄약 표는 프로필에 강하게 의존하지 않습니다.

향후 `내 현재 Trader LL에서 구할 수 있는가` 같은 편의 기능이 필요하면 Profile 상태를 읽을 수 있지만, 탄약의 기본 데이터 자체와 사용자 상태를 섞지는 않습니다.

## 10.6 파생 지표

준현 헬퍼가 별도의 효율 지표를 계산하게 된다면:

- 원천 사실과 명확히 구분
- 계산식을 문서화
- 사용자의 실제 요구가 있을 때만 추가

현재는 임의 휴리스틱을 만들지 않습니다.

---

# 11. 지도 시스템의 위치

지도는 제품 의도가 존재하지만 현재 핵심 데이터 설계 밖에 둡니다.

## 11.1 의존 방향

향후 지도는 다음 정보를 조회할 수 있습니다.

- Quest 정보/목표
- Item/Extract/기타 맵 정보
- 사용자 진행 상태

하지만 Quest/Hideout/Needed Items는 지도 시스템을 몰라야 합니다.

## 11.2 나중에 독립적으로 검증할 것

- 지도 이미지/벡터 원천
- 라이선스
- 좌표 데이터 원천
- Screenshot 위치 추적의 최신 유효성
- 좌표 변환/보정
- 층 감지
- Mini-map 실행 구조

기존 Tarkov-Helper의 지도 코드는 이 검증 후 참고할 수 있으나 설계 기준으로 사용하지 않습니다.

---

# 12. Scanner의 위치

RatScanner 계열 통합은 핵심 데이터 시스템 위의 입력/조회 도구로 봅니다.

예상 방향:

```text
게임 화면에서 Item 식별
        ↓
Item ID 결정
        ↓
기존 Item/Needed Items/Ammo 데이터 조회
        ↓
사용자에게 정보 표시
```

Scanner가 자체 아이템 DB, 자체 퀘스트 계산, 자체 필요 아이템 계산을 만들면 안 됩니다.

Scanner를 제거해도 핵심 기능은 그대로 동작해야 합니다.

---

# 13. Application 동작 단위

UI가 내부 저장소를 직접 건드리지 않도록 사용자 행동을 명확한 명령/조회로 표현합니다.

개념 예:

### 사용자 상태 변경

- CreateProfile
- SelectProfile
- UpdateCharacterState
- CompleteQuest
- SetHideoutLevel
- SetItemInventory

### 조회

- GetCurrentQuests
- GetQuestDetails
- GetHideoutProgress
- GetNeededItems
- GetAmmoByCaliber
- GetAmmoDetails

### 콘텐츠 관리

- GetContentStatus
- UpdateGameContent
- RestoreLastValidContent (필요한 경우)

구현 언어에서 반드시 이 이름/클래스로 만들겠다는 뜻은 아닙니다. **사용자 행동이 이 정도의 단순한 시스템 명령으로 귀결되어야 한다는 설계 기준**입니다.

---

# 14. 상태 변경 시 영향 범위

복잡한 이벤트 동기화 대신 어떤 원본 상태가 무엇을 바꾸는지만 명확히 합니다.

## 레벨 변경

- User Progress: 레벨 저장
- Quest 결과: 다음 조회/재계산 시 변경 가능
- Needed Items: Current Quest 범위 정책에 따라 다음 조회/재계산 시 변경 가능

다른 저장 데이터를 일괄 수정하지 않습니다.

## Quest 완료

- User Progress: CompletedQuest에 Quest ID 추가
- Quest 결과: 후속 퀘스트 재계산
- Needed Items: 퀘스트 요구량 재계산

Hideout 진행 자체를 수정하지 않습니다.

## Hideout 레벨 변경

- User Progress: 시설 레벨 변경
- Hideout 결과: 다음 요구사항 변경
- Needed Items: 은신처 요구량 재계산

Quest 상태를 수정하지 않습니다.

## Item 보유량 변경

- User Progress: inventory 변경
- Needed Items: 남은 수량 재계산

Quest/Hideout 진행을 자동 변경하지 않습니다.

## Game Content 업데이트

- Game Content만 새 콘텐츠로 교체
- User Progress는 그대로 유지
- 모든 파생 결과는 새 Game Content + 기존 User Progress로 다시 계산

이것이 시스템 단순성의 핵심입니다.

---

# 15. 최소 테스트 구조

구현 시 테스트도 시스템 경계를 그대로 따라갑니다.

## 15.1 Importer Fixture Test

작고 고정된 원본 JSON fixture를 넣어 내부 모델이 정확히 만들어지는지 검증합니다.

필수 사례:

- 기본 퀘스트
- 선행 조건 AND/OR
- 한국어/영어 번역
- 퀘스트 제출/획득 목표 구분
- Prestige/Edition/Faction
- 은신처 Item requirement
- 탄약 판매/교환/제작
- 중복/비정상 ID
- 지원하지 않는 새 필드/조건

## 15.2 Domain Test

외부 API 없이 내부 모델과 프로필만으로 계산합니다.

예:

- 레벨 전/후 퀘스트 해금
- 선행 Quest 완료 후 Current 목록 변화
- 프로필 격리
- Hideout level에 따른 요구 변경
- Quest + Hideout Item 집계
- FIR/일반 혼합 계산

## 15.3 Content Update Test

- 다운로드 실패
- 변환 실패
- 관계 검증 실패
- 중단된 staging
- 정상 콘텐츠 교체
- 실패 시 기존 콘텐츠 byte/상태 보존
- User Progress 비변경

## 15.4 Live Data Smoke

실제 최신 API를 사용해:

- 전체 변환 성공
- 참조 무결성
- 미지원 조건/목표 타입 발견
- 비정상 대량 감소
- 필수 데이터 누락

을 확인합니다.

Live smoke는 외부 서비스 장애 때문에 deterministic fixture 테스트와 분리합니다.

---

# 16. 기존 Tarkov-Helper 회수품 적용 규칙

현재 새 설계가 먼저 존재하므로 이후 회수는 다음과 같이 합니다.

## 직접 참고 가치가 높은 항목

- staging/validation/rollback 개념 → Content Update에 맞게 새로 구현
- deterministic/live smoke 분리 → 테스트 전략에 반영
- FIR 계산 실패 사례 → Needed Items 회귀 테스트에 반영
- Objective ID/번역 충돌 → Importer 식별자 테스트에 반영
- sell/find/collect와 실제 제출 requirement 혼동 → Quest Item Requirement 테스트에 반영
- 저장 reset race → User Progress 저장 구현 시 필요성 재검토
- Ammo 판매/교환/제작 관계 추출 → Ammo Importer 설계 자료

## 지금 가져오지 않는 항목

- 기존 Quest 상태 엔진
- 기존 UserData DB
- MainWindow/Page 구조
- 레거시 마이그레이션
- 지도 데이터/자산
- 임의 탄약 효율 계산
- Scanner 코드

새 설계에 구멍이 있을 때만 과거 구현을 다시 엽니다.

---

# 17. 현재 의도적으로 미정인 것

지금 결정하면 오히려 불필요하게 설계를 고정하므로 다음은 보류합니다.

- 언어/프레임워크
- SQLite 등 저장 기술
- ORM 사용 여부
- DI 프레임워크
- 자동 갱신 주기
- 오프라인 정책의 상세값
- 화면/탭 레이아웃
- Quest 완료 취소 UI
- 실패/분기 퀘스트 상세 UX
- Item 자동 차감
- Hideout에서 다음 레벨/전체 미래 요구 중 어떤 범위를 기본 표시할지
- 지도/Scanner 구현 방식

이 항목들은 실제 구현 또는 UX에 필요해지는 시점에 최소 범위로 결정합니다.

---

# 18. 구현 순서 제안

핵심을 가장 낮은 의존성부터 만들면 다음 순서가 적합합니다.

1. 실제 데이터 원천 검증
2. 내부 Game Content 모델 확정
3. Importer + Validator
4. Game Content 저장/안전 교체
5. User Progress 최소 모델
6. Quest 계산
7. Hideout 계산
8. Needed Items 계산
9. Ammo 조회/수급처
10. 핵심 UI
11. 지도
12. Scanner

지도와 Scanner는 핵심 진행 관리가 안정화되기 전까지 내부 구조에 영향을 주지 않습니다.

---

# 19. 설계 적합성 기준

준현 헬퍼 핵심 기능이 완성된 뒤에도 다음 설명이 사실이어야 합니다.

> 게임 데이터 DB는 지우고 다시 만들어도 사용자 진행이 살아 있다.
>
> 사용자 진행 하나를 바꾸면 관련 결과는 재계산될 뿐 여러 저장소를 동기화하지 않는다.
>
> UI를 바꿔도 게임 데이터 변환기는 바뀌지 않는다.
>
> API 형식이 바뀌면 주로 Importer/Validator만 영향을 받는다.
>
> 지도와 Scanner가 없어도 Quest/Hideout/Needed Items/Ammo가 완전하게 동작한다.

이 조건이 깨지기 시작하면 기능을 더 붙이기 전에 구조를 다시 단순화합니다.
