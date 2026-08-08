# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 필요한 공식 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2B — 실제 Desktop 핵심 흐름 구현**

상태: `IN PROGRESS — data pipeline + profile + Quest + Hideout + future Items + Ammo desktop flows verified`

현재 사용자는 게임 모드별 캐릭터 프로필을 만들고 수정하고, 같은 프로필을 기준으로 Quest 진행, Hideout 시설 레벨, 미래 필요 아이템과 실제 보유량을 관리할 수 있으며 최신 탄약 성능과 수급처를 비교할 수 있습니다.

필요 아이템은 현재 할 일 체크리스트가 아니라 **앞으로 필요한 물건을 미리 모으고, 업데이트나 진행 변화로 더 이상 필요하지 않은 보유품을 안전하게 정리하게 해주는 기능**으로 확정·구현되었습니다.

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
- 명시적으로 입력된 Hideout station levels
- inventory FIR / Non-FIR

저장하지 않음:

- Current / Locked / Indeterminate
- 미래 Quest 도달 가능성 결과
- 다음 Hideout 업그레이드 결과
- Needed Items / Cleanup 결과
- Ammo 조회/필터 상태
- UI filter/sort 결과

Game Content가 업데이트되어도 user.db 보유량과 진행 사실은 삭제하지 않습니다.

---

## Profile

`ProfileApplicationService` + WPF profile UI 구현.

- PvP / PvE / 시즌별 명시적 프로필 생성
- same mode 중복 자동 생성 방지
- level / faction / edition / prestige 편집
- trader별 입력 여부 + LL + standing
- 미입력 trader를 0으로 추측하지 않음
- 설정 수정 시 completed Quest / Hideout / Inventory 보존

---

## Quest

`QuestApplicationService` + WPF Quest 화면 구현.

사용자 행동:

- 수동 완료
- 완료 취소

탐색:

- 진행 중 / 잠김 / 완료
- 검색 / 상인 / 지도 dropdown
- Wiki / 상세
- `Indeterminate` 별도 판정 문제

현재 availability evaluator:

- level
- faction
- edition
- prestige
- trader reputation / loyalty
- prerequisite Complete / Active
- disabled

미지원/미입력은 추측하지 않습니다.

### 미래 Quest 도달 가능성

필요 아이템 전용 `QuestFutureReachabilityEvaluator` 구현.

상태:

- `Potential` — 미래 요구량 포함
- `Completed` — 제외
- `Unavailable` — 영구 불가로 증명, 제외
- `IndeterminatePotential` — 불명확하지만 보수적으로 미래 요구량 포함

규칙:

- level / trader / prestige 부족 → 미래에 충족 가능하므로 포함
- faction / edition / disabled → 영구 불가이므로 제외
- 완료 Quest → 제외
- 이미 완료된 Quest의 `Failed`만 요구하는 후속 분기 → 영구 불가로 닫을 수 있음
- 아직 추적하지 않는 Failed 상태 / `dialogue` / 기타 미지원 의미 → 정리 위험을 피하기 위해 가능성 유지
- 영구 불가 prerequisite를 요구하는 후속 Quest도 전파해 제외

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

### 미래 필요량

`FutureNeededItemsPlanner` 구현.

입력:

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
- 아직 닫히지 않은 불명확 가능성도 보수적으로 포함
- 입력된 Hideout current level 이후 모든 업그레이드 재료

제외:

- 완료 Quest
- 영구 불가로 증명된 Quest/경로
- 이미 지난 Hideout level 요구량

### 정리 필요

`InventorySurplusCalculator` 구현.

- inventory-only item도 계산 대상
- `필요 0 / 보유 > 0`이면 정리 필요에 남음
- 필요량보다 많이 가진 경우 초과분 표시
- FIR minimum과 total requirement를 동시에 지킨 뒤 안전한 FIR/Non-FIR cleanup 수량만 반환
- 대체 제출 후보는 임의 선택하지 않아 cleanup 보호
- 미입력 Hideout 관련 item도 cleanup 보호

Game Content에 더 이상 item metadata가 없어도 user.db에 보유량이 남아 있으면 Item 화면에서 stable Item ID라도 계속 보입니다.

### 변화 감지

`InventoryCleanupChangeDetector` 구현.

Quest 완료/취소, Hideout level 변화, Game Content update 후 이전 계획과 새 계획의 `정리 가능` 증가분을 비교할 수 있습니다.

특히 데이터 업데이트로 새 정리 가능 보유품이 생기면:

- 사용자에게 알림
- Item 탭에 지속적인 `정리 필요` 목록

알림을 닫아도 실제 inventory가 초과 상태인 한 `정리 필요` 자체는 사라지지 않습니다.

### Item Application/UI

`ItemsApplicationService` + 첫 WPF Item 화면 구현.

Application:

- item별 FIR / Non-FIR 보유량만 수정
- 0/0이면 해당 inventory row 제거
- 다른 profile 진행 사실 보존
- 저장 후 미래 필요량 재계산

Item 탭:

- 검색
- `필요 / 전체 / 정리 필요 / 충분 / 판단 보류` 필터
- 미래 필요량 / FIR 요구
- 현재 보유 FIR / 일반
- 추가 필요량
- 안전한 정리 가능량
- 필요 출처
- 판단 보류 이유
- 보유량 직접 수정
- 새 cleanup 변화 배너

Quest/Hideout/Items는 서로의 저장소를 직접 수정하지 않습니다. 사용자 사실 하나를 변경한 뒤 세 workspace를 동일한 Core 규칙으로 다시 계산합니다.

---

## Ammo

Canonical Ammo + acquisition:

- TraderPurchase
- TraderBarter
- HideoutCraft

실제 탄약은 `ItemPropertiesAmmo`로 식별합니다.

WPF Ammo 화면 구현:

- 상위 `탄약` 탭
- 탄약 이름 검색
- 구경 dropdown
- 정렬 가능한 비교 표
- damage / projectile count / penetration / armor damage / initial speed
- fragmentation / accuracy / recoil
- 선택 탄약의 추가 성능 정보
- TraderPurchase / TraderBarter / HideoutCraft 수급처 상세
- 상인/시설 레벨, 가격, 재료, 구매 제한, 제작 시간, 결과 수량, 퀘스트 해금 표시

탄약은 읽기 전용 Game Content 기능입니다.

- 별도 User Progress를 저장하지 않음
- Quest/Hideout/Items 재계산에 결합하지 않음
- 콘텐츠 로드/업데이트 시에만 새 canonical Ammo를 받음
- 자체 방어구 효율/티어/추천 점수 없음
- 여러 projectile은 피해량을 임의 합산하지 않고 원본 `damage × projectileCount`로 표시
- 알려지지 않은 새 caliber 식별자는 화면을 깨뜨리지 않고 canonical 값으로 fallback

---

## 최신 검증

### Ammo Desktop checkpoint — 2026-08-08

Windows Server 2025 / .NET SDK 10.0.302:

- Desktop restore/build 성공
- **0 warnings / 0 errors**
- 전체 테스트 **106 passed / 0 failed / 0 skipped**

기존 Future Items 회귀 테스트도 모두 유지됩니다.

---

## 아직 남은 주요 제품/구현 항목

- Failed/branch Quest 상태를 사용자가 어떻게 명시할지
  - 현재는 미확정 failed branch를 `IndeterminatePotential`로 보수적으로 포함
- Alternative Quest item을 사용자가 선택/관리할 UX
- Quest reward 전체 canonical model
- profile 삭제/reset UX
- 지도
- Scanner
- 기존 Tarkov-Helper migration은 현재 목표 아님

---

## 다음 순서

1. 실제 데이터에서 Quest branch/failed 상태가 미래 필요 아이템 정확도에 주는 영향을 검토
2. 필요한 최소 사용자 입력만 정의해 닫힌 분기를 확실하게 제거할 방법 설계
3. Alternative Quest item 선택 UX와 함께 Needed Items의 남은 `판단 보류`를 줄임
4. 이후 profile 삭제/reset 또는 지도 데이터 공급원 조사 중 제품 우선순위가 높은 항목 진행

새 기능도 `Game Content / User Progress / Domain Logic / Application / UI` 경계를 유지합니다.

## 마지막 갱신

2026-08-08 — canonical Ammo를 그대로 읽는 WPF 탄약 비교 화면 구현. 구경 dropdown/검색/정렬 표와 상인 구매·물물교환·은신처 제작 상세를 연결하고, 자체 효율 점수나 별도 사용자 상태는 추가하지 않음. Windows Desktop 0 warning/0 error, 전체 테스트 106/106 통과.
