# Quest prerequisite / live data audit — 2026-08-15

## 결론

2026-08-15 현재 json.tarkov.dev의 regular / pve / pvp-season 데이터를 준현 헬퍼의 실제 importer와 validator로 다시 검증했다. 기존 `taskRequirements`의 상태 모델 자체는 유효하지만, v0.1.0에는 최신 데이터에서 availability를 너무 낙관적으로 보일 수 있는 두 가지 공백이 있었다.

1. Lightkeeper / BTR Driver / Ref 상인 접근 gate가 후속 Quest의 `taskRequirements`에 반복되지 않아 일부 Quest가 너무 일찍 열릴 수 있었다.
2. `globalVariable`, `dialogue`, timed availability가 Core에서 `Indeterminate`여도 제품이 이를 정확한 `Current`처럼 보이게 할 수 있었다.

특수 상인 gate와 opaque 조건 보존은 v0.1.1에서 도입했다. 이후 실사용에서 `Indeterminate → Current` fallback 때문에 `진행 중` Quest가 200개 이상으로 부풀어 보이는 문제가 확인되어 **v0.1.4부터 제품 표현 정책을 `확인 필요`로 변경**한다. 이 문서의 live 데이터 감사 결과는 그대로 유효하지만, v0.1.1 당시의 optimistic Current 제품 표현은 DEC-039에 의해 대체되었다.

## 현재 live Quest 구조

| GameMode | Quest | Objective | Quest item requirement |
|---|---:|---:|---:|
| regular | 517 | 1457 | 307 |
| pve | 513 | 1428 | 291 |
| pvp-season | 490 | 1392 | 286 |

`taskRequirements.status`에서 확인된 값은 `active`, `complete`, `failed`뿐이다. 현재 prerequisite graph에서 missing reference, self reference, duplicate prerequisite reference는 발견되지 않았다. Trader requirement는 `level`, `reputation`과 기존 지원 비교 연산자 `>=`, `<=`, `<` 범위였다.

## 특수 상인 접근 gate

현재 source는 Lightkeeper / BTR Driver / Ref가 제공하는 후속 Quest 대부분에 상인 접근 unlock Quest를 직접 prerequisite로 반복하지 않는다. JunhyunHelper는 current GameMode에 해당 unlock Quest가 존재할 때만 Complete requirement를 canonical prerequisite에 보강한다.

검증된 보강 결과:

| GameMode | Lightkeeper | BTR Driver | Ref |
|---|---:|---:|---:|
| regular | 14 | 19 | 20 |
| pve | 14 | 19 | 20 |
| pvp-season | 13 | 19 | 0 |

season에서 Ref unlock Quest가 current quest set에 없으므로 dangling prerequisite를 만들지 않는다. upstream이 이후 같은 prerequisite를 직접 제공하면 중복하지 않고 기존 requirement를 Complete로 강화한다.

## opaque otherRequirements

현재 각 mode에서 확인된 opaque availability 조건:

- `globalVariable`: 162
- `dialogue`: 12

upstream data manager도 일부 알려진 global variable만 의미 있는 구조로 변환하고 나머지는 opaque variable/comparison/value로 남긴다. JunhyunHelper는 이 의미를 추측하지 않는다.

현재 제품 정책(v0.1.4 후보):

```text
Core Indeterminate
→ UI: 확인 필요
→ 정확한 Current(진행 중) 수치/기본 필터에서 제외
→ Map Current Quest sidebar에서 제외
→ QuestWorkspace.Problems / 자동 판정 제한에 원인 보존
```

`확인 필요`를 거짓으로 `잠김` 처리하지도 않는다. 사용자가 실제 게임에서 Quest를 받았거나 완료한 사실을 알고 있으면 수동 완료할 수 있고, 비재시작형 영구 실패 동기화가 필요한 Quest라면 수동 실패도 허용한다. Future Needed Items는 기존 `IndeterminatePotential` 보수 정책을 유지하여 잠재적으로 필요한 아이템을 안전하게 보호한다.

## timed availability

각 mode에 `availableDelaySecondsMin/Max`가 있는 Quest가 정확히 13개 존재한다. 범위는 수 초부터 24시간까지 포함한다.

JunhyunHelper는 min/max를 canonical metadata로 저장하지만 자동 countdown은 하지 않는다. Helper에서 사용자가 완료 버튼을 누른 시각은 실제 Tarkov 완료 시각이 아닐 수 있으므로 그 시각을 기준으로 9시간/24시간 잠금을 걸면 잘못된 상태를 만들 수 있기 때문이다. timed Quest는 `availabilityDelay` unresolved condition으로 보존하며 v0.1.4부터 UI에서는 `확인 필요`로 분리된다.

## fail condition 점검

현재 `taskStatus` fail condition은 Complete 기반이며 기존 completion-triggered failure 모델로 처리 가능하다. 그 외 `useItem`, `visit`, `shoot`, `extract`, `traderStanding`, `plantItem` 등은 restartable 여부와 함께 기존 보수적 manual permanent-failure 정책을 유지한다. 자동으로 실패를 추정하지 않는다.

## 전체 live content 검증

GitHub Actions run `31819603896`, job `94829428837`에서 실제 product importer/validator로 세 GameMode를 전부 빌드했다.

| 항목 | regular | pve | pvp-season |
|---|---:|---:|---:|
| Items | 5312 | 5312 | 5312 |
| Traders | 16 | 16 | 16 |
| Maps | 17 | 17 | 17 |
| Quests | 517 | 513 | 490 |
| Hideout stations | 26 | 26 | 26 |
| Ammo | 200 | 200 | 200 |
| importer warnings | 0 | 0 | 0 |
| validation | valid | valid | valid |

당시 live data 감사 run에서 Desktop Release build 성공, 자동 테스트 173 passed / 0 failed였다. 이후 v0.1.2~v0.1.4 기능/회귀 테스트가 추가되었으며 각 릴리즈는 별도의 최신 release gate를 기준으로 한다.

## Content schema

Current schema는 v5다. v3/v4 content DB는 offline last-known-good로 계속 읽을 수 있다. v0.1.1의 availability metadata와 특수 상인 gate를 받으려면 v5 content가 필요하다. 이미 v0.1.1 이상에서 v5 content를 사용 중인 사용자는 **v0.1.3 → v0.1.4에서 필수 데이터 업데이트가 없다.** `확인 필요` 분리는 기존 v5 metadata를 제품 UI에서 다르게 해석하는 변경이다. `user.db`는 이 변경과 독립이며 삭제/덮어쓰기하지 않는다.
