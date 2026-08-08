# DATA SOURCE AUDIT — 최신 게임 데이터 공급원 검증

검증일: **2026-08-08**

상태: `CONFIRMED — regular 핵심 raw 계약 직접 검증`

이 문서는 준현 헬퍼가 의존할 외부 게임 데이터가 실제로 어떤 구조와 의미를 제공하는지 기록합니다.

## 1. 1차 원천 — json.tarkov.dev

현재 사용 endpoint:

| Endpoint | 준현 헬퍼 용도 |
|---|---|
| `tasks` | 퀘스트, quest item, prestige |
| `hideout` | 은신처 시설/레벨/요구사항 |
| `items` | 공통 아이템 + 탄약 성능 + 직접 상인 구매 |
| `traders` | 상인 참조 |
| `maps` | 퀘스트 맵 참조/분류 |
| `barters` | 상인 물물교환 수급처 |
| `crafts` | 은신처 제작 수급처 |

게임 모드:

- `regular`
- `pve`
- `pvp-season`

한국어 `ko` 지원.

## 2. 2026-08-08 live raw probe

GitHub Actions runner에서 현재 `json.tarkov.dev/regular/*`를 직접 다운로드하여 raw JSON 구조를 검사했습니다.

검사한 endpoint:

- `items`
- `tasks`
- `barters`
- `crafts`

당시 개수:

- Items: **5,310**
- Tasks: **510**
- Barters: **789**
- Crafts: **214**
- `properties.propertiesType == ItemPropertiesAmmo`: **200**

이 숫자는 데이터 정상 여부를 판정하는 영구 임계값으로 사용하지 않습니다. 패치로 정상적으로 달라질 수 있습니다.

목적은 **현재 raw shape와 의미를 최초 importer의 contract로 고정**하는 것입니다.

## 3. 번역 구조

번역 가능한 endpoint는 base document와 언어 문서를 사용합니다.

준현 헬퍼 정책:

1. base 원본에서 ID/관계를 먼저 해석
2. 표시 문자열에만 한국어 적용
3. 한국어 누락 시 영어 fallback
4. 번역은 ID/참조를 바꿀 수 없음

번역 실패는 핵심 게임 사실의 실패와 같은 등급으로 취급하지 않습니다.

## 4. Quest live contract

현재 task에 확인된 주요 의미:

- ID / 이름
- trader / map
- minimum player level
- faction
- prerequisite tasks + accepted statuses
- trader requirements
- required prestige
- objectives
- fail conditions
- rewards
- quest item refs
- mode-specific metadata
- unlock delay

### prerequisite 상태

현재 실제 데이터에서 확인:

- `complete`
- `active`
- `failed`

준현 헬퍼는 이 세 의미를 canonical enum으로 보존합니다.

### faction

현재 실제 값:

- `Any`
- `BEAR`
- `USEC`

### requiredPrestige

현재 일부 task가 prestige ID를 string reference로 사용합니다.

`tasks.data.prestige`의 ID → prestige level 관계를 먼저 읽고 task의 요구 레벨로 정규화합니다.

### traderRequirements — 중요한 live 계약

현재 상인 평판과 상인 로열티 레벨 조건은 **같은 `traderRequirements` 배열**에 있습니다.

구분 필드:

- `requirementType`
- `compareMethod`
- `value`
- `trader`

현재 확인된 `requirementType`:

- `reputation`
- `level`

현재 reputation 비교:

- `>=`
- `<=`
- `<`

현재 level 비교:

- `>=`

따라서 준현 헬퍼는 숫자 부호로 비교 방향을 추측하지 않습니다.

예:

```text
reputation + >= + 1.5 → Standing AtLeast 1.5
reputation + <= + -1 → Standing AtMost -1
reputation + <  + -1 → Standing LessThan -1
level      + >= + 3 → LoyaltyLevel >= 3
```

새 `requirementType` 또는 새 비교 연산자가 나타나면 자동 추정하지 않고 content update를 실패시켜 importer 업데이트가 필요함을 알립니다.

### objective types

현재 regular tasks에서 확인된 objective types:

- `giveItem`
- `visit`
- `shoot`
- `findItem`
- `plantItem`
- `findQuestItem`
- `extract`
- `giveQuestItem`
- `mark`
- `buildWeapon`
- `plantQuestItem`
- `skill`
- `traderLevel`
- `taskStatus`
- `useItem`
- `sellItem`
- `experience`
- `traderStanding`
- `dialogue`
- `globalVariable`

Needed Items 관련 핵심:

- `giveItem` → stash에서 제출해야 하는 일반 아이템 requirement
- `findItem` → 획득 목표이며 제출 재료와 동일하게 합산하지 않음
- `sellItem` → 판매 목표이며 제출 재료로 합산하지 않음
- `findQuestItem` / `giveQuestItem` → 전용 quest item이며 일반 보유 아이템 집계에 자동 포함하지 않음

`giveItem.items`는 여러 대체 가능한 item ID를 담을 수 있으므로 하나의 requirement group으로 보존합니다.

## 5. Hideout

`hideout`은 현재 큰 틀 요구에 필요한 다음 정보를 제공합니다.

- station
- levels
- item requirements
- station requirements
- trader/skill requirements
- crafts

현재 사용자 진행에는 실제 시설의 현재 level만 저장하고, requirement는 Game Content에서 계산하는 방향을 유지합니다.

## 6. Ammo live contract

### 탄약 식별

`types` 배열에 `ammo`가 있다는 이유만으로 탄약 표에 넣지 않습니다.

실제 raw에는:

- grenade
- ammo box

등도 `ammo` type을 포함할 수 있습니다.

현재 실제 탄약 성능 레코드의 명확한 식별자는:

```text
properties.propertiesType == "ItemPropertiesAmmo"
```

입니다.

2026-08-08 regular items에서는 **200개**가 이 계약을 만족했습니다.

### 현재 사용하는 성능 필드

`properties`에서 확인:

- `caliber`
- `ammoType`
- `projectileCount`
- `damage`
- `armorDamage`
- `penetrationPower`
- `fragmentationChance`
- `ricochetChance`
- `accuracyModifier`
- `recoilModifier`
- `initialSpeed`
- `heavyBleedModifier`
- `lightBleedModifier`
- `tracer`
- `tracerColor`

raw에는 ballistic coefficient, mass, diameter, durability/heat/misfire 등 추가 속성도 있지만 **데이터가 있다는 이유만으로 canonical model을 비대하게 만들지 않습니다.** 탄약 표의 제품 요구가 필요로 할 때 추가합니다.

### 직접 상인 구매

Ammo item의 `buyFromTrader`에서 현재 확인:

- trader
- price
- priceRUB
- currency
- currencyItem
- minTraderLevel
- taskUnlock
- restockAmount
- buyLimit

준현 헬퍼는 원화 환산 같은 별도 추론 없이 원천의 실제 price/currency 관계를 저장합니다.

### barter

`barters.data`는 array입니다.

현재 entry:

- id
- trader
- taskUnlock
- requiredItems
  - item
  - count
  - attributes
- minTraderLevel
- restockAmount
- buyLimit
- offeredItem
  - item
  - count
  - attributes

현재 ammo output barter는 실제 item barter이며 직접 화폐 구매와 별도로 취급합니다.

### craft

`crafts.data`는 array입니다.

현재 entry:

- id
- requiredItems
- requiredQuestItems
- station
- duration
- gameEditions
- level
- taskUnlock (일부)
- productItem

craft requirement의 `attributes.tool == true`는 도구 의미를 잃지 않게 canonical requirement에 보존합니다.

## 7. 보정 데이터/overlay 정책

TarkovTracker 등 다른 소비자가 community correction overlay를 사용하는 것은 확인했지만 준현 헬퍼는 자동으로 이를 승계하지 않습니다.

원칙:

1. 우선 `json.tarkov.dev` 원본을 canonical model로 변환
2. 실제 게임과 반복적으로 확인되는 오류/누락이 있으면 별도 보정 원천 도입 검토
3. 도입한다면 `Base Source → Explicit Overlay → Validation`으로 공식화
4. 개별 Quest ID를 코드 곳곳에 하드코딩해 보정하지 않음

## 8. 결론

현재 핵심 흐름은 유지할 수 있습니다.

```text
json.tarkov.dev
  ├─ tasks
  ├─ hideout
  ├─ items
  ├─ traders
  ├─ maps
  ├─ barters
  └─ crafts
        ↓
명시적 Importer
        ↓
semantic/reference validation
        ↓
준현 헬퍼 Game Content
        ↓
Quest / Hideout / Needed Items / Ammo
```

외부 데이터가 현재 아는 계약과 다르게 바뀌면 프로그램은 새 의미를 스스로 추측하지 않습니다. 안전하게 해석할 수 없는 content update를 중단하고 마지막 정상 데이터를 유지합니다.
