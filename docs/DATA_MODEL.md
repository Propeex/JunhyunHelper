# DATA MODEL — 준현 헬퍼 내부 데이터 모델

상태: `CONFIRMED — 현재 핵심 구현과 동기화`

이 문서는 **외부 API 모양이 아니라 준현 헬퍼가 이해하는 의미**를 정의합니다.

핵심 원칙:

> 원본 JSON을 제품 모델로 사용하지 않는다. 개발 단계에서 의미가 검증된 값만 작은 canonical model로 변환한다.

---

## 1. 데이터 종류

### Game Content

게임이 정하며 온라인에서 다시 만들 수 있는 사실입니다.

- Item
- Trader / Map 최소 참조
- Quest 정의/조건/목표
- Hideout 정의/요구 재료
- Ammo 성능/수급 관계

게임 모드별로 독립된 snapshot을 사용합니다.

### User Progress

사용자가 실제 게임에서 만든 상태이며 온라인 Game Content로 복구할 수 없습니다.

- 프로필 ID / game mode
- level / faction / edition / prestige
- trader progress
- completed quest IDs
- hideout current levels
- inventory FIR / Non-FIR

### Derived State

위 두 데이터에서 계산할 수 있는 결과입니다. 기본적으로 저장하지 않습니다.

- Quest `Current / Locked / Indeterminate`
- Needed Items
- 남은 수량/집계
- 화면용 그룹/정렬 결과

---

## 2. 식별 원칙

관계의 영구 식별은 표시 이름이 아니라 원천의 stable ID를 사용합니다.

예:

- ItemId
- QuestId
- TraderId
- MapId
- StationId

번역 문자열은 관계를 만들거나 식별자를 대체할 수 없습니다.

Quest Objective는 전역 Objective ID 하나가 아니라 `(QuestId, ObjectiveId)` 범위로 식별합니다.

---

## 3. Item

공통 Item은 다른 기능의 기준 참조입니다.

현재 최소 의미:

- `Id`
- 한국어/영어 이름
- 한국어/영어 짧은 이름
- icon URL
- wiki URL
- category IDs

API에 필드가 있다는 이유만으로 가격/무게/크기 등 모든 속성을 복사하지 않습니다.

---

## 4. Quest

### QuestDefinition

현재 보존하는 핵심 의미:

- ID / 표시 이름
- Trader / Map 참조
- Wiki / XP
- Kappa / Lightkeeper 관련 표시 사실
- disabled
- minimum player level
- faction
- required prestige level
- prerequisite Quest + accepted statuses
- trader reputation requirements
- trader loyalty requirements
- 아직 판정하지 못하는 availability requirement type 목록

### 선행 Quest 상태

현재 canonical 상태:

- `Complete`
- `Active`
- `Failed`

준현 헬퍼는 수주 가능한 Quest를 이미 수락한 것으로 간주하므로, 해금된 Quest는 `Active` 조건에 사용할 수 있습니다.

`Failed` 상태는 원천 의미를 보존하지만 사용자 실패 진행 UX가 아직 정의되지 않아 현재 판정에서는 `Indeterminate`를 만듭니다.

### Trader requirement

2026-08-08 live raw에서 `traderRequirements`는 `requirementType + compareMethod + value + trader` 구조입니다.

현재 canonical 의미:

- reputation `>=` → `AtLeast`
- reputation `<=` → `AtMost`
- reputation `<` → `LessThan`
- level `>=` → loyalty level requirement

숫자 부호를 보고 비교 방향을 추측하지 않습니다.

새 requirement type / comparison이 나타나면 importer가 실패합니다.

### 추가 availability requirement

현재 live data에서 비어 있지 않은 `otherRequirements`는 일부 Quest의 `dialogue` 유형으로 확인했습니다.

현재 제품이 이 상태를 입력/판정하지 않으므로:

- Game Content에는 type을 보존
- Quest availability는 `Indeterminate`

로 처리합니다.

조건을 조용히 무시하여 Quest를 `Current`로 오판하지 않습니다.

### Edition

사용자 프로필의 `EditionId`는 실제 사용자 사실로 저장합니다.

하지만 현재 `json.tarkov.dev/tasks` raw에는 Quest edition 허용/제외 규칙이 직접 존재하지 않습니다.

따라서 **신뢰 가능한 데이터 원천을 확정하기 전까지 edition 기준 Quest filtering 규칙을 canonical Quest model에 하드코딩하지 않습니다.**

---

## 5. Quest Objective / Quest Item Requirement

Quest Objective는 퀘스트 내용을 표현하는 데이터입니다.

Needed Items는 Objective를 화면마다 다시 해석하지 않습니다. Importer가 아이템 관련 의미를 한 번 분류합니다.

현재 중요한 분류:

- `giveItem` → stash 제출 requirement
- `findItem` / `collect` → 획득 목표, 제출 재료 합계에서 제외
- `sellItem` → 판매 목표, 제출 재료 합계에서 제외
- `findQuestItem` / `giveQuestItem` → 전용 Quest Item, 일반 stash 합계에 자동 포함하지 않음
- 기타 → 원래 Objective에는 보존하되 Needed Items로 추측하지 않음

### QuestItemRequirement

현재 의미:

- QuestId
- ObjectiveId
- AcceptedItemIds
- Count
- FoundInRaid

여러 `AcceptedItemIds`가 있으면 **대체 가능한 하나의 요구 그룹**입니다.

준현 헬퍼는 이 중 하나를 임의로 선택하지 않습니다.

`NeededItemRequirementBuilder`는:

- accepted item이 하나뿐인 요구 → 확정 계산 가능
- accepted item이 여러 개인 요구 → `AlternativeQuestRequirements`로 별도 반환

합니다.

---

## 6. Hideout

### HideoutStation

- ID
- 표시 이름
- 이미지 참조
- Levels

### HideoutLevel

현재 핵심 구현은 시설 레벨과 Item Requirement를 보존합니다.

### HideoutItemRequirement

- StationId
- TargetLevel
- ItemId
- Count
- FoundInRaid

User Progress에는 Game Content 요구사항을 복사하지 않고 **현재 station level만 저장**합니다.

Needed Items에서 어느 레벨 범위를 포함할지는 아직 제품 정책이므로 Core가 임의로 결정하지 않습니다.

호출자가 명시적으로 선택한 Hideout requirement만 계산 입력으로 사용합니다.

---

## 7. Ammo

Ammo는 별도 아이템 체계가 아니라 `ItemId`를 참조하는 전용 정보입니다.

### 탄약 식별

현재 live raw 기준:

`properties.propertiesType == "ItemPropertiesAmmo"`

만 탄약 성능 레코드로 인정합니다.

`types`에 `ammo`가 있다는 이유만으로 포함하지 않습니다. 현재 raw에는 grenade/ammo box도 `ammo` type을 가질 수 있기 때문입니다.

### AmmoDefinition

현재 canonical 성능:

- ItemId
- Caliber / AmmoType
- ProjectileCount
- Damage / ArmorDamage / PenetrationPower
- FragmentationChance / RicochetChance
- AccuracyModifier / RecoilModifier
- InitialSpeed
- HeavyBleedModifier / LightBleedModifier
- Tracer / TracerColor

추가 raw 속성은 실제 제품 요구가 생기기 전까지 넣지 않습니다.

### AmmoAcquisition

종류:

- `TraderPurchase`
- `TraderBarter`
- `HideoutCraft`

필요한 경우 다음 의미를 보존합니다.

- Trader / Station
- required level
- unlock Quest
- output count
- price / currency
- duration
- buy limit
- barter/craft required items
- craft tool 여부

수급처를 사람이 작성한 문자열로 저장하지 않습니다.

---

## 8. User Progress

`GameProfileSnapshot` 하나는 실제 Tarkov 캐릭터 하나입니다.

모드별 캐릭터는 독립입니다.

현재 explicit facts:

- ProfileId
- GameMode (`regular / pve / pvp-season`)
- Level
- Faction
- EditionId?
- PrestigeLevel?
- Traders `{TraderId → LoyaltyLevel, Standing}`
- CompletedQuestIds
- HideoutLevels `{StationId → Level}`
- Inventory `{ItemId → FIR, NonFIR}`

현재 Quest/Needed Items 계산 결과는 저장하지 않습니다.

---

## 9. Quest Availability

계산 결과:

- `Completed`
- `Current`
- `Locked`
- `Indeterminate`

`Indeterminate`는 오류를 숨기기 위한 상태가 아니라 **현재 입력과 지원 규칙만으로는 참/거짓을 안전하게 결정할 수 없다는 명시적인 결과**입니다.

예:

- 필요한 trader 값 미입력
- 필요한 prestige 값 미입력
- Failed-only prerequisite
- 미지원 `dialogue` requirement
- dependency cycle

시간 지연 해금은 확정 제품 결정에 따라 판정하지 않습니다.

---

## 10. Needed Items

확정적으로 계산 가능한 요구사항은 내부 `ItemRequirement`로 정규화합니다.

- ItemId
- RequiredTotal
- RequiredFir
- Source

Source는 최소:

- Quest + Objective
- Hideout + TargetLevel

을 보존합니다.

FIR 규칙:

- FIR 최소 수량 충족 필요
- 총 수량 최소도 별도로 충족 필요
- FIR 아이템은 unrestricted 수량에도 사용할 수 있음
- 같은 아이템을 이중 계산하지 않음

대체 아이템 요구는 자동 선택하지 않고 별도 그룹으로 반환합니다.

---

## 11. GameContentCatalog / Snapshot

한 게임 모드의 검증된 canonical Game Content를 `GameContentCatalog`로 묶습니다.

포함:

- Items
- Traders
- Maps
- Quests
- QuestObjectives
- QuestItemRequirements
- HideoutStations
- Ammo

Game Content는 모드별 `content.db`에 versioned snapshot으로 저장합니다.

계산 편의를 위한 convenience property는 `[JsonIgnore]` 처리하여 같은 사실을 snapshot에 중복 저장하지 않습니다.

Game Content는 재생성 가능하므로 내부 모델이 호환 불가능하게 바뀌면 긴 migration chain보다 새 API 데이터에서 재구축하는 것을 기본으로 합니다.

User Progress는 재생성 불가능하므로 별도 migration 정책을 사용합니다.
