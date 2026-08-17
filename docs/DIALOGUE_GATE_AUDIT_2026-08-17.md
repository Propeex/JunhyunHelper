# Dialogue Quest availability audit — 2026-08-17

## 결론

2026-08-17의 `json.tarkov.dev` live task feed를 regular / pve / pvp-season 세 GameMode에서 다시 점검했다.

현재 세 모드 모두 `otherRequirements`에 다음 두 종류만 존재한다.

- `globalVariable`: 162건
- `dialogue`: 12건

12개의 `dialogue` Quest는 세 GameMode에서 동일한 Quest ID 집합이다. 현재 raw task feed에서 이 12개는 모두 ordinary `taskRequirements`와 `traderRequirements`가 비어 있고, dialogue requirement ID는 자기 Quest 안에서만 한 번 등장한다. 따라서 공개 task feed만으로는 어떤 서버 이벤트 또는 이전 Quest가 해당 dialogue flag를 썼는지 역추적할 수 없다.

이 12건을 전부 `확인 필요`로 남기면 초반 진행표가 불필요하게 막히지만, 반대로 모든 dialogue condition을 무시하면 향후 다른 의미의 dialogue gate까지 잘못 통과시킬 수 있다. 따라서 **현재 검증된 정확한 12개 Quest ID에만 좁은 compatibility rule을 적용**한다.

---

## 현재 12개 Quest

| Quest ID | 현재 live 표시 이름 | 처리 |
|---|---|---|
| `657315ddab5a49b71f098853` | First in Line | 시작 Quest로 처리 |
| `657315e270bb0b8dba00cc48` | Burning Rubber | 시작 Quest로 처리 |
| `657315e4a6af4ab4b50f3459` | Saving the Mole | 시작 Quest로 처리 |
| `59689fbd86f7740d137ebfc4` | Operation Aquarius | Shortage 완료 + Lv.6 |
| `596a0e1686f7741ddf17dbee` | Supply Plans | Pharmacist 완료 + Lv.13 |
| `596b36c586f77450d6045ad2` | Supplier | Burning Rubber 완료 + Lv.5 |
| `5ac23c6186f7741247042bad` | Gunsmith - MP-133 | Saving the Mole 완료 + Lv.2 |
| `5ae448bf86f7744d733e55ee` | Make ULTRA Great Again | Only Business 완료 |
| `5ae448f286f77448d73c0131` | live feed: Fuel Crisis | Big Sale 완료 AND Make ULTRA Great Again 완료 |
| `5ae449c386f7744bde357697` | live feed: Pathfinder | Gratitude 완료 + Lv.30 |
| `5d2495a886f77425cd51e403` | Introduction | Gunsmith - MP-133가 Active + Lv.2 |
| `675c1570526ff496850895d9` | Passion for Ergonomics | Farming - Part 2 완료 |

`5ae448f286f77448d73c0131`과 `5ae449c386f7744bde357697`은 현재 translation feed의 표시명이 과거 canonical Quest 명칭과 다르다. compatibility는 이름이 아니라 **stable Quest ID**를 기준으로 적용하므로 번역명 변경에 영향을 받지 않는다.

---

## 판정 정책

`TarkovDialogueAvailabilityCompatibility`는 다음 조건을 모두 만족할 때만 동작한다.

1. Quest ID가 위의 검증된 12개 중 하나다.
2. 현재 Trader ID가 감사 당시의 Trader와 일치한다.
3. upstream `taskRequirements`가 여전히 0개다.
4. `UnsupportedAvailabilityRequirements`에 `dialogue`가 여전히 존재한다.
5. compatibility가 복원해야 하는 prerequisite Quest ID가 현재 GameMode의 Quest 집합에 모두 존재한다.

조건 하나라도 어긋나면 **아무 것도 추측하지 않고 원래 `dialogue`를 보존**한다.

upstream이 미래에 이 Quest의 정확한 `taskRequirements`를 직접 제공하기 시작하면 조건 3 때문에 compatibility가 자동으로 손을 떼며 source rule이 우선한다.

새로운 dialogue Quest가 추가되어도 allowlist에 없으므로 자동 통과하지 않고 기존처럼 `확인 필요(Indeterminate)`가 된다.

`dialogue` 이외의 unsupported condition이 같은 Quest에 추가되면 그 조건은 제거하지 않는다. 즉 compatibility가 해소한 fact만 제거하고 나머지 불명확성은 그대로 보존한다.

---

## 기존 Content snapshot 호환

이 변경은 저장 구조를 바꾸는 것이 아니라 이미 저장된 Quest availability metadata의 **해석을 정정**하는 변경이다.

따라서 Content schema v7을 유지한다. `ContentSnapshotStore.ReadAsync`에서 모든 readable snapshot에 compatibility를 메모리상 적용한다.

- 기존 content DB 삭제 불필요
- 강제 데이터 재다운로드 불필요
- `user.db` 변경 없음
- 사용자의 Quest/Inventory/Hideout 진행 기록 변경 없음

다음 정상 데이터 업데이트에서도 동일 compatibility가 candidate validation 전에 적용된다.

---

## Needed Items 영향

Quest 필요 아이템 계획은 Quest availability와 별도의 future-reachability 판정을 사용한다.

- 이번에 deterministic prerequisite로 복원된 12개는 일반 Quest graph와 동일하게 미래 필요 여부가 계산된다.
- 향후 unknown dialogue Quest가 생겨 `Indeterminate`로 남아도 future planner는 `IndeterminatePotential`을 계속 포함한다.
- 따라서 불명확한 availability 때문에 필요한 아이템을 잘못 `정리 가능`으로 판정하지 않는다.

---

## 구현 / 회귀 방지

구현:

- `src/JunhyunHelper.Infrastructure/TarkovJson/Quests/TarkovDialogueAvailabilityCompatibility.cs`
- `src/JunhyunHelper.Infrastructure/Content/TarkovContentBuildService.cs`
- `src/JunhyunHelper.Infrastructure/Storage/ContentSnapshotStore.cs`

회귀 테스트:

- `tests/JunhyunHelper.Tests/Infrastructure/TarkovDialogueAvailabilityCompatibilityTests.cs`

테스트는 최소한 다음을 고정한다.

- 검증된 root dialogue Quest만 deterministic 처리
- Supplier의 Burning Rubber Complete + level gate 복원
- Introduction의 `Active` prerequisite 보존
- unknown dialogue는 계속 Indeterminate
- future upstream `taskRequirements`가 생기면 compatibility가 덮어쓰지 않음
- prerequisite가 누락되면 fail-closed
- 함께 존재하는 다른 unsupported requirement는 보존
