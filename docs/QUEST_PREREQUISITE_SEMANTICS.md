# QUEST PREREQUISITE SEMANTICS — 특수 상인 접근과 선행조건

기준일: **2026-08-15**
상태: **CONFIRMED / IMPLEMENTED ON MAIN**

이 문서는 `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md` 후속 감사에서 확정한 Quest prerequisite 의미와 특수 상인 접근 모델을 기록한다. 기존 `DEC-010`의 자동 수락 원칙은 그대로 유지하며, `DEC-038`의 특수 상인 gate 보강 부분을 아래 규칙으로 정정한다.

## 1. 일반 taskRequirements 의미

- 서로 다른 `taskRequirements` 항목은 **AND**다.
- 한 requirement의 `status[]` 값은 **OR**다.
- `complete`는 해당 선행 Quest 완료를 뜻한다.
- `active`는 해당 선행 Quest가 진행 상태에 도달했음을 뜻한다.
- `failed`는 해당 선행 Quest 실패를 뜻한다.
- 준현 헬퍼는 별도의 `수주 가능` 상태를 만들지 않는다. 게임에서 받을 수 있는 Quest는 즉시 수락한 것으로 간주한다(`DEC-010`).
- 따라서 `active`로 이미 열린 후속 Quest는 선행 Quest가 나중에 완료되었다고 다시 잠기지 않는다.

## 2. Upstream 조건 우선

호환성 overlay는 upstream `json.tarkov.dev`가 이미 제공한 직접 prerequisite를 **덮어쓰거나 더 강한 상태로 변경하지 않는다**.

이 규칙은 2026-08-15 감사에서 발견한 BTR Driver 회귀를 막기 위해 확정했다.

- `Shipping Delay - Part 2`의 raw source는 `A Helping Hand = active`다.
- 과거 JunhyunHelper overlay는 이를 `complete`로 바꾸어 Quest를 실제보다 늦게 열었다.
- 수정 후 source에 직접 gate가 있으면 그대로 보존한다.

## 3. BTR Driver

BTR Driver의 최초 접근 gate는 `A Helping Hand`의 **Active** 의미다.

- source가 직접 `A Helping Hand` gate를 제공하면 그 상태 배열을 그대로 사용한다.
- BTR Driver 후속 Quest에 접근 gate가 반복되지 않은 경우에만 compatibility overlay가 `A Helping Hand = Active`를 추가한다.
- `DEC-010`에 따라 A Helping Hand가 수주 가능해지는 순간 Helper에서도 active로 간주한다.
- 이후 A Helping Hand를 완료해도 이미 열렸을 BTR Quest를 다시 잠그지 않는다.

## 4. Ref

Ref 접근은 현재 검증된 GameMode별 unlock Quest의 **Complete** gate를 사용한다.

- regular: 현재 regular Ref unlock Quest
- PvE: 현재 PvE Ref unlock Quest
- pvp-season: 해당 mode에 unlock Quest가 없으면 dangling gate를 만들지 않는다.
- source가 직접 gate를 제공하면 source를 보존하고 overlay는 중복 추가하지 않는다.

## 5. Lightkeeper는 일반 monotonic prerequisite가 아니다

Lightkeeper는 최초 접근 후에도 DSP transmitter 상태 변화로 접근을 잃고, Make Amends 계열을 통해 다시 복구할 수 있다. 이 사실은 `CompletedQuestIds`만으로 현재 접근권을 항상 복원할 수 없다.

따라서 Lightkeeper 후속 Quest마다 `Getting Acquainted = Complete`를 일반 prerequisite로 영구 주입하지 않는다.

대신 `QuestSpecialTraderAccessRequirement`로 별도 모델링한다.

- 최초 접근은 `Getting Acquainted = Complete`에서 자동 추론한다.
- 최초 unlock이 아직 끝나지 않았다면 사용자는 접근 상태를 수동으로 우회할 수 없다.
- Getting Acquainted가 완료 또는 영구 실패로 종결된 뒤, 실제 게임에서 접근권을 잃거나 복구한 경우에만 sparse user fact를 기록할 수 있다.
- 이 user fact는 `GameProfileSnapshot.SpecialTraderAccessOverrides`에 trader id별 bool로 저장한다.
- key가 없으면 자동 추론을 사용한다.
- `false`는 실제 접근 상실, `true`는 실제 접근 복구를 뜻한다.
- 평상시 사용자가 관리하는 설정이 아니며, 해당 특수 상황에서만 Quest 상세 UI에 contextual action으로 노출한다.
- 접근권을 잃은 상태는 Quest 경로의 영구 불가능을 뜻하지 않으므로 `Unavailable`이 아니라 **Locked**로 취급한다.

## 6. 실패 분기와의 관계

기존 실패 정책을 유지한다.

- 다른 Quest 완료로 확정되는 sibling failure는 자동 추론한다.
- 프로그램이 알 수 없는 비재시작형 영구 실패만 사용자 입력을 받는다.
- 재시작 가능한 레이드 실패는 영구 사용자 사실로 저장하지 않는다.
- `Getting Acquainted`의 실제 영구 실패는 기존 explicit failure 입력으로 표현할 수 있으며, 이후 Make Amends 복구 후 Lightkeeper access override를 `true`로 동기화할 수 있다.

## 7. Content 저장 호환성

- 최신 Content schema: **v6**
- 읽기 가능 last-known-good 범위: **v3~v6**
- v3~v5 snapshot은 네트워크 업데이트 없이 읽을 때 메모리에서 legacy special-trader overlay를 정규화한다.
  - BTR의 과거 강제 `Complete` gate → `Active` compatibility gate
  - Lightkeeper의 과거 `Getting Acquainted = Complete` 일반 gate → recoverable special trader access gate
  - Ref의 기존 Complete 의미는 유지
- `user.db` SQLite table schema는 계속 v1이다. `SpecialTraderAccessOverrides`는 optional JSON property이므로 기존 사용자 DB에 파괴적 migration이 필요 없다.

## 8. 데이터 업데이트 방어 규칙

`GameContentValidator`는 Quest graph에 대해 다음을 fatal validation으로 검사한다.

- prerequisite accepted status가 비어 있음
- self prerequisite
- 같은 Quest에 동일 prerequisite target 중복
- missing prerequisite Quest
- dependency cycle
- special trader access의 빈 status
- missing/mismatched trader
- self unlock Quest
- missing unlock Quest
- 같은 unlock Quest를 ordinary prerequisite와 special access로 동시에 평가

현재 live source에 이런 graph integrity 오류가 없더라도, 향후 게임 패치 데이터가 변할 때 조용한 오판 대신 candidate Content activation을 차단하기 위한 규칙이다.

## 9. 이번 변경에서 하지 않는 것

- 별도 `수주 가능` Quest 상태 추가
- 모든 Quest의 active/in-progress 상태를 사용자에게 수동 입력하게 함
- Battery Change의 의심스러운 upstream failure 데이터를 임의 보정
- 프로그램 릴리즈 생성

이번 변경은 Quest prerequisite semantics와 update resilience를 정정하는 개발 변경이며, public release는 별도 결정으로 진행한다.
