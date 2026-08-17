# Decision — current-version trader task-pool runtime compatibility

날짜: 2026-08-17
상태: **CONFIRMED / v0.1.10 REFINED**
관련 기존 결정: `DEC-038`, `DEC-039`, `DEC-044`

## 결정

EFT `globalVariable` Quest availability의 기본 원칙은 계속 **exact observed profile value 우선 / 증명할 수 없는 fact는 추측하지 않음**이다.

다만 current EFT 1.1 live dataset에서 `globalVariable` 162 Quest가 정확히 27개의 trader-local staged task-pool 변수로 묶이고, LL2~LL4에 대해 같은 trader의 direct loyalty-level seed batch가 current source에서 교차검증되는 구조는 제품의 current Quest 표시에서 **버전 고정 compatibility**로 복원할 수 있다.

v0.1.10에서는 LL1도 진행값을 역산하는 대신, **현재 LL1 + 해당 trader completed Quest 0개인 pristine 상태**에 한해 current counter=0이라는 최소 사실만 추가로 사용한다.

이 compatibility는 generic inference가 아니다. 정확히 감사된 current-version shape가 그대로일 때만 허용한다.

## 우선순위

1. exact current EFT profile `Variables` 값이 있으면 그 값이 항상 권위값이다.
2. exact 값이 없고 audited current-version shape가 완전히 일치하면 LL2~LL4 task-pool runtime reconstruction을 허용한다.
3. audited LL1 pool이고 현재 trader LL=1이며 해당 trader completed Quest=0이면 pristine current value=0을 허용한다.
4. 위 조건이 성립하지 않으면 기존 `Indeterminate / 확인 필요`로 fail closed한다.

## 허용 조건

각 variable에 대해 다음이 모두 일치해야 한다.

- exact variable ID
- exact trader ID
- exact pool Quest count
- 각 Quest가 해당 variable 하나의 `>= threshold` requirement만 가짐
- exact threshold set
- 새로운 ordinary task/trader/unsupported availability requirement가 없음
- LL2+의 direct same-trader loyalty seed Quest count가 audit 값과 일치

하나라도 달라지면 compatibility를 적용하지 않는다.

## LL2~LL4 현재값 재구성

검증된 pool에 대해:

- 현재 trader LL이 해당 pool stage보다 낮음 → current pool value = 0
- 현재 trader LL이 stage 이상 → `완료한 direct LL seed Quest 수 + 완료한 해당 pool Quest 수`

합성 값은 Quest current availability를 평가하는 runtime profile copy에만 넣고 `user.db`에는 저장하지 않는다.

## LL1 경계

LL1 task-pool 48 Quest에는 progressed counter 전체를 증명하는 public direct-LL seed/write rule이 없다.

따라서 여전히:

- 진행된 LL1 값을 완료 Quest 수만으로 임의 재구성하지 않는다.
- trader가 LL2 이상인 상태에서 missing LL1 값을 0으로 되돌려 추정하지 않는다.
- 해당 trader completed Quest가 하나라도 있으면 pristine-zero inference를 사용하지 않는다.
- exact profile variable 값이 들어오면 정상 exact evaluator가 처리한다.

단, audited LL1 pool에서 **현재 trader가 LL1이고 해당 trader completed Quest가 0개**라면 아직 그 trader의 Quest completion에 의해 진행된 사실이 없으므로 current counter=0을 안전한 초기 상태로 취급한다.

이 예외는 새/초기 profile의 false `확인 필요`를 줄이기 위한 최소 규칙이며 progressed LL1의 write semantics를 발명하지 않는다.

## Needed Items와 분리

Quest current UI에서 false `확인 필요`를 줄이는 것과 Item cleanup 안전성은 별개다.

`FutureNeededItemsPlanner`는 current UI runtime reconstruction을 낙관적으로 전파하지 않고 기존 conservative future reachability를 유지한다. missing profile-variable future fact는 계속 `IndeterminatePotential`로 보호한다.

따라서 이 결정은 필요한 미래 Item을 잘못 `정리 가능`으로 만드는 방향으로 사용하지 않는다.

## 이유

v0.1.8에서는 canonical read-side condition을 정확히 보존했지만 exact profile value가 없다는 이유만으로 162 usage 전체가 structural unknown이 되어 사용자에게 지나치게 많은 `확인 필요`를 노출했다.

2026-08-17 감사 결과:

- tasks: 517
- globalVariable Quest: 162
- unique variables: 27
- audited LL2~LL4 Quest: 114
- LL1 Quest: 48
- availability delay: 13

LL2~LL4 compatibility 이후 raw source-level unresolved ceiling은 61(48 + 13)이다. 이는 profile-independent 구조 수치다. 실제 profile에는 exact variable, 완료/잠김 상태, 그리고 v0.1.10 pristine LL1 zero가 적용되므로 실제 UI `확인 필요`는 더 작아질 수 있다.

이는 source가 제공하지 않는 generic server write rule을 발명하지 않으면서도, current source와 사용자가 입력한 명백한 profile 상태에서 증명 가능한 사실만 제품에 이용하는 절충이다.

## 장기 방향

향후 scanner/importer가 EFT profile payload에서 exact `Variables` dictionary와 completion timestamps를 안전하게 가져올 수 있으면 exact 값 사용이 우선하며 compatibility 의존도는 줄어든다.

새 variable ID, 새 threshold 구조, seed count 변경은 자동 학습/추론하지 않는다. 재감사 전까지 fail closed한다.

## 근거 문서

- `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`
- `docs/FEEDBACK_FIXES_2026-08-17.md`
- `docs/RELEASE_0.1.9.md`
- `docs/RELEASE_0.1.10.md`
