# Decision — current-version trader task-pool runtime compatibility

날짜: 2026-08-17  
최종 정제: 2026-08-31  
상태: **CONFIRMED / v1.12.0 REFINED**  
관련 기존 결정: `DEC-038`, `DEC-039`, `DEC-044`

## 결정

EFT `globalVariable` Quest availability의 기본 원칙은 계속 **exact observed profile value 우선 / 증명할 수 없는 fact는 추측하지 않음**이다.

다만 current EFT 1.1 live dataset에서 `globalVariable` Quest가 정확히 감사된 trader-local staged task-pool 변수로 묶이고, LL2~LL4에 대해 같은 trader의 direct loyalty-level seed batch가 current source에서 교차검증되는 구조는 제품의 current Quest 표시에서 **버전 고정 compatibility**로 복원할 수 있다.

v0.1.10에서는 LL1도 진행값을 역산하는 대신, **현재 LL1 + 해당 trader completed Quest 0개인 pristine 상태**에 한해 current counter=0이라는 최소 사실만 추가로 사용했다.

v1.12.0에서는 사용자 실사용에서 Quest를 진행한 뒤 `확인 필요`가 수십 개로 폭증한 회귀를 다시 감사했다. 캡처의 `확인 필요 49` 중 **48개가 current audit의 LL1 task-pool Quest 수와 정확히 일치**했고, EFT 1.1 side-task 규칙은 같은 loyalty group을 진행하는 것 외에 **다음 Trader Loyalty Level에 도달하는 것도 다음 side-task group의 대체 unlock 조건**으로 정의한다.

따라서 audited stage보다 현재 Trader LL이 이미 높다면 그 과거 stage의 hidden counter exact 값이 없어도 **그 stage의 모든 availability threshold가 충족되었다는 사실은 증명 가능**하다. 이때 사용하는 값은 숨은 서버 counter의 exact 값이 아니라 current Quest availability를 위한 **effective floor**다.

이 compatibility는 generic inference가 아니다. 정확히 감사된 current-version shape가 그대로일 때만 허용한다.

## 우선순위

1. exact current EFT profile `Variables` 값이 있으면 그 값이 항상 권위값이다.
2. exact 값이 없고 audited current-version shape가 완전히 일치하면 current-stage LL2~LL4 task-pool runtime reconstruction을 허용한다.
3. audited LL1 pool이고 현재 trader LL=1이며 해당 trader completed Quest=0이면 pristine current value=0을 허용한다.
4. audited pool stage보다 현재 Trader LL이 높으면 해당 과거 stage의 모든 threshold를 충족하는 effective availability floor를 허용한다.
5. 위 조건이 성립하지 않으면 기존 `Indeterminate / 확인 필요`로 fail closed한다.

## 허용 조건

각 variable에 대해 다음이 모두 일치해야 한다.

- exact variable ID
- exact trader ID
- exact pool Quest count
- 각 Quest가 해당 variable 하나의 `>= threshold` requirement만 가짐
- exact threshold set
- 새로운 ordinary task/trader/unsupported availability requirement가 없음
- LL2+ current-stage reconstruction에는 direct same-trader loyalty seed Quest count도 audit 값과 일치

하나라도 달라지면 compatibility를 적용하지 않는다.

## stage별 current availability 계산

검증된 pool에 대해:

- `현재 trader LL < pool stage` → effective value = 0
- `현재 trader LL == pool stage == LL1`
  - same-trader completed Quest 0 → 0
  - 하나 이상 완료 → exact LL1 write semantics를 알 수 없으므로 일반적으로 unknown
  - 단, 완료된 해당 gated Quest 자체가 required threshold 도달의 witness가 되는 경우 equal/lower threshold의 true만 증명 가능
- `현재 trader LL == pool stage >= LL2` → `완료한 direct LL seed Quest 수 + 완료한 해당 pool Quest 수`
- `현재 trader LL > pool stage` → `max(audited thresholds)`를 effective availability floor로 사용

합성 값은 Quest current availability를 평가하는 runtime profile copy에만 넣고 `user.db`에는 저장하지 않는다.

### 과거 stage floor가 안전한 이유

이 규칙은 “숨은 server variable 값이 실제로 max threshold다”라고 추정하지 않는다.

제품이 필요한 질문은 **현재 이 과거 stage의 Quest gate가 열렸다고 판단할 수 있는가**다. EFT 1.1 side-task 규칙 자체가 다음 Trader LL 도달을 next-group unlock의 대체 조건으로 제공하므로, 현재 LL이 stage를 이미 넘어섰다면 과거 stage의 `>= threshold` gate를 `확인 필요`로 되돌리는 것은 사용자 진행과 게임 의미에 맞지 않는다.

따라서 max threshold는 persistence나 game-fact 재구성이 아니라 그 stage의 boolean availability를 표현하기 위한 runtime-only floor다.

## LL1 경계

LL1 task-pool에는 progressed counter 전체를 증명하는 public direct-LL seed/write rule이 없다.

따라서 현재 trader가 **여전히 LL1인 동안**은 계속 다음을 지킨다.

- 진행된 LL1 값을 완료 Quest 수만으로 임의 재구성하지 않는다.
- 해당 trader completed Quest가 하나라도 있으면 pristine-zero inference를 사용하지 않는다.
- exact profile variable 값이 들어오면 정상 exact evaluator가 처리한다.

단, audited LL1 pool에서 현재 trader가 LL1이고 해당 trader completed Quest가 0개라면 아직 그 trader의 Quest completion에 의해 진행된 사실이 없으므로 current counter=0을 안전한 초기 상태로 취급한다.

그리고 trader가 LL2 이상으로 올라간 순간에는 **과거 LL1 counter의 exact 숫자를 복원하지 않고** 위 past-stage availability floor 규칙을 사용한다.

## Needed Items와 분리

Quest current UI에서 false `확인 필요`를 줄이는 것과 Item cleanup 안전성은 별개다.

`FutureNeededItemsPlanner`는 current UI runtime compatibility를 낙관적으로 전파하지 않고 기존 conservative future reachability를 유지한다. missing profile-variable future fact는 계속 `IndeterminatePotential`로 보호한다.

따라서 이 결정은 필요한 미래 Item을 잘못 `정리 가능`으로 만드는 방향으로 사용하지 않는다.

## v1.12.0 사용자 회귀와 검증 계약

사용자 실사용 증상:

```text
fresh profile → 확인 필요 0
Quest 일부 진행 / trader 진행 → 확인 필요 수십 개
실사용 캡처 → 확인 필요 49
```

current audited LL1 task-pool count:

```text
48
```

이 일치성을 root-cause 증거로 사용하고 다음 결정적 회귀를 고정한다.

- LL1 pool + current trader LL2 → 과거 LL1 threshold 전부 current availability에서 satisfied
- LL2 pool + current trader LL3 → 과거 LL2 threshold 전부 current availability에서 satisfied
- current LL1 + no same-trader completion → zero
- current LL1 + same-trader completion + missing exact variable → 일반적으로 fail-closed 유지
- exact profile variable → compatibility보다 항상 우선
- audited shape drift → synthetic value 생성 금지

## 장기 방향

향후 scanner/importer가 EFT profile payload에서 exact `Variables` dictionary와 completion timestamps를 안전하게 가져올 수 있으면 exact 값 사용이 우선하며 compatibility 의존도는 줄어든다.

새 variable ID, 새 threshold 구조, seed count 변경은 자동 학습/추론하지 않는다. 재감사 전까지 fail closed한다.

## 근거

- `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md` 또는 해당 날짜의 current-version audit 기록
- EFT 1.1 official task-system description: side-task group은 current group 진행 또는 next Trader Loyalty Level 도달로 다음 group이 unlock됨
- 사용자 2026-08-31 v1.11.4 실사용 캡처
- `tests/JunhyunHelper.Tests/Quests/QuestTaskPoolVariableCompatibilityTests.cs`
