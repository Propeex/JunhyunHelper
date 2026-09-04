# DATA MODEL — 준현 헬퍼 내부 데이터 모델

상태: `CONFIRMED — 현재 핵심 구현과 동기화`

이 문서는 **외부 API 모양이 아니라 준현 헬퍼가 이해하는 의미**를 정의합니다.

핵심 원칙:

> 원본 JSON을 제품 모델로 사용하지 않는다. 의미가 검증된 값만 작은 canonical model로 변환한다.

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

현재 Content snapshot write schema는 **v12**, 읽기 지원 범위는 **v3~v12**입니다.

### User Progress

사용자가 실제 게임에서 만든 상태이며 온라인 Game Content로 복구할 수 없습니다.

- 프로필 ID / game mode
- level / faction / edition / prestige
- trader progress
- completed Quest / 필요한 explicit permanent failure
- exact profile-variable 값이 실제로 관측된 경우의 `ProfileVariables`
- recoverable special-trader access의 sparse override fact
- hideout current levels
- inventory FIR / Non-FIR
- Quest / Hideout 자동 소비와 rollback을 위한 consumption ledger

`user.db` SQLite schema는 현재 v1입니다.

### Derived State

위 두 데이터에서 계산할 수 있는 결과입니다. 기본적으로 저장하지 않습니다.

- Quest availability / presentation state
- Future Needed Items planning
- Needed Items / cleanup 결과
- Flexible hand-in progress
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
- ProfileVariableId

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
- structured profile-variable requirements
- recoverable special-trader access requirement
- availability delay metadata
- 아직 판정하지 못하는 availability requirement type 목록

### 선행 Quest 상태

canonical prerequisite 상태:

- `Complete`
- `Active`
- `Failed`

준현 헬퍼는 수주 가능한 Quest를 이미 수락한 것으로 간주하므로, 해금된 Quest는 `Active` 조건에 사용할 수 있습니다.

`Failed` 의미는 source 그대로 보존합니다. 사용자가 동기화할 수 있는 실제 비재시작형 영구 실패 fact와 결합해 판정하며, 프로그램이 실패 여부를 모르는 경우 거짓으로 확정하지 않습니다.

### Trader requirement

canonical 의미는 raw `requirementType + compareMethod + value + trader`를 검증한 뒤 명시적인 비교로 보존합니다.

예:

- reputation `>=` → `AtLeast`
- reputation `<=` → `AtMost`
- reputation `<` → `LessThan`
- level `>=` → loyalty level requirement

숫자 부호를 보고 비교 방향을 추측하지 않습니다.

새 requirement type / comparison이 importer가 이해하는 범위를 벗어나면 fail-closed 합니다.

### Profile-variable requirement — schema v7에서 구조화 도입

`globalVariable` requirement는 opaque 문자열이 아니라 다음 의미를 구조적으로 보존합니다.

- `VariableId`
- comparison operator
- required integer value

판정:

1. 동일 ID의 exact current `ProfileVariables` 값이 있으면 그 값 사용
2. current-version audited task-pool compatibility가 정확히 증명되는 범위는 검증된 reconstruction 사용 가능
3. 어느 쪽도 아니면 `Indeterminate`

key 부재를 값 0으로 해석하지 않습니다.

### Special trader access

Lightkeeper처럼 최초 unlock 이후에도 접근을 잃고 복구할 수 있는 상태는 ordinary prerequisite와 분리합니다.

- BTR Driver 누락 gate는 검증된 `A Helping Hand = Active` 의미
- Ref 누락 gate는 GameMode별 검증된 unlock Quest `Complete` 의미
- Lightkeeper recoverable access는 별도 special-trader access requirement

recoverable 접근 상실은 영구 Quest 불가가 아니라 `Locked` 원인이 될 수 있습니다.

### Dialogue / 기타 availability

정확히 감사된 dialogue Quest에는 exact-ID compatibility를 적용할 수 있습니다.

새롭거나 변경된 unsupported availability condition은 조용히 무시하지 않습니다.

- type/diagnostic 의미 보존
- 안전하게 판정할 수 없으면 `Indeterminate`

### Availability delay

`availableDelaySecondsMin / Max`는 canonical metadata로 보존합니다.

실제 게임 완료 시각이 필요한데 User Progress에 그 fact가 없으면 `Indeterminate`입니다. Helper 버튼 클릭 시각으로 가짜 completion timestamp를 만들지 않습니다.

### Edition

사용자 프로필의 `EditionId`는 실제 사용자 사실로 저장합니다.

Quest edition 허용/제외 규칙은 신뢰 가능한 source/overlay에서 검증된 경우에만 적용하며 이름이나 추측으로 canonical 규칙을 만들지 않습니다.

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

v0.1.13 final canonical validation은 다음을 fatal로 차단합니다.

- accepted item 후보가 비어 있음
- `Count <= 0`

---

## 6. Hideout

### HideoutStation

- ID
- 표시 이름
- 이미지 참조
- Levels

### HideoutLevel

시설 레벨과 Item Requirement를 보존합니다.

### HideoutItemRequirement

- StationId
- TargetLevel
- ItemId
- Count
- FoundInRaid

`Count <= 0`은 final validation에서 fatal입니다.

User Progress에는 Game Content 요구사항을 복사하지 않고 **현재 station level만 저장**합니다.

Needed Items는 현재 station level 이후의 미래 upgrade requirement를 Application planning에서 선택합니다.

---

## 7. Ammo

Ammo는 별도 아이템 체계가 아니라 `ItemId`를 참조하는 전용 정보입니다.

### 탄약 식별

현재 raw 기준:

`properties.propertiesType == "ItemPropertiesAmmo"`

만 탄약 성능 레코드로 인정합니다.

`types`에 `ammo`가 있다는 이유만으로 포함하지 않습니다. grenade/ammo box도 `ammo` type을 가질 수 있기 때문입니다.

### AmmoDefinition

canonical 성능:

- ItemId
- Caliber / AmmoType
- ProjectileCount
- Damage / ArmorDamage / PenetrationPower
- FragmentationChance / RicochetChance
- AccuracyModifier / RecoilModifier
- InitialSpeed
- HeavyBleedModifier / LightBleedModifier
- Tracer / TracerColor
- Wiki Ballistics table membership
- Armor Class 1~6 effectiveness when available

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

Ammo favorite는 Game Content가 아니라 presentation preference이며 `%LocalAppData%/JunhyunHelper/ammo-favorites.json`에 별도 저장합니다.

---

## 8. User Progress

`GameProfileSnapshot` 하나는 실제 Tarkov 캐릭터 하나입니다.

모드별 캐릭터는 독립입니다.

현재 explicit facts:

- ProfileId
- GameMode (`regular / pve / pvp-season`)
- Level
- Faction
- EditionId
- PrestigeLevel (기본 0)
- Traders `{TraderId → LoyaltyLevel, Standing}`
- CompletedQuestIds
- 필요한 permanent failed Quest facts
- ProfileVariables `{VariableId → exact current integer}` — 관측된 key만 저장
- SpecialTraderAccessOverrides — recoverable 상태가 실제로 동기화된 경우만 저장
- HideoutLevels `{StationId → Level}`
- Inventory `{ItemId → FIR, NonFIR}`
- Quest/Hideout consumption ledger

Quest/Needed Items 계산 결과는 저장하지 않습니다.

---

## 9. Quest Availability

Core 계산 결과의 핵심 상태:

- `Completed`
- `Current`
- `Locked`
- `Indeterminate`

Application은 faction/permanent branch 같은 제품 상태와 결합해 사용자에게 `사용 불가` 등을 표시할 수 있지만, Core `Indeterminate`를 optimistic `Current`로 바꾸지 않습니다.

`Indeterminate`는 오류를 숨기기 위한 상태가 아니라 **현재 입력과 지원 규칙만으로는 참/거짓을 안전하게 결정할 수 없다는 명시적인 결과**입니다.

예:

- 필요한 exact profile-variable 값이 없고 audited compatibility도 적용할 수 없음
- 새로운/변경된 unsupported dialogue condition
- 실제 completion timestamp가 필요한 delay
- dependency cycle 또는 안전하게 판정할 수 없는 prerequisite fact

사용자에게는 `확인 필요`로 표시합니다.

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

Future planning은 `IndeterminatePotential`을 보수적으로 보호하여 프로그램이 미래 필요 가능성을 증명 없이 제거하지 않습니다.

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

현재 최신 schema는 **v7**이고 v3~v7을 last-known-good 범위로 읽습니다.

계산 편의를 위한 convenience property는 같은 사실을 snapshot에 중복 저장하지 않습니다.

Game Content는 재생성 가능하므로 내부 모델이 호환 불가능하게 바뀌면 긴 migration chain보다 새 API 데이터에서 재구축하는 것을 기본으로 합니다.

User Progress는 재생성 불가능하므로 별도 migration 정책을 사용합니다.
