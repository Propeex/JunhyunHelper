# QUEST FAILURE ANALYSIS — 퀘스트 실패/분기 데이터 분석

기준일: **2026-08-08**

이 문서는 준현 헬퍼가 퀘스트 실패와 분기를 어떻게 다뤄야 하는지 결정하기 위해, 현재 사용하는 `json.tarkov.dev` regular 모드의 최신 task 원천을 실제 importer 관점에서 점검한 결과를 기록합니다.

## 결론

대형 분기 관리 시스템은 필요하지 않습니다.

준현 헬퍼는 다음 원칙으로 충분합니다.

1. 다른 퀘스트 완료로 확정되는 실패는 기존 `완료 퀘스트` 사실에서 자동 계산합니다.
2. 아직 결과가 결정되지 않은 실패 분기는 `판정 문제`가 아니라 **미래에 가능한 정상 경로**로 남깁니다.
3. 실패가 확정되면 불가능해진 분기의 미래 필요 아이템을 즉시 제외하고, 이미 모아둔 초과 보유량은 `정리 필요`로 계산합니다.
4. 게임 데이터만으로 알 수 없는 **비재시작형 영구 실패**에 한해서만 사용자가 `실패 처리`를 직접 입력합니다.
5. 재시작 가능한 레이드 실패는 영구 사용자 사실로 저장하지 않습니다.

## 최신 원천 규모

regular 모드 기준:

- 퀘스트: **510개**
- task prerequisite 관계: **607개**

선행 퀘스트 상태 조합:

| 요구 상태 | 관계 수 | 해석 |
| --- | ---: | --- |
| Complete | 549 | 일반적인 완료 선행 조건 |
| Active + Complete | 23 | 선행 퀘스트를 시작했거나 완료하면 충족 |
| Complete + Failed | 19 | 성공 또는 실패로 종료되면 충족 |
| Active | 11 | 선행 퀘스트가 진행 가능/진행 상태이면 충족 |
| Failed | 4 | 선행 퀘스트가 실패해야 열리는 분기 |
| Active + Complete + Failed | 1 | 어떤 진행/종료 결과도 허용 |

`Failed`가 포함된 선행 관계는 총 **24개**이며, 그중 실제로 실패 여부가 분기 조건이 되는 `Failed only`는 **4개**뿐입니다.

## Failed-only 4건

현재 데이터에서 확인된 대표 구조:

- `Trust Regain` ← `Out of Curiosity` 실패
- `Loyalty Buyout` ← `Chemical - Part 4` 실패
- `No Offence` ← `Big Customer` 실패
- `Hot Wheels - Let's Try Again` ← `Hot Wheels` 실패

Chemical Part 4 계열은 한 분기를 완료하면 다른 분기의 실패가 게임 데이터의 `taskStatus` 실패 조건으로 확정되므로 **추가 사용자 입력 없이 자동 계산할 수 있습니다.**

예:

```text
Chemical - Part 4 완료
→ Out of Curiosity 실패 자동 확정
→ Big Customer 실패 자동 확정
→ 실패한 두 분기의 요구 아이템 제외
→ 실패 후 열리는 회복/보상 퀘스트는 가능 경로로 유지
```

## failConditions 전체

현재 원천에서 확인된 fail condition:

- 총 fail condition: **50개**
- fail condition이 있는 퀘스트: **38개**

유형별:

| 유형 | 수 | 처리 방향 |
| --- | ---: | --- |
| taskStatus | 35 | 다른 퀘스트 완료 사실에서 자동 추론 가능 |
| extract | 5 | 주로 재시작형 레이드 실패 — 영구 저장 안 함 |
| shoot | 5 | 재시작 여부에 따라 판단; 비재시작형만 수동 입력 후보 |
| traderStanding | 2 | 비재시작형이면 수동 입력 후보 |
| useItem | 1 | 재시작형이면 영구 저장 안 함 |
| visit | 1 | 재시작형이면 영구 저장 안 함 |
| plantItem | 1 | 비재시작형이면 수동 입력 후보 |

`taskStatus` 실패는 **35개 조건 / 23개 퀘스트**이며, 모두 다른 퀘스트의 완료를 트리거로 사용합니다. 따라서 기존의 `CompletedQuestIds`만으로 결정론적으로 계산할 수 있습니다.

## 수동 실패 입력이 필요한 경우

원천의 실패 조건이 있고 `restartable = false`인데, 현재 프로그램이 그 실제 게임 사건을 외부 데이터나 다른 사용자 사실에서 결정할 수 없는 경우만 `실패 처리`를 노출합니다.

현재 분석에서 대표적으로 실제 의미가 있는 경우:

- `Colleagues - Part 3`: 비재시작형 `shoot` 실패
- `Hot Wheels`: 비재시작형 `plantItem` 실패

`Getting Acquainted`, `Make Amends`처럼 비재시작형 traderStanding 실패도 데이터 구조상 수동 입력 후보지만, 직접 필요 아이템이 없는 경우가 있어 Needed Items 영향은 제한적입니다.

중요한 점은 **이 예외 때문에 모든 퀘스트에 실패 상태 UI를 만들지 않는 것**입니다. canonical 데이터가 정말 영구 실패 입력을 필요로 하는 퀘스트에서만 버튼을 노출합니다.

## 상태 의미

Quest 화면의 정상 상태를 다음과 같이 구분합니다.

- `진행 중(Current)`: 지금 수행 가능한 미완료 퀘스트
- `잠김(Locked)`: 레벨/선행 진행 등 앞으로 충족 가능한 조건 때문에 아직 열리지 않은 퀘스트
- `사용 불가(Unavailable)`: 현재 캐릭터/확정된 진행에서 더 이상 수행할 수 없음이 증명된 퀘스트
- `완료(Completed)`: 사용자가 완료 사실을 입력한 퀘스트
- `판정 문제(Indeterminate)`: 데이터 누락, 미지원 의미 등으로 프로그램이 상태를 안전하게 결정할 수 없는 경우

`Failed only` 선행조건이 아직 결정되지 않았다는 이유만으로 `Indeterminate`로 만들지 않습니다. 실패 가능성이 살아 있는 동안은 미래 계획에서 정상적인 가능한 분기입니다.

## Needed Items와의 연결

분기 결정 전:

```text
가능한 미래 분기 A 요구량
+
가능한 미래 분기 B 요구량
+
가능한 미래 분기 C 요구량
→ 모두 보관 계획에 포함
```

분기 결정 후:

```text
A 완료
→ B/C 영구 실패 자동 계산
→ B/C 요구량 제거
→ A 이후 경로만 유지
→ 보유량 > 새 미래 필요량이면 정리 필요
```

이 방식은 사용자가 미래를 대비해 아이템을 넓게 모으되, 경로가 실제로 닫혔을 때 불필요한 물건을 계속 보관하지 않도록 하는 Needed Items의 목적과 일치합니다.

## 업데이트 내구성

`FailedQuestIds`는 사용자 사실이므로 패치 업데이트 때 임의 삭제하지 않습니다.

다만 새 Game Content에서 어떤 퀘스트가 더 이상 `비재시작형 수동 실패 대상`이 아니게 되면, 과거의 실패 기록을 새 규칙에 강제로 적용하지 않습니다.

이후 새 규칙에서 해당 퀘스트를 완료하면:

- 완료 사실 저장
- 같은 퀘스트의 낡은 explicit failure 기록 제거
- 다른 사용자 진행/보유 데이터는 보존

즉 **사용자 데이터를 패치가 삭제하지 않으면서도, 오래된 상태가 새 게임 규칙을 오염시키지 않도록** 처리합니다.

## 회귀 테스트 기준

다음 동작을 반드시 테스트합니다.

- failed-only 선행조건은 결과 결정 전 Locked/Potential이지 Indeterminate가 아님
- explicit permanent failure가 failed-only 후속 분기를 활성화함
- 다른 분기 완료가 sibling 실패를 자동 추론함
- 자동 실패 후 불가능한 sibling 요구량이 Needed Items에서 빠짐
- 실패 후 가능한 recovery quest 요구량은 남음
- explicit failure / undo failure가 다른 프로필 사실을 손상시키지 않음
- restartable failure는 explicit permanent failure로 저장할 수 없음
- content round-trip에서 failure 조건 보존
- 없는 failure trigger quest는 content validation 실패
- 패치 후 stale explicit failure가 새 규칙에서 완료를 방해하지 않음

## 설계 이유

이 구조는 퀘스트 분기를 별도의 복잡한 상태 머신으로 만들지 않습니다.

준현 헬퍼가 이미 알고 있는 사실인 `퀘스트 완료`, 최신 Game Content의 `failConditions`, 그리고 정말 필요한 소수의 `영구 실패 사용자 입력`만 사용합니다.

따라서 프로그램은 계속 다음 원리를 유지합니다.

> **알 수 있는 것은 데이터와 명시 규칙으로 자동 계산하고, 알 수 없는 실제 사용자 사실만 최소한으로 입력받는다.**
