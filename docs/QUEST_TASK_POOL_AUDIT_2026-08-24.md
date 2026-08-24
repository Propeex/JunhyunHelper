# Quest task-pool live audit — 2026-08-24

상태: **AUDITED / v1.5.0 INPUT**

## 목적

v1.4.3 이후 Quest 화면에서 `확인 필요`가 다시 증가할 수 있는 원인을 최신 `json.tarkov.dev` task payload 기준으로 재감사했다.

기존 runtime compatibility는 2026-08-17 구조와 퀘스트 수, threshold 집합, 상인, LL seed 개수가 모두 일치할 때만 synthetic profile-variable 값을 허용한다. 따라서 Tarkov 데이터가 조금만 변해도 해당 pool 전체가 안전하게 fail-closed 된다.

## 실행 근거

PR #172 temporary Windows live-data audit workflow:

- run `32684278245`
- result: SUCCESS
- 대상 mode: `regular`, `pve`, `pvp-season`
- source: current `https://json.tarkov.dev/<mode>/tasks` and `/items`

## 현재 공통 shape

세 mode 모두:

- unique task-pool variables: **27**
- `globalVariable` requirements: **164**
- `dialogue` quests: **12**
- delayed quests: **13**
- `studyItems` availability requirements: **0**

따라서 `dialogue` 증가가 회귀 원인은 아니다. 기존 compatibility가 대상으로 삼던 12개 dialogue quest set은 그대로 유지된다.

## 2026-08-17 대비 확인된 drift

### Pool membership

1. Prapor LL3 — variable `6a32651a811905ed0cac0973`
   - old expected: 6 quests
   - current: **7 quests**
   - thresholds remain `[1, 3]`
   - trader and availability shape unchanged
   - current extra quest: `66ab970848ddbe9d4a0c49a8`

2. Mechanic LL2 — variable `6a3c0fefbea2d2ad581c090b`
   - old expected: 10 quests
   - current: **11 quests**
   - thresholds remain `[1, 3, 5]`
   - trader and availability shape unchanged
   - current extra quest: `67a09636b8725511260bc421`

이 두 변화가 `globalVariable` requirement 총계가 162 → **164**로 증가한 원인이다.

### Direct LL seed batches

3. Ragman LL3 — variable `6a4b9c9a60b56d421cceea18`
   - old expected seed count: 5
   - current: **6** in all three modes

4. Skier LL4 — variable `6a5a1192efde11cc7105b18f`
   - old expected seed count: 4
   - current: **5** in all three modes

5. Skier LL2 — variable `6a5a111de1f417ac80a163e5`
   - old expected seed count: 3
   - current Regular: **4**
   - current PvE: **4**
   - current PvP Season: **3**

Skier LL2는 이제 mode-dependent contract로 검증해야 한다. 하나의 전역 seed count로 완화해서는 안 된다.

## 안전성 판단

이번 수정은 structural validation을 느슨하게 만드는 것이 아니다.

- exact profile `Variables` 값이 있으면 항상 그 값이 최우선이다.
- current mode의 audited quest count/threshold/trader/seed count가 모두 일치할 때만 runtime reconstruction을 허용한다.
- Skier LL2는 `GameMode`별 expected seed count를 검증한다.
- 새 variable, 새 threshold, 새로운 availability 조건, 새로운 구조 변화는 자동 추론하지 않는다.
- 구조가 다시 달라지면 기존처럼 `Indeterminate / 확인 필요`로 fail-closed 한다.

## v1.5.0 변경값

- Prapor LL3 pool count: `7`
- Mechanic LL2 pool count: `11`
- Ragman LL3 seed count: `6`
- Skier LL4 seed count: `5`
- Skier LL2 seed count:
  - Regular `4`
  - PvE `4`
  - PvP Season `3`

관련 구현:

- `src/JunhyunHelper.Core/Quests/QuestTaskPoolVariableCompatibility.cs`
- `tests/JunhyunHelper.Tests/Quests/QuestTaskPoolVariableCompatibilityTests.cs`

## Scanner market side observation

같은 live audit에서 current item payload는 5,312 items를 제공했고 Regular 기준:

- `sellToTrader` present: **4,790**
- legacy/raw `traderPrices` present: **5,312**
- positive `avg24hPrice`: **3,558**

따라서 v1.5.0 Scanner parser는 `sellToTrader`를 우선적인 explicit trader-offer shape로 지원하되 legacy representation도 호환해야 한다.
