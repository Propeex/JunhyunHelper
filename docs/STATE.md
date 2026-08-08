# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽고 필요한 공식 문서만 추가 확인합니다.

## 현재 Phase

**Phase 2B — 실제 Desktop 핵심 흐름 구현**

상태: `IN PROGRESS — data pipeline + profile + Quest + Hideout desktop flows verified`

현재 사용자는 프로그램 안에서 게임 모드별 캐릭터 프로필을 만들고 수정하고, 같은 프로필을 기준으로 Quest 진행과 Hideout 시설 레벨을 관리할 수 있습니다.

다음 핵심 제품 영역은 **Needed Items**입니다. 단, 어떤 미래 Quest/Hideout 범위까지 기본 합산할지는 아직 제품 정책으로 확정하지 않았으므로 Core가 임의로 결정하지 않습니다.

---

## 최우선 철학

준현 헬퍼는 생각하는 AI가 아니라 **검증된 명시 규칙을 실행하는 결정론적 도구**입니다.

`최신 Tarkov 데이터 다운로드 → 검증 → canonical model 변환 → 안전한 로컬 콘텐츠 갱신 → 사용자 진행과 결합 → 명시 규칙으로 결과 계산`

- 일반 패치 때 GPT가 데이터를 다시 수작업 변환하지 않음
- 동일 입력 → 동일 결과
- 모르는 의미를 추측하지 않음
- 미입력을 임의의 0/false로 바꾸지 않음
- 안전한 판정이 불가능하면 `Indeterminate` 또는 업데이트 실패
- Game Content와 User Progress를 분리
- 계산 가능한 결과를 별도 진실의 원천으로 저장하지 않음
- UI는 Domain 규칙을 다시 구현하지 않음

---

## 책임 경계

1. **Game Content** — 온라인 원천에서 재생성 가능한 게임 사실
2. **User Progress** — 사용자가 실제 게임에서 만든 진행 사실
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

## 데이터 원천

### 1차 — `json.tarkov.dev`

- tasks
- hideout
- items
- traders
- maps
- barters
- crafts

모드:

- regular
- pve
- pvp-season

한국어 `ko` 사용.

### 보조 — edition rules only

TarkovTracker `tarkov-data-overlay` 중 `editions` 섹션만 사용:

- Edition ID / 표시명
- `exclusiveTaskIds`
- `excludedTaskIds`

전체 community correction overlay는 자동 적용하지 않습니다.

---

## 구현 상태

### Game Content / 업데이트

Canonical model:

- Item
- Trader / Map 최소 참조
- Edition Quest rules
- Quest / prerequisite / objective / submit-item requirement
- Hideout station / level / material requirement
- Ammo performance / acquisition
- `GameContentCatalog`

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

### User Progress / `user.db`

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
- 다음 Hideout 업그레이드 계산 결과
- Needed Items 결과
- UI filter/sort 결과

### Profile

`ProfileApplicationService` + WPF profile UI 구현.

- PvP / PvE / 시즌별 명시적 프로필 생성
- 같은 지원 game mode 중복 자동 생성 방지
- level / faction / edition / prestige 편집
- trader별 입력 여부 + LL + standing
- 미입력 trader는 0으로 추측하지 않음
- 프로필 설정 수정 시 completed Quest / Hideout / Inventory 보존

### Quest

`QuestApplicationService` + WPF Quest 화면 구현.

사용자 행동:

- 수동 완료
- 완료 취소

탐색:

- 진행 중 / 잠김 / 완료
- 검색
- 상인 dropdown
- 지도 dropdown
- Wiki
- 상세 정보

`Indeterminate`는 정상 Quest 상태가 아니라 별도 `판정 문제`로 표시합니다.

현재 evaluator가 처리:

- level
- faction
- edition rules
- prestige
- trader reputation / loyalty
- prerequisite Complete / Active
- disabled

추측하지 않음:

- 미입력 profile value
- Failed-only prerequisite
- dependency cycle
- `dialogue` 등 미지원 availability requirement

시간 지연은 제품 결정에 따라 계산하지 않습니다.

### Hideout

`HideoutApplicationService` + 첫 WPF Hideout 화면 구현.

핵심 원칙:

- station current level은 **사용자 사실**
- `미입력`과 `Lv.0`을 구분
- 미입력 level을 0으로 추측하지 않음
- 사용자가 level을 명시하면 그 값만 `user.db`에 저장
- station level 변경은 다른 profile 사실을 직접 수정하지 않음

현재 UI:

- Quest / Hideout 상위 탭
- Hideout 검색
- 시설 목록
- 현재 level 표시
- `미입력 / Lv.0 ... max` 명시적 level selector
- level이 알려진 경우 canonical data에서 **바로 다음 업그레이드** 표시
- 다음 업그레이드 item requirement + FIR 표시
- construction time 표시
- max level 표시

현재 의도적으로 구현하지 않음:

- 로그/게임 화면 기반 자동 Hideout 추정
- `미입력 = Lv.0` 가정
- 최종 레벨까지 남은 재료를 자동 합산하는 정책
- Needed Items에서 어느 Hideout 미래 범위까지 기본 포함할지 결정
- canonical model에 아직 없는 trader/skill/station prerequisite를 UI에서 추측

### Quest + Hideout → Needed Items 기반

이미 Core에 존재:

- Quest `giveItem` submit requirement
- Hideout material requirement
- Item ID별 집계
- 출처 보존
- FIR / Non-FIR 이중 계산 방지
- alternative Quest item group 보존

아직 Desktop에서는 **어느 요구사항을 기본 포함할지** 제품 정책이 정해지지 않아 자동 집계를 연결하지 않았습니다.

### Ammo

Canonical Ammo + acquisition 구현:

- TraderPurchase
- TraderBarter
- HideoutCraft

실제 탄약은 `ItemPropertiesAmmo`로 식별합니다.

Desktop Ammo 화면은 아직 새 제품에 구현하지 않았습니다.

---

## 실제 검증

실제 `json.tarkov.dev + editions-only overlay`로 regular / pve / pvp-season 모두:

`live sources → canonical build → candidate.db → validation → active → read-back`

성공.

### 최신 Windows CI — Hideout checkpoint

- Windows Server 2025
- .NET SDK 10.0.302
- Desktop build 성공
- **0 warnings / 0 errors**
- 전체 테스트 **86 passed / 0 failed / 0 skipped**

회귀 테스트 포함:

- profile settings 수정 시 기존 진행 보존
- Quest 완료/완료 취소 후 재계산
- `Indeterminate`가 Current에 섞이지 않음
- Hideout 미입력 level 유지
- 명시적 Lv.0과 미입력 구분
- Hideout level 변경/해제 저장
- max range 밖 level 거부
- Hideout level 변경 시 다른 profile 진행 보존

---

## 기존 Tarkov-Helper 참고 정책

사용성 패턴만 참고:

- 적은 수의 상위 탭
- 검색 + ComboBox
- 목록 / 우측 detail split
- 상태 badge
- 명확한 주 행동 버튼
- section화된 detail
- Hideout level control
- Ammo caliber dropdown + table

기존 code-behind/service/event 구조와 오래된 제품 규칙은 승계하지 않습니다.

---

## 아직 제품 결정/구현이 남은 것

- Needed Items의 기본 포함 범위
  - Current Quest만인지
  - Locked/Future Quest까지 포함할지
  - Hideout next upgrade만인지
  - 모든 미래 Hideout level까지 포함할지
- Item 자동 차감 여부
- 대체 제출 아이템을 사용자가 어떻게 선택/관리할지
- Quest 실패/분기 상태 입력 방식
- Quest reward 전체 canonical model
- profile 삭제/reset UX
- Ammo Desktop
- 지도
- Scanner

---

## 다음 순서

1. **Needed Items 제품 범위 확정**
2. 확정된 입력 범위만 `NeededItemsQuery`에 전달하는 얇은 Application flow 구현
3. 보유 수량 입력/수정과 Needed Items Desktop 화면
4. Quest 완료 / Hideout level 변경 → 별도 저장 동기화 없이 Needed Items 재계산 검증
5. 이후 Ammo Desktop

Core가 미래 요구 범위를 임의로 선택하지 않는 원칙을 유지합니다.

## 마지막 갱신

2026-08-08 — 명시적 Hideout station level 저장과 첫 Hideout WPF 화면을 구현. 미입력과 Lv.0을 분리하고, 입력된 current level에서 바로 다음 upgrade requirement만 사실로 표시. Desktop build 0 warning/0 error, 전체 테스트 86/86 통과 후 main 반영. 다음 제품 결정은 Needed Items 기본 포함 범위.
