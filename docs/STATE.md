# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 필요한 공식 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2B — 실제 Desktop 핵심 흐름 구현**

상태: `IN PROGRESS — data pipeline + profile + Quest branching/failure + Hideout + future Items + Ammo desktop flows verified`

현재 사용자는 게임 모드별 캐릭터 프로필을 만들고 수정하고, Quest 진행/분기, Hideout 시설 레벨, 미래 필요 아이템과 실제 보유량을 관리할 수 있으며 최신 탄약 성능과 수급처를 비교할 수 있습니다.

필요 아이템은 현재 할 일 체크리스트가 아니라 **앞으로 필요한 물건을 미리 모으고, 업데이트나 진행 변화로 더 이상 필요하지 않은 보유품을 안전하게 정리하게 해주는 기능**입니다.

---

## 최우선 철학

준현 헬퍼는 생각하는 AI가 아니라 **검증된 명시 규칙을 실행하는 결정론적 도구**입니다.

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 안전한 로컬 콘텐츠 갱신 → 사용자 진행과 결합 → 명시 규칙으로 결과 계산`

- 일반 패치 때 GPT가 데이터를 다시 수작업 변환하지 않음
- 동일 입력 → 동일 결과
- 모르는 의미를 추측하지 않음
- 미입력을 임의의 0/false로 바꾸지 않음
- 안전한 판정이 불가능하면 `Indeterminate`, `판단 보류`, 또는 업데이트 실패
- Game Content와 User Progress를 분리
- 계산 가능한 결과를 별도 진실의 원천으로 저장하지 않음
- UI는 Domain 규칙을 다시 구현하지 않음
- 사용자가 필요한 아이템을 잘못 버리게 만드는 false-positive cleanup을 특히 피함
- 게임 사실과 개발자가 만든 임의 평가/추천 점수를 섞지 않음
- 자동으로 알 수 있는 진행 사실은 다시 사용자에게 입력시키지 않음

---

## 책임 경계

1. **Game Content** — 온라인 원천에서 재생성 가능한 게임 사실
2. **User Progress** — 사용자가 실제 게임에서 만든 진행/보유 사실
3. **Domain Logic** — 두 입력에서 결과를 계산하는 순수 규칙
4. **Application** — 사용자 명령과 저장/재계산의 얇은 조정
5. **Desktop/UI** — 결과 표시와 사용자 입력 전달

현재 사용하지 않음:

- ORM
- DI container
- 별도 backend
- 범용 rule engine
- runtime AI/GPT
- 기능 간 거대한 event bus

---

## 데이터 원천 / 콘텐츠 업데이트

### 1차 — `json.tarkov.dev`

- tasks
- hideout
- items
- traders
- maps
- barters
- crafts

모드: `regular / pve / pvp-season`, 한국어 `ko` 사용.

### 보조 — edition rules only

TarkovTracker `tarkov-data-overlay` 중 `editions` 섹션만 사용:

- Edition ID / 표시명
- `exclusiveTaskIds`
- `excludedTaskIds`

전체 community correction overlay는 자동 적용하지 않습니다.

모드별 콘텐츠 저장:

```text
content/
  regular/
    content.db
    content.candidate.db
    content.previous.db
  pve/
    ...
  pvp-season/
    ...
```

업데이트:

`API + edition source → canonical import → semantic/reference validation → candidate DB → DB validation → active replace`

- 실패 candidate는 active를 건드리지 않음
- active 손상 시 같은 모드 previous 복구 가능
- 실제 세 모드 live update / read-back 검증 완료

---

## User Progress / `user.db`

프로필 하나 = 실제 Tarkov 캐릭터 하나.

저장:

- game mode
- level / faction / edition / prestige
- 명시적으로 입력된 trader LL / standing
- completed Quest IDs
- **필요한 경우에만 explicit permanent failed Quest IDs**
- 명시적으로 입력된 Hideout station levels
- inventory FIR / Non-FIR

저장하지 않음:

- Current / Locked / Unavailable / Indeterminate
- 다른 완료 사실에서 자동 추론 가능한 Quest failure
- 미래 Quest 도달 가능성 결과
- 다음 Hideout 업그레이드 결과
- Needed Items / Cleanup 결과
- Ammo 조회/필터 상태
- UI filter/sort 결과

Game Content가 업데이트되어도 user.db 보유량과 진행 사실을 자동 삭제하지 않습니다.

과거 explicit failure 기록이 새 패치에서 더 이상 영구 실패 규칙에 해당하지 않으면 기록 자체는 보존하지만 계산에는 강제로 적용하지 않습니다. 새 규칙에서 해당 Quest를 완료하면 같은 Quest의 낡은 explicit failure 기록만 제거합니다.

---

## Profile

`ProfileApplicationService` + WPF profile UI 구현.

- PvP / PvE / 시즌별 명시적 프로필 생성
- same mode 중복 자동 생성 방지
- level / faction / edition / prestige 편집
- trader별 입력 여부 + LL + standing
- 미입력 trader를 0으로 추측하지 않음
- 설정 수정 시 Quest / Hideout / Inventory 진행 보존

---

## Quest

`QuestApplicationService` + WPF Quest 화면 구현.

### 정상 상태

- `Current` — 진행 중
- `Locked` — 앞으로 충족 가능한 조건 때문에 잠김
- `Unavailable` — 현재 캐릭터/확정된 진행에서 영구적으로 사용 불가
- `Completed` — 완료

`Indeterminate`는 정상 상태가 아니라 실제 데이터/입력 문제만 별도 `판정 문제`로 표시합니다.

탐색:

- 진행 중 / 잠김 / 사용 불가 / 완료
- 검색 / 상인 / 지도 dropdown
- Wiki / 상세
- 판정 문제 별도 표시

현재 availability evaluator:

- level
- faction
- edition
- prestige
- trader reputation / loyalty
- prerequisite Active / Complete / Failed
- disabled
- explicit permanent failure
- 다른 Quest 완료로 발생하는 taskStatus failure
- 영구 불가 prerequisite 전파

### Quest 실패/분기

공식 조사: `docs/QUEST_FAILURE_ANALYSIS.md`

2026-08-08 regular live source 분석:

- Quest 510개
- task prerequisite 607개
- Failed 포함 prerequisite 24개
- Failed-only prerequisite 4개
- failConditions 50개 / 38 Quest
- taskStatus failure 35개 / 23 Quest

핵심 규칙:

- 다른 Quest 완료로 확정되는 실패는 `CompletedQuestIds + failConditions`에서 자동 추론
- 자동 추론 failure는 user.db에 중복 저장하지 않음
- failed-only 선행조건은 결과 결정 전 `Indeterminate`가 아니라 정상 미래 가능성
- 완료/실패가 확정되면 불가능한 sibling/후속 경로를 `Unavailable`로 계산
- 재시작 가능한 raid failure는 permanent user fact로 저장하지 않음
- Game Content가 `restartable = false`이고 프로그램이 실제 발생 여부를 알 수 없는 failure만 상세 화면에 `실패 처리` 표시
- explicit failure는 `실패 취소` 가능

사용자가 모든 분기를 수동 관리하는 별도 branch manager는 만들지 않습니다.

### 미래 Quest 도달 가능성

필요 아이템 전용 `QuestFutureReachabilityEvaluator`:

- `Potential` — 미래 요구량 포함
- `Completed` — 제외
- `Unavailable` — 영구 불가로 증명, 제외
- `IndeterminatePotential` — 데이터 의미가 불명확하여 보수적으로 포함

분기 선택 전에는 가능한 여러 경로의 아이템을 유지하고, 결과가 확정된 뒤 불가능 경로만 제거합니다.

---

## Hideout

`HideoutApplicationService` + WPF Hideout 화면 구현.

- station current level은 사용자 사실
- `미입력`과 `Lv.0` 구분
- Hideout 화면은 입력된 current level의 바로 다음 upgrade 표시
- 필요 아이템 계획에서는 current level보다 높은 **모든 미래 level material** 합산
- current level 미입력 시설은 Lv.0으로 가정하지 않음
- 해당 시설에서 쓸 수 있는 보유 item은 잘못 정리하지 않도록 cleanup 판단 보호

---

## Future Needed Items / Item Desktop

공식 제품 기준: `docs/NEEDED_ITEMS_EXPERIENCE.md`

`FutureNeededItemsPlanner` 입력:

`Game Content + User Progress`

출력:

- 미래 고정 필요량
- 현재 부족량
- 안전하게 정리 가능한 초과 보유량
- 대체 Quest 요구
- cleanup 판단 보호 항목
- 미래 Quest reachability 진단

포함:

- 현재/미래 가능 Quest 제출 아이템
- 아직 열려 있는 여러 분기
- 데이터 의미가 불명확한 가능성은 보수적으로 유지
- 입력된 Hideout current level 이후 모든 업그레이드 재료

제외:

- 완료 Quest
- 진영/에디션/disabled 등 영구 불가 Quest
- 확정된 완료/실패 분기로 닫힌 Quest/경로
- 이미 지난 Hideout level 요구량

### 정리 필요

`InventorySurplusCalculator`:

- inventory-only item도 계산 대상
- `필요 0 / 보유 > 0`이면 정리 필요에 남음
- 필요량보다 많이 가진 경우 초과분 표시
- FIR minimum과 total requirement를 모두 지킨 뒤 안전한 FIR/Non-FIR cleanup 수량만 반환
- 대체 제출 후보는 임의 선택하지 않아 cleanup 보호
- 미입력 Hideout 관련 item도 cleanup 보호

분기 완료/실패로 미래 요구량이 줄면 이미 모은 초과분도 `정리 필요`로 이어집니다.

Game Content에 더 이상 item metadata가 없어도 user.db에 보유량이 남아 있으면 Item 화면에서 stable Item ID라도 계속 보입니다.

### 변화 감지

`InventoryCleanupChangeDetector`가 Quest 완료/취소/실패/실패 취소, Hideout level 변화, Game Content update 전후의 `정리 가능` 증가분을 비교합니다.

새 정리 가능 보유품이 생기면:

- 사용자에게 알림
- Item 탭에 지속적인 `정리 필요` 목록

알림을 닫아도 실제 inventory가 초과 상태인 한 `정리 필요`는 사라지지 않습니다.

---

## Ammo

WPF Ammo 화면 구현:

- 상위 `탄약` 탭
- 탄약 이름 검색
- 구경 dropdown
- 정렬 가능한 비교 표
- damage / projectile count / penetration / armor damage / initial speed
- fragmentation / accuracy / recoil
- TraderPurchase / TraderBarter / HideoutCraft 수급처 상세
- 상인/시설 레벨, 가격, 재료, 구매 제한, 제작 시간, 결과 수량, Quest 해금 표시

탄약은 읽기 전용 Game Content 기능입니다.

- 별도 User Progress를 저장하지 않음
- Quest/Hideout/Items 재계산에 결합하지 않음
- 콘텐츠 로드/업데이트 시 새 canonical Ammo 표시
- 자체 방어구 효율/티어/추천 점수 없음
- 여러 projectile은 `damage × projectileCount`로 원본 의미 유지

---

## 최신 검증

### Quest failure/branch checkpoint — 2026-08-08

Windows Server 2025 / .NET SDK 10.0.302:

- Desktop restore/build 성공
- **0 warnings / 0 errors**
- 전체 테스트 **121 passed / 0 failed / 0 skipped**

새 회귀 범위:

- failed-only prerequisite는 결과 전 Locked/Potential
- explicit permanent failure가 failed-only recovery branch를 활성화
- sibling Quest 완료에서 taskStatus failure 자동 추론
- 자동 실패한 Quest 요구량이 Needed Items에서 제거
- recovery branch 필요량 유지
- explicit fail / undo가 다른 User Progress를 보존
- restartable failure permanent 저장 거부
- failure condition content import / DB round-trip
- missing failure trigger content validation 실패
- 패치 후 stale explicit failure가 새 canonical 완료를 방해하지 않음

---

## 아직 남은 주요 제품/구현 항목

- Alternative Quest item을 사용자가 선택/관리할 UX
- Quest reward 전체 canonical model
- profile 삭제/reset UX
- 지도 데이터 공급원/지도 기능
- Scanner
- 기존 Tarkov-Helper migration은 현재 목표 아님

---

## 다음 순서

1. **Alternative Quest item**이 현재 실제 데이터에서 얼마나 존재하고 Needed Items를 얼마나 `판단 보류`시키는지 live 데이터 기준으로 분석
2. 필요한 경우에만 최소 선택 UX 추가
3. 그 다음 profile 삭제/reset 또는 지도 데이터 공급원 조사 중 제품 우선순위가 높은 항목 진행

새 기능도 `Game Content / User Progress / Domain Logic / Application / UI` 경계를 유지합니다.

## 마지막 갱신

2026-08-08 — 최신 Quest failConditions를 canonical model에 추가하고, 다른 Quest 완료로 확정되는 분기 실패를 자동 계산하도록 구현. Quest 상태에 `사용 불가`를 분리하고 failed-only 미래 분기는 정상 가능성으로 처리. 프로그램이 알 수 없는 희귀 비재시작형 영구 실패만 수동 `실패 처리/실패 취소`를 제공. 분기 변화가 Future Needed Items와 `정리 필요`에 자동 반영되도록 연결. Windows Desktop 0 warning/0 error, 전체 테스트 121/121 통과.
