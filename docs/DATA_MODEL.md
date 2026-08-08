# DATA MODEL — 준현 헬퍼 내부 데이터 모델

이 문서는 외부 Tarkov 데이터를 준현 헬퍼가 어떤 의미로 저장하고 사용하는지 정의합니다.

상태: `CONFIRMED — 큰 틀 / 일부 원천 필드는 구현 전 라이브 fixture로 최종 검증`

핵심 원칙:

> 외부 API의 구조를 복사한 데이터베이스를 만들지 않는다. 준현 헬퍼의 기능이 실제로 필요로 하는 의미만 내부 모델로 변환한다.

관련 문서:

- `PRODUCT.md`
- `SYSTEM_DESIGN.md`
- `MAINTENANCE_PHILOSOPHY.md`
- `DATA_VALIDATION.md`

---

# 1. 데이터의 세 종류

준현 헬퍼에서 데이터는 세 종류로 명확히 구분합니다.

## 1.1 Game Content — 게임이 정하는 사실

온라인 원천에서 받아 다시 만들 수 있는 데이터입니다.

예:

- 아이템 정의
- 퀘스트 정의와 조건
- 은신처 시설/레벨/요구 재료
- 탄약 성능 및 수급 관계
- 상인/맵 등 참조 데이터

Game Content는 콘텐츠 업데이트 시 통째로 재구축할 수 있어야 합니다.

## 1.2 User Progress — 사용자가 만든 진행 상태

온라인 게임 데이터에서 복구할 수 없는 사용자별 상태입니다.

예:

- 캐릭터 프로필
- 레벨/진영/에디션/프레스티지
- 필요한 상인 상태
- 완료 퀘스트
- 은신처 현재 레벨
- 보유 아이템 수량

Game Content 업데이트는 User Progress를 수정하지 않습니다.

## 1.3 Derived State — 두 데이터에서 계산되는 결과

기본적으로 영구 저장하지 않습니다.

예:

- 현재 가능한 퀘스트
- 잠긴 퀘스트
- 앞으로 필요한 은신처 재료
- 현재 필요한 아이템 수량
- 아이템별 필요 출처 집계

필요하면 성능 캐시를 둘 수 있지만 언제든 재생성 가능해야 하며 진실의 원천이 아닙니다.

---

# 2. 콘텐츠 스냅샷

한 번의 성공한 데이터 업데이트 결과를 하나의 `ContentSnapshot`으로 봅니다.

최소 메타데이터:

- `schemaVersion` — 준현 헬퍼 내부 콘텐츠 형식 버전
- `builtAt` — 내부 DB 생성 시각
- `gameMode` — `regular | pve | pvp-season`
- `languages` — 포함된 표시 언어
- `sources` — 사용한 외부 원천/엔드포인트
- `sourceHashes` — 가능한 경우 원본/산출물 해시
- `warnings` — 활성화를 막지 않은 데이터 경고

게임 모드별 콘텐츠는 서로 독립적으로 빌드할 수 있어야 합니다.

한국어 표시를 기본으로 하되, 한국어 번역이 누락된 필드는 영어 fallback을 사용할 수 있습니다.

---

# 3. 공통 참조 데이터

## 3.1 Item

아이템의 영구 식별은 이름이 아니라 원천의 안정적인 `ItemId`를 사용합니다.

최소 필드:

- `id`
- `nameKo`
- `nameEn`
- `shortNameKo?`
- `shortNameEn?`
- `iconUrl?`
- `wikiUrl?`
- `categoryIds[]`

필요한 기능이 생기기 전에는 가격, 크기, 무게 등 모든 원본 필드를 저장하지 않습니다.

## 3.2 Trader

최소 필드:

- `id`
- `nameKo`
- `nameEn`
- `loyaltyLevels[]?`

로열티 레벨을 퀘스트 판정이나 탄약 수급처에서 사용할 경우에만 관련 요구 레벨/평판 정보를 내부 모델에 포함합니다.

## 3.3 MapReference

현재 핵심 데이터 영역에서 지도 화면 자체를 구현하기 위한 모델이 아닙니다.

퀘스트 분류/표시를 위한 최소 참조만 둡니다.

- `id`
- `nameKo`
- `nameEn`
- `normalizedKey?`

실제 지도 이미지, 좌표, 층 등은 지도 시스템의 별도 데이터 모델로 다룹니다.

---

# 4. Quest 모델

퀘스트 모델은 크게 세 부분으로 나눕니다.

1. 기본 정보
2. 해금 판정 정보
3. 목표/필요 아이템 정보

## 4.1 Quest 기본 정보

최소 필드:

- `id`
- `nameKo`
- `nameEn`
- `traderId?`
- `mapId?`
- `wikiUrl?`
- `experience?`
- `kappaRequired?`
- `lightkeeperRequired?`
- `disabled?`

보상 등은 사용자에게 실제로 보여줄 필요가 확정되면 추가할 수 있습니다. 현재 퀘스트 해금/필요 아이템 계산의 필수 데이터로 보지 않습니다.

## 4.2 QuestAvailabilityRule

퀘스트의 현재 가능 여부를 판단할 때 필요한 **게임 규칙**만 정규화합니다.

현재 확인된 종류:

- 최소 플레이어 레벨
- 진영 제한
- 선행 퀘스트와 허용 상태
- 상인 평판 조건
- 상인 로열티 조건
- 프레스티지 조건
- 에디션 허용/제외 조건
- 선택된 게임 모드 자체에 따른 콘텐츠 차이

시간 지연(`AvailableAfter`)은 제품 결정에 따라 내부 해금 판정에서 사용하지 않습니다.

### 선행 퀘스트 조건

내부 표현은 최소한 다음 의미를 보존해야 합니다.

- `requiredTaskId`
- `acceptedStatuses[]`
- 동일 그룹의 AND/OR 의미가 원천 데이터에 존재하면 그 관계

`Quest A 완료`라고 단순화하여 원천의 상태 조건을 잃지 않습니다.

### 조건 엔진의 단순성 원칙

처음부터 범용 수식/스크립트 엔진을 만들지 않습니다.

실제 데이터에서 확인된 조건 종류만 명시적인 내부 타입으로 지원합니다. 새로운 조건 종류가 나타나면 데이터 검증에서 감지하고 그때 모델을 확장합니다.

## 4.3 QuestObjective

사용자에게 퀘스트 내용을 설명하기 위한 목표입니다.

필요 범위의 필드:

- `questId`
- `objectiveId`
- `type`
- `descriptionKo`
- `descriptionEn`
- `optional`
- `count?`
- `foundInRaid?`
- `mapIds[]`
- `acceptedItemIds[]`
- `questItemId?`
- 필요한 경우 무기/장비/거리/지역 등의 제약 정보

Objective의 실질적인 내부 식별은 `(questId, objectiveId)` 조합으로 취급합니다. Objective ID가 전체 퀘스트에서 항상 전역 유일하다고 가정하지 않습니다.

## 4.4 QuestItemRequirement

`필요 아이템` 계산에서 중요한 것은 원본 objective의 모양이 아니라 **사용자가 실제로 보관/제출해야 하는 아이템 요구량**입니다.

따라서 콘텐츠 빌드 과정에서 QuestObjective를 해석하여 별도의 의미 모델로 정규화합니다.

최소 필드:

- `questId`
- `objectiveId`
- `itemIds[]` — 대체 가능한 아이템이 있으면 모두 보존
- `count`
- `foundInRaid`
- `requirementKind`

`requirementKind`는 최소한 다음 의미를 구분할 수 있어야 합니다.

- 실제 제출/인도 재료
- 단순 획득/발견 목표
- 판매 목표
- 기타 아이템 관련 목표

기본 `필요 아이템` 합계에는 **실제로 보관/제출해야 한다고 검증된 요구만 포함**합니다. `findItem`, `collect`, `sellItem` 같은 원본 타입을 이름만 보고 무조건 필요한 재료로 합산하지 않습니다.

---

# 5. Hideout 모델

## 5.1 HideoutStation

최소 필드:

- `id`
- `nameKo`
- `nameEn`
- `imageUrl?`
- `levels[]`

## 5.2 HideoutLevel

최소 필드:

- `stationId`
- `level`
- `constructionTime?`
- `itemRequirements[]`
- `stationLevelRequirements[]`
- `traderRequirements[]`
- `skillRequirements[]`

사용자 프로필에는 이 정의를 복사하지 않고 `현재 station level`만 저장합니다.

## 5.3 HideoutItemRequirement

최소 필드:

- `stationId`
- `targetLevel`
- `itemId`
- `count`
- `foundInRaid?`

이 데이터가 `필요 아이템`의 두 번째 원천입니다.

## 5.4 비아이템 요구조건

다른 시설 레벨, 상인, 스킬 요구사항은 Game Content에 보존할 수 있지만, **사용자가 실제로 은신처 해금 가능 여부까지 준현 헬퍼에서 계산하기로 확정하기 전에는 User Progress 입력 항목을 늘리지 않습니다.**

즉 데이터가 존재한다는 이유만으로 사용자에게 모든 스킬/상인 값을 입력하게 하지 않습니다.

---

# 6. Ammo 모델

탄약은 Item과 별도의 독립 아이템 체계를 만들지 않습니다.

`Ammo`는 `ItemId`를 참조하는 탄약 전용 정보입니다.

현재 필요한 큰 범위:

- `itemId`
- `caliber`
- `damage?`
- `penetrationPower?`
- `armorDamage?`
- `fragmentationChance?`
- `projectileCount?`
- `initialSpeed?`
- `accuracyModifier?`
- `recoilModifier?`
- 출혈 관련 값 등 실제 표시 가치가 확인된 성능값

정확한 원천 필드 이름/타입은 구현 직전 `regular/items` 라이브 fixture로 최종 고정합니다. 현재 원천의 `properties`가 탄약별 속성을 포함한다는 사실은 확인했지만, 준현 헬퍼는 필요한 속성만 명시적으로 변환합니다.

## 6.1 AmmoAcquisition

탄약 수급처는 표시 문자열 하나로 굳혀 저장하지 않습니다.

정규화된 관계로 저장합니다.

종류:

- `TraderPurchase`
- `TraderBarter`
- `HideoutCraft`

최소 의미:

- `ammoItemId`
- `kind`
- `traderId?`
- `traderLevel?`
- `stationId?`
- `stationLevel?`

가격/교환 재료/제작 재료를 어디까지 보여줄지는 탄약 상세 UX 단계에서 결정합니다.

---

# 7. User Progress 모델

## 7.1 GameProfile

프로필 하나는 실제 Tarkov 캐릭터 하나의 진행 상태입니다.

최소 큰 범위:

- `profileId`
- `gameMode`
- `level`
- `faction`
- `edition?`
- `prestige?`
- `traderProgress{}` — 실제 퀘스트 판정에 필요한 값만
- `completedQuestIds`
- `hideoutLevels{stationId -> level}`
- `inventory{itemId -> quantities}`

`pvp`, `pve`, `season` 캐릭터 간 진행 상태를 공유하지 않습니다.

## 7.2 InventoryQuantity

필요 아이템 계산을 위해 최소한 다음을 표현할 수 있어야 합니다.

- `fir`
- `nonFir`

총 보유량은 `fir + nonFir`로 계산합니다.

FIR/일반 혼합 요구의 만족 판정은 동일 아이템을 이중 계산하지 않습니다.

---

# 8. Derived State 모델

## 8.1 QuestAvailability

저장하지 않고 계산합니다.

결과는 최소한:

- `Locked`
- `Current`
- `Completed`

현재 제품 결정상 수주 가능한 퀘스트는 `Current`로 간주합니다.

## 8.2 NeededItem

필요 아이템 결과는 출처를 잃지 않아야 합니다.

개념적 구조:

- `itemId`
- `requiredFir`
- `requiredTotal`
- `ownedFir`
- `ownedNonFir`
- `remaining`
- `sources[]`

각 source는 최소한:

- `Quest` 또는 `Hideout`
- 관련 quest/station ID
- 요구 수량
- FIR 여부

를 보존합니다.

따라서 퀘스트 하나를 완료하거나 은신처 레벨 하나를 변경하면 해당 source만 계산에서 자연스럽게 제외됩니다.

---

# 9. 데이터 원천 검증 결과 — 2026-08-08

현재 `json.tarkov.dev/endpoints`에서 확인되는 핵심 엔드포인트:

- `tasks`
- `hideout`
- `items`
- `traders`
- `maps`
- `barters`
- `crafts`

현재 게임 모드:

- `regular`
- `pve`
- `pvp-season`

한국어 `ko` 번역도 지원합니다.

현재 TarkovTracker의 운영 코드가 `json.tarkov.dev`를 소비하며 확인하는 구조상:

- Tasks: task requirements, faction, min level, required prestige, trader requirements, objectives/fail conditions/rewards
- Hideout: item/station/skill/trader requirements와 crafts
- Items: item/category/properties
- Tasks payload: quest items와 prestige 포함

을 정규화할 수 있습니다.

주의:

- 현재 TarkovTracker 문서상 PvE prestige 데이터는 upstream에 없다고 명시되어 있으므로 `prestige`는 모든 프로필에 반드시 존재하는 값으로 취급하지 않습니다.
- 에디션별 퀘스트 허용/제외 규칙의 안정적인 원천은 별도 검증이 필요합니다.
- 탄약 세부 `properties` 필드와 barter/craft의 정확한 원본 shape는 구현 시작 시 라이브 fixture를 저장해 계약 테스트로 확정합니다.

---

# 10. 의도적으로 하지 않는 것

- 외부 JSON 필드를 그대로 전부 DB 컬럼으로 복제
- UI가 원본 JSON을 직접 읽음
- 이름을 영구 ID로 사용
- 파생 상태를 여러 DB에 중복 저장
- Objective type 문자열을 UI마다 제각각 해석
- 미래 조건을 예상한 범용 규칙 언어/스크립트 엔진
- API에 새 필드가 생겼다는 이유만으로 즉시 사용자 프로필 입력 항목 추가

새 데이터가 현재 모델이 이해할 수 없는 의미를 추가하면 자동으로 추측하지 않고 검증 단계에서 발견한 뒤 모델을 의도적으로 확장합니다.
