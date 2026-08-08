# BALLISTICS EFFECTIVENESS ANALYSIS — 방탄복 클래스 효율값 조사

검증일: **2026-08-08**

상태: `MECHANICS FORMULA VERIFIED / WIKI 0–6 DERIVATION NOT YET PROVEN`

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

## 7. 왜 Wiki 0~6을 아직 바로 계산할 수 없는가

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

## 8. 과거 구현 조사

과거 커뮤니티 앱 `TheHideoutAndroid`는 Item ID별로 `666654` 같은 6자리 문자열을 직접 하드코딩해 저장했다.

즉 그 앱도 Wiki/NoFood 값을 공식을 통해 재생성한 것이 아니라 **외부 표 데이터로 취급**했다.

이 사실만으로 공식이 없다고 단정할 수는 없지만, 적어도 널리 공개된 단일 변환식이 있었다는 근거는 아니다.

## 9. 현재 결론

### CONFIRMED

- 현재 게임의 penetration chance 공식은 재현 가능하다.
- 현재 게임의 armor durability damage 공식도 재현 가능하다.
- projectile 수까지 포함한 반복 방어구 손상 시뮬레이션을 만드는 데 필요한 핵심 게임 공식은 상당 부분 확인됐다.
- Wiki 0~6은 단순 penetrationPower 구간표가 아니다.

### NOT YET PROVEN

- Wiki/NoFood 0~6을 **정확히 같은 값으로** 출력하는 공개된 reference armor / simulation / classification algorithm.

## 10. 다음 결정 규칙

1. NoFood/eft-ammo가 사용하는 정확한 reference/simulation 방법을 더 조사한다.
2. 방법이 확인되면 해당 공식을 준현 헬퍼 Core에 순수 함수 + 회귀 fixture로 구현한다.
3. 현재 eft-ammo 표의 샘플 Ammo를 대량 대조해 0~6 결과가 일치하는지 검증한다.
4. 일치할 때만 Ammo 표에 `Class 1~6` 색상 cell을 노출한다.
5. 공식이 끝내 공개/검증되지 않으면 임의 공식을 만들지 않는다.
   - 그 경우 정확한 외부 rating source를 validated overlay로 사용하는 방안과
   - 실제 게임 공식 기반 `초기 관통 확률`을 별도 정보로 표시하는 방안을 분리해 검토한다.

## 11. 구현 시 색상 원칙

0~6은 숫자가 없어도 색만으로 의미가 전달되게 하지 않는다.

- 숫자를 항상 표시
- 색상은 보조 신호
- 색각 이상 사용자를 위해 값/텍스트만으로도 비교 가능

최종 palette는 전체 dark theme과 함께 결정한다.
