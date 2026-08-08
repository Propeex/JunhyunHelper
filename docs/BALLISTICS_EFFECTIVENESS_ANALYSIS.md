# BALLISTICS EFFECTIVENESS ANALYSIS — 방탄복 클래스 효율값 조사

검증일: **2026-08-08**

상태: `EXACT RATING SOURCE VERIFIED / IMPLEMENTATION IN PROGRESS`

목적:

> Ammo 표에 Tarkov Wiki / NoFoodAfterMidnight 계열의 `Bullet effectiveness against armor class` Class 1~6 숫자와 색상을 제공할 수 있는지, API 원천값과 실제 게임 공식을 기준으로 검증한다.

## 1. 제품 요구

사용자 요구:

- Ammo 표에 Armor Class 1~6 열을 둔다.
- 각 칸에는 0~6 숫자와 숫자에 대응하는 색상을 표시한다.
- Tarkov Wiki의 `Bullet effectiveness against armor class`와 같은 의미여야 한다.
- 원천 API가 직접 값을 주지 않더라도 공식이 명확하다면 준현 헬퍼가 결정론적으로 계산한다.
- 임의의 자체 등급은 만들지 않는다.

Ammo 기본 정렬:

1. penetration power 오름차순
2. penetration power 동률이면 damage 오름차순
3. 둘 다 동률이면 name 순

## 2. 현재 1차 API가 주는 것

현재 json.tarkov.dev 기반 canonical Ammo에는 다음 원시 사실이 있다.

- projectileCount
- damage
- armorDamage
- penetrationPower
- fragmentationChance
- ricochetChance
- accuracy/recoil modifiers
- initialSpeed
- bleed modifiers
- tracer

하지만 Wiki/NoFood의 Armor Class 1~6 **0~6 effectiveness rating 자체는 raw field가 아니다.**

따라서 그 등급을 표시하려면:

1. 공식 계산이 가능해야 하거나,
2. 별도의 명시적이고 검증 가능한 source/overlay가 필요하다.

## 3. Wiki/NoFood 0~6의 의미

현재 Tarkov Wiki와 eft-ammo.com은 이 값을 정확한 실제 전투 결과가 아니라 **comparison guideline**으로 설명한다.

현재 표의 의미:

| 값 | 대략적 의미 | Avg shots stopped before killing |
|---:|---|---:|
| 0 | 사실상 무의미 | 20+ |
| 1 | 매우 낮은 가능성 | 13–20 |
| 2 | 대량 사격 필요 | 9–13 |
| 3 | 약간 효과적 | 5–9 |
| 4 | 효과적 | 3–5 |
| 5 | 매우 효과적 | 1–3 |
| 6 | 거의 무시 | <1 / 초기 관통 >80% |

중요한 Wiki 주석:

- 한 번 쏠 때의 모든 projectile이 방어구에 맞는다고 가정한다.
- **각 projectile은 방어구 내구도에 최소 1의 손상을 준다.**
- 그래서 낮은 penetration이라도 pellet/dart가 많은 탄은 반복 사격 시 방어구를 빠르게 깎아 높은 등급을 받을 수 있다.

이 때문에 0~6 등급은 단순한 `penetrationPower / 10` 또는 처음 한 발의 관통 확률만으로 만들 수 없다.

검증 예:

- `.50 AE JHP` — Pen 12 → `6,1,0,0,0,0`
- `.300 Blackout Whisper` — Pen 14 → `6,4,2,1,0,0`
- `12/70 Flechette` — Pen 31, 8 darts → `6,6,6,5,5,5`

동일한 penetration 값 근처에서도 armor damage와 projectile 구조 때문에 값이 달라지므로 단순 penetration/class ratio는 정답이 아니다.

참조:

- https://escapefromtarkov.fandom.com/wiki/Ballistics
- https://www.eft-ammo.com/

## 4. 현재 게임의 관통 공식 — 확인됨

2026-01-17 공개된 SPT 4.0 클라이언트 역컴파일 코드에서 EFT 클라이언트가 사용하는 계산을 확인했다.

참조 파일:

- `itinybad/SPT-Client-400/Assembly-CSharp/GClass659.cs`
- `itinybad/SPT-Client-400/Assembly-CSharp/ArmorResistanceStruct.cs`
- `itinybad/SPT-Client-400/Assembly-CSharp/EFT/InventoryLogic/ArmorComponent.cs`

### 4.1 실제 방어 저항값

```text
durabilityPercent = currentDurability / templateDurability * 100

RealResistance =
    (121 - 5000 / (45 + durabilityPercent * 2))
    * ArmorClassResistance
    * 0.01
```

`ArmorClassResistance`는 backend armor class 설정에서 읽는다.

완전한 방어구에서는 RealResistance가 대략 해당 클래스의 기준 저항값에 가까우며, 내구도가 내려갈수록 저항도 내려간다.

### 4.2 관통 확률

`R = RealResistance`, `P = penetrationPower`일 때 게임 코드의 확률 단위는 0~100이다.

```text
if R >= P + 15:
    chance = 0
else if R >= P:
    chance = 0.4 * (R - P - 15)^2
else if R <= P - 15:
    chance = 100
else:
    chance = 100 + P / (0.9 * R - P)
```

실제 ArmorComponent는 이 확률과 shot RNG를 비교해 penetrate/block을 결정한다.

### 4.3 관통 후 감소 계수

```text
CF = clamp(P / (R + 12), 0.6, 1.0)
```

관통한 경우 projectile의 damage와 penetration power는 이 계수로 감소한다.

## 5. 현재 게임의 방어구 내구도 손상 — 확인됨

실제 `ArmorComponent.ApplyDamage`에서 확인된 구조다.

기호:

- `P` = 원래 penetration power
- `A` = ammo armor-damage multiplier
- `C` = ArmorClassResistance
- `D` = armor material destructibility
- `CF` = 위 관통 후 감소 계수

### 관통한 경우

```text
postPen = P * CF

armorDurabilityDamage =
    postPen
    * A
    * clamp(P / C, 0.5, 0.9)
    * D
```

### 방어구에 막힌 경우

```text
armorDurabilityDamage =
    P
    * A
    * clamp(P / C, 0.6, 1.1)
    * D
```

마지막으로:

```text
armorDurabilityDamage = max(1, armorDurabilityDamage)
```

따라서 projectile 하나당 최소 1 내구도 손상이라는 Wiki 설명과 일치한다.

주의:

`DamageInfo.ArmorDamage`가 canonical API의 `armorDamage` 백분율과 어떤 정규화 관계인지 importer/게임 template 기준으로 한 번 더 검증한 뒤 실제 C# 구현해야 한다.

## 6. 게임 UI 자체도 클래스별 관통 확률을 계산함

현재 공개 SPT Realism client의 UI patch는 원본 EFT의 탄약 관통 UI 계산을 다음 형태로 호출한다.

```text
RealResistance(100, 100, armorClass, penetrationPower)
    .GetPenetrationChance(penetrationPower)
```

즉 **Armor Class별 초기 관통 확률**은 준현 헬퍼에서도 동일한 공식으로 결정론적으로 계산 가능하다.

하지만 이것은 Wiki의 0~6 effectiveness rating과 동일하지 않다.

## 7. Wiki 0~6의 exact 계산식은 공개 근거가 부족함

반복 사격 후 효과를 계산하려면 Ammo 원시값 외에도 표준 방어구의 다음 사실이 필요하다.

- armor class resistance
- 시작/max durability
- material destructibility
- 어떤 방어구/plate를 그 클래스의 대표값으로 쓰는지
- 반복 사격에서 kill/penetration을 어떤 기준으로 등급화하는지
- projectile가 여러 개인 탄의 처리
- RNG를 평균화하는 방법

현재 게임 공식을 이용하면 **특정한 실제 방어구 하나에 대한 시뮬레이션**은 가능하다.

그러나 Wiki/eft-ammo 표는 `Class 4`처럼 클래스 하나만 표시하고, 현재 확인한 공개 설명에서는 "Class 4 rating을 계산할 때 어느 durability/material의 어떤 reference armor를 사용한다"는 규칙을 공개하지 않는다.

따라서 임의로 예를 들어:

```text
Class 4 = durability 50 / steel
```

같은 기준을 준현 헬퍼가 정하면 그 순간 자체 heuristic이 된다.

이는 제품 철학에 맞지 않는다.

## 8. exact rating의 실시간 원천 — 확인됨

### 8.1 공식 Tarkov Wiki Ballistics 표

사용자가 직접 지정한 공식 Wiki Ballistics 페이지에는 현재 Ammo별로 다음이 명시되어 있다.

```text
Ammo name
raw ballistic stats
Class 1 rating
Class 2 rating
Class 3 rating
Class 4 rating
Class 5 rating
Class 6 rating
```

즉 **0~6 값 자체를 외부 표 데이터로 취급할 수 있는 검증 가능한 source가 존재한다.**

이것은 추정 공식이 아니다.

### 8.2 MediaWiki Action API

Fandom Wiki는 MediaWiki 기반이며, MediaWiki는 `api.php?action=parse&page=...` 방식으로 현재 페이지의 parser output을 제공하는 Action API를 지원한다.

따라서 준현 헬퍼는 일반 HTML 화면을 browser automation으로 긁는 대신:

```text
Escape from Tarkov Wiki MediaWiki Action API
→ Ballistics page parser output
→ ammo row + rightmost Class 1~6 values 추출
→ canonical Tarkov ammo와 안전하게 이름 매칭
→ verified optional enrichment
```

형태로 구현한다.

중요:

- `json.tarkov.dev`가 계속 raw Ammo stat의 1차 원천이다.
- Wiki source는 **Armor Class 1~6 rating만** 보충한다.
- Wiki source 장애가 Quest/Hideout/Item/Game Content 기본 업데이트 전체를 막으면 안 된다.
- row가 모호하거나 canonical ammo에 유일하게 매칭되지 않으면 값을 추정하지 않고 `unknown`으로 둔다.

### 8.3 eft-ammo.com 교차 검증

현재 eft-ammo.com은 NoFoodAfterMidnight의 동일한 0~6 scale을 제공하고 있으며 공식 Wiki의 대표 샘플과 값이 일치한다.

이를 runtime 1차 source로 추가하지 않고 **회귀 샘플/교차 검증 근거**로 사용한다.

이유:

- 핵심 요구가 Wiki와 같은 값인 점
- 원천을 불필요하게 둘 이상 runtime 의존시키지 않기 위함
- 두 source가 일시적으로 업데이트 시점이 어긋날 때 자동 충돌 해석을 만들지 않기 위함

## 9. 과거 구현 조사

과거 커뮤니티 앱 `TheHideoutAndroid`는 Item ID별로 `666654` 같은 6자리 문자열을 직접 하드코딩해 저장했다.

즉 그 앱도 Wiki/NoFood 값을 공식을 통해 재생성한 것이 아니라 **외부 표 데이터로 취급**했다.

과거 NoFoodAfterMidnight Google Sheet도 Armor Class 1~6 값을 명시적인 표 데이터로 제공했다.

현재 방식은 이 데이터를 소스 코드에 수작업 하드코딩하는 대신 현재 Wiki source에서 다시 내려받아 변환한다.

## 10. 최종 source 결정

`CONFIRMED`

### 사용하지 않음

- `penetrationPower / armorClass` ratio heuristic
- 임의 threshold table
- 임의 reference armor simulation을 Wiki 값이라고 부르는 방식
- 오래된 Google Sheet 값을 고정 하드코딩

### 사용

1. `json.tarkov.dev` — Ammo raw facts
2. 공식 Escape from Tarkov Wiki Ballistics — Class 1~6 0~6 rating optional enrichment

### 실패 의미

Wiki enrichment 실패 시:

- 기본 Game Content 업데이트는 계속 가능
- 해당 rating은 `unknown`으로 유지
- 이전 값이나 자체 계산값을 최신이라고 가장하지 않음
- User Progress에는 영향 없음

parser가 Wiki 구조를 이해할 수 없는 경우도 동일하게 fail-open for core / fail-closed for rating 처리한다.

## 11. 구현 검증 기준

구현은 최소 다음을 검증한다.

1. page/API 응답에서 6개 rating이 모두 0~6 범위인지 확인
2. 하나의 canonical Ammo에 둘 이상의 서로 다른 row가 매칭되면 해당 Ammo를 거부
3. 매칭되지 않은 Wiki row를 억지로 가까운 이름에 붙이지 않음
4. 전체 matching이 비정상적으로 적으면 source schema warning 발생
5. 대표 회귀 샘플:
   - `.50 AE JHP` → `6,1,0,0,0,0`
   - `.50 AE Copper Solid` → `6,6,6,5,3,2`
   - `.300 Blackout Whisper` → `6,4,2,1,0,0`
   - `.366 TKM AP-M` → `6,6,6,6,5,4`
   - `12/70 Flechette` → `6,6,6,5,5,5`
6. rating이 없는 Ammo는 UI에서 숫자를 발명하지 않음

## 12. 구현 시 색상 원칙

0~6은 숫자가 없어도 색만으로 의미가 전달되게 하지 않는다.

- 숫자를 항상 표시
- 색상은 보조 신호
- 색각 이상 사용자를 위해 값/텍스트만으로도 비교 가능
- unknown은 중립색 + `?`

최종 palette는 전체 dark theme과 함께 결정한다.
