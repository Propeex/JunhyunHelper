# FINAL AUDIT — 2026-08-18

## 목적

v0.1.12 공개 후 기능 추가를 잠시 중단하기 전에 준현 헬퍼의 현재 상태를 제품/데이터/코드/런타임 관점에서 최종 점검한다.

이번 감사는 새 기능 구현을 위한 작업이 아니다. 다음을 확인하는 maintenance audit이다.

1. Quest 판정 정확성
2. Quest objective → 제출 Item requirement 변환 정확성
3. Future Needed Items / Inventory cleanup 안전성
4. Hideout requirement 계산
5. 온라인 Content update / 저장 / rollback 안전성
6. UI 렌더 계약과 런타임 smoke
7. 불필요·중복·legacy 코드 및 기술부채
8. current live Tarkov source와 current compatibility rule의 구조 일치 여부

감사 중 product code는 변경하지 않았다.

---

## 최종 판정

**현재 v0.1.12는 기능 개발을 중단하고 동결해도 되는 상태다.**

- 현재 지원하는 `json.tarkov.dev` task/hideout/item domain에서 즉시 수정이 필요한 blocking correctness bug는 발견하지 못했다.
- Quest → 제출 Item → Future Needed Items → cleanup 계산은 현재 live 데이터에서 구조적 결손 없이 통과했다.
- current live Regular / PvE / PvP Season 모두 canonical build/validation에 성공했다.
- 공개 v0.1.12가 이미 통과한 rendered UI / Main Map / Factory / MiniMap smoke 계약도 그대로 유효하다.
- 다만 **Tarkov 1.0 Story Chapters가 현재 주 데이터 소스의 ordinary task feed 범위 밖이라는 product coverage gap**이 있다. 따라서 'Tarkov의 모든 진행 요구 Item을 완전하게 보호한다'고까지 주장할 수는 없다.

자체 평가: **8.7 / 10 (A-)**

현재 제품을 사용하는 데 문제가 있어서 8.7인 것이 아니라, 지원 데이터 범위의 완전성과 legacy Map 유지보수성까지 제품 전체 기준으로 포함한 점수다.

---

## 1. Quest 판정

### 판정 결과

**좋음 / 유지 권장**

현재 Quest availability는 다음 원칙을 일관되게 지킨다.

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 accepted status = OR
- Complete / Active / Failed 의미 보존
- available Quest는 별도 Available state 없이 Current/Active 취급(DEC-010)
- faction/edition 같은 영구 배제는 Unavailable
- level/trader LL 같은 현재 미충족은 Locked
- 필요한 fact가 없거나 upstream 의미를 증명할 수 없으면 Indeterminate(`확인 필요`)
- completed/failure branch precedence 처리
- Special trader access는 ordinary prerequisite와 분리
- source ordinary prerequisite가 compatibility보다 우선

잘못된 진행 상태를 만들기 위해 unknown을 임의로 true/false로 바꾸는 경로는 확인하지 못했다.

### Dialogue compatibility

2026-08-17에 감사한 exact 12-ID dialogue compatibility는 현재 live build 이후 **residual dialogue 0**으로 확인됐다.

새 dialogue 또는 구조 변경은 자동 통과시키지 않고 fail-closed 한다.

### Trader task-pool compatibility

current live 재감사 결과:

```text
Regular:   audited task-pool structure 27 / 27 valid
PvE:       audited task-pool structure 26 / 27 valid
PvPSeason: audited task-pool structure 27 / 27 valid
```

PvE에서 불일치한 것은 Skier LL2 variable `6a5a111de1f417ac80a163e5` 한 묶음이다.

```text
pool quest count:  expected 9 / actual 9
thresholds:        expected 1,3,4 / actual 1,3,4
direct LL2 seeds:  expected 3 / actual 4
```

추가 live PvE seed candidate:

```text
6834145ebc1f443d7603c8a7 — Easy Money - Part 1 [PVE ZONE]
```

현재 코드는 이 drift를 감지하면 그 pool을 추측하지 않는다. 따라서 잘못 해금하는 대신 exact profile variable이 없으면 관련 Quest가 `확인 필요`로 남을 수 있다.

**판정: 현재 동작이 올바르다.** 새 seed가 실제 같은 server counter를 증가시키는지 증명하기 전에는 expected seed count를 3→4로 바꾸지 않는다.

---

## 2. Quest objective → 필요 Item 변환

### 코드 계약

Quest objective importer는 mandatory `giveItem`만 `QuestItemRequirement`로 만든다.

- `findItem`은 별도 material requirement로 중복 합산하지 않는다.
- `sellItem`은 stash에 계속 보관해야 할 hand-in material로 계산하지 않는다.
- optional `giveItem`은 mandatory Needed Items에서 제외한다.
- 여러 accepted item ID는 하나의 flexible alternative requirement로 유지한다.
- duplicate objective ID는 같은 Quest 안에서 fatal이다.

이는 find + hand-in이 함께 존재하는 흔한 Quest에서 같은 물건을 두 번 필요하다고 계산하는 오류를 방지한다.

### 2026-08-18 live 감사

```text
Regular
  quests                     517
  objectives                1457
  quest item requirements    307
  mandatory submit objective 307
  malformed submit             0
  missing derived requirement  0
  duplicate derived key        0
  alternative requirements    60
  FIR requirements           245
  missing item reference       0
  non-positive count           0

PvE
  quests                     513
  objectives                1428
  quest item requirements    291
  mandatory submit objective 291
  malformed submit             0
  missing derived requirement  0
  duplicate derived key        0
  alternative requirements    44
  FIR requirements           233
  missing item reference       0
  non-positive count           0

PvP Season
  quests                     490
  objectives                1392
  quest item requirements    286
  mandatory submit objective 286
  malformed submit             0
  missing derived requirement  0
  duplicate derived key        0
  alternative requirements    41
  FIR requirements           230
  missing item reference       0
  non-positive count           0
```

**지원 source 범위 안에서는 mandatory submit objective와 derived requirement가 1:1로 맞는다.**

---

## 3. Future Needed Items / cleanup

### 판정 결과

**제품의 가장 중요한 안전 로직 중 하나이며 현재 설계가 적절하다.**

- 완료된 Quest requirement는 미래 필요량에서 제거
- faction/edition 등 영구 불가능 Quest는 제외
- level-locked Quest는 미래에 가능하므로 포함
- unsupported/missing fact Quest는 `IndeterminatePotential`로 포함
- Failed-only recovery branch도 미래 가능성에 맞게 처리
- Hideout은 현재 level 이후 모든 future level requirement 포함
- profile에 station entry가 없으면 level 0으로 취급
- FIR requirement를 먼저 보존하고 unrestricted requirement에 남은 FIR/Non-FIR을 올바르게 배분
- flexible alternative requirement는 임의의 한 후보를 골라 cleanup하지 않고 후보 전체를 보호

### 의도적인 보수성

Flexible requirement의 후보 Item은 현재 **후보별 정확한 surplus 최적화보다 안전을 우선**한다.

예를 들어 `A 또는 B 중 2개 제출`인 경우 A/B가 cleanup 대상으로 잘못 빠지지 않도록 후보 Item을 강하게 보호한다. 이 때문에 실제로는 팔아도 되는 일부 후보 Item까지 남길 수 있다.

이것은 false-safe보다 false-keep을 택한 제품 정책이며 현재는 유지하는 편이 낫다.

---

## 4. Hideout

2026-08-18 current live 모든 GameMode에서:

```text
hideout stations:             26
hideout item requirements:   317
missing hideout item refs:     0
non-positive requirement count: 0
```

future level 누적 방식과 Inventory 결합도 정상이다.

### 작은 hardening gap

현재 `GameContentValidator`는 item reference 존재 여부는 다시 검사하지만 Quest/Hideout item requirement의 **`Count > 0`을 최종 validator에서 중복 방어하지 않는다.**

Quest importer 쪽은 positive count만 requirement로 생성하고 current live 데이터도 모두 정상이다. Hideout importer는 negative를 막지만 zero에 대한 final redundant guard는 약하다.

따라서 현재 bug는 아니지만 다음 maintenance 때 다음을 권장한다.

- Hideout importer에서 `count <= 0` reject
- final `GameContentValidator`에서도 Quest/Hideout requirement `Count > 0`, accepted item set non-empty를 fatal validation

우선순위: **중간 이하 / 작은 안정성 보강**

---

## 5. Content update / 저장 안전성

### 평가

**매우 좋음.**

온라인 update는 active DB를 바로 덮지 않는다.

```text
download
→ importer
→ canonical candidate
→ validation
→ candidate write/read-back
→ activation
→ active read-back
```

- invalid candidate는 active content를 대체하지 않음
- previous snapshot 보존/복구 경로 존재
- SQLite integrity check 사용
- content update와 `user.db`는 분리
- user profile / inventory / quest / hideout 진행을 content update가 덮어쓰지 않음
- schema v7 / readable v3~v7

Major-update resilience test도 requirement 교체, quantity 감소, edition 변화, Hideout material 교체, flexible candidate 변화, invalid candidate rollback을 직접 검증한다.

---

## 6. UI / UX

### 평가

**현재 사용자가 승인한 v0.1.12 UI를 유지한다.**

Dark theme은 Window, TextBlock, Button, TextBox, ComboBox, ComboBoxItem, DataGrid, ScrollBar 등을 product-wide resource로 통일한다.

v0.1.12부터 핵심 회귀는 source string 검사가 아니라 실제 WPF `Measure/Arrange` 결과로 검증한다.

- Flexible row full-width / lane axis
- Ammo `☆/★`
- Ammo detail `▼/▲`
- Map Quest title actual X axis
- Map sidebar handle edge

동일 published EXE smoke에서 Main Map / Factory / MiniMap / graceful close도 통과했다.

### 향후 개선 가능

현재 rendered smoke는 **과거 실제로 문제가 생겼던 요소 중심**이다. 모든 화면을 여러 DPI/창 크기로 비교하는 full visual regression suite는 아니다.

다음 대규모 UI 작업이 생기는 시점에는 100%/125%/150% DPI + 최소/기본 창 크기의 screenshot regression을 고려할 수 있다.

지금 UI를 단지 '정리' 목적으로 다시 만지는 것은 권장하지 않는다.

---

## 7. 코드 구조 / 불필요 코드 / 기술부채

### Core / Application / Infrastructure

핵심 기능 영역은 책임이 비교적 잘 분리돼 있다.

- raw source parsing/import
- canonical content model
- validation
- availability/future reachability
- Needed Items calculation
- application mutation
- storage/activation
- desktop presentation

이 영역은 전면 refactor할 이유가 없다.

### Map legacy debt

Map은 현재 가장 큰 maintenance debt다.

`MainWindow.LegacyMapHost`는 V2 Quest sidebar를 실제 제품에 쓰면서도, UI delta 재사용을 위해 **연결되지 않은 V1 `LegacyMapQuestSidebar`와 `LegacyMapProductAdapter`를 생성**한다.

그 결과 adapter 안에는 현재 host에서 실제 Quest content/workspace를 받지 않는 V1 Quest marker layer 및 관련 subscription/render 코드가 남아 있다.

현재 기능을 깨뜨리지는 않지만 다음 Map 대수선 때 정리할 수 있는 dead runtime surface다.

권장:

- `LegacyMapProductAdapter`에서 순수 UI-delta 역할을 별도 class로 분리
- disconnected V1 sidebar 제거
- V1 Quest projection/marker path가 다른 곳에서 사용되지 않는지 확인 후 제거
- V2/V3 naming을 기능 기준 이름으로 정리

**단, 현재 Map/MiniMap이 안정화된 상태이므로 지금 단독 cleanup 작업으로 건드리지 않는다.**

### donor warnings

final audit build:

```text
18 warnings
0 errors
```

warning은 pinned donor Tarkov-Helper Map 코드의 nullable/unawaited/unused-event 계열이다. JunhyunHelper는 donor source를 불필요하게 변형하지 않기 위해 해당 warning을 warning-as-error에서 예외 처리한다.

현재 증상이 없는 한 warning 제거만을 목적으로 donor source를 수정하지 않는다.

### stale compatibility API

`UnenteredHideoutLevel` cleanup protection / `UnenteredHideoutStationIds` 같은 과거 보수 모델의 흔적이 남아 있다. 현재 제품은 missing station을 level 0으로 처리하므로 정상 path에서는 비어 있다.

우선순위 낮은 dead/stale API 정리 후보이다.

---

## 8. 가장 중요한 coverage gap — Story Chapters

### 발견

현재 product content builder의 primary progression source는 `json.tarkov.dev`의 `tasks`이며, 별도 Story Chapter source를 가져오지 않는다.

한편 tarkov.dev 프로젝트에는 EFT 1.0 Story Chapters가 사이트에 아직 표현되지 않는다는 공개 feature request가 존재한다(`the-hideout/tarkov-dev#1287`).

즉 현재 준현 헬퍼의 Quest/Needed Items pipeline은 **가져온 ordinary task feed 안에서는 정확하지만, EFT 1.0 Story Chapters까지 포함한 전체 게임 진행 데이터라고 볼 수 없다.**

### 영향

- Story Chapter 전용 hand-in이 ordinary task feed에 없으면 Future Needed Items가 알 수 없다.
- 따라서 Story Chapter에서만 필요한 일반 inventory Item에 대해서는 cleanup 보호를 완전히 보장할 수 없다.
- Story progression으로 해금되는 early trader/progression gate도 ordinary Quest graph만으로는 완전하게 표현하지 못할 수 있다.

이는 Needed Items arithmetic bug가 아니라 **source coverage 문제**다.

### 향후 수정 방향

개발을 다시 시작할 때 가장 먼저 검토할 개선 후보는 Story Chapters를 canonical progression model에 추가하는 것이다.

원칙:

1. runtime에서 안정적으로 받을 수 있는 구조화된 허용 source가 있으면 importer 추가
2. 없으면 작은 product-owned compatibility dataset을 별도 관리하되 exact ID/semantic 근거와 drift guard 필수
3. Story hand-in requirement도 기존 `QuestItemRequirement`/Future Needed Items와 같은 안전 모델에 연결
4. source가 불완전하면 추측하지 않고 fail-closed
5. 외부 wiki/데이터를 직접 복제하는 경우 라이선스/attribution 먼저 확인

우선순위: **향후 개발 재개 시 가장 높은 정확도 개선 항목**

현재 v0.1.12 사용을 중단해야 할 blocker는 아니다. 다만 제품 설명에서 `tasks` 기반 Quest/Hideout Needed Items와 'EFT 전체 progression item'을 구분해야 한다.

---

## 9. 유지보수 우선순위

### 다음 개발 재개 시 먼저 볼 것

1. **Story Chapters coverage 설계/통합**
2. PvE Skier LL2 task-pool의 새 `[PVE ZONE]` seed가 실제 counter write에 포함되는지 근거 확보
3. Quest/Hideout requirement positive-count final validation 추가

### 그 다음 개선

- `user.db` 자동 backup / export / restore UX
- multi-DPI screenshot visual regression
- data download transient retry/backoff
- flexible alternative surplus의 더 정밀한 안전 계산
- stale Hideout cleanup API 제거
- Map disconnected V1 path 정리
- code signing / installer / updater

### 지금 건드리지 말 것

- Indeterminate / IndeterminatePotential 보수 정책
- task-pool drift fail-closed
- FIR allocation / cleanup 기본 모델
- 현재 승인된 v0.1.12 UI
- pinned donor Map을 warning 제거만을 위해 수정
- Core/Application/Infrastructure 전면 재설계

---

## 10. 자체 점수

| 영역 | 점수 | 평가 |
|---|---:|---|
| Quest 판정 — 지원 source 범위 | 9.3/10 | unknown을 추측하지 않고 source 의미 보존 |
| Quest Item 추출 / Needed Items | 9.4/10 | live mandatory submit 1:1 검증, cleanup 보수성 우수 |
| Content update / 데이터 보존 | 9.5/10 | candidate/validation/read-back/rollback 구조 우수 |
| UI / UX 안정성 | 9.0/10 | v0.1.12 실제 rendered gate 적용 |
| Core architecture | 9.0/10 | 주요 책임 분리 양호 |
| Data coverage | 7.4/10 | Story Chapters가 source 범위 밖 |
| Map maintainability | 6.8/10 | 기능은 안정적이나 donor/bridge/V1~V3 debt 존재 |
| 전체 | **8.7/10** | 현재 안정적으로 동결 가능, 다음 정확도 개선점 명확 |

---

## 최종 결론

**v0.1.12를 현재 안정 기준선으로 유지한다.**

즉시 hotfix가 필요한 발견은 없다.

가장 중요한 다음 과제는 새로운 UI나 편의 기능이 아니라 **Story Chapters까지 포함하는 progression data coverage 확대**다. 그 전까지 현재 Quest/Needed Items는 `json.tarkov.dev` task feed + 검증된 compatibility 범위에서 신뢰할 수 있는 보수적 도구로 취급한다.
