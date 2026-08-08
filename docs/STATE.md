# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 이 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 및 대형 패치 내구성 검증**

상태: `IN PROGRESS — first usable core workflow implemented and major-update resilience verified`

현재 실제 Desktop에서 다음 흐름이 연결되어 있습니다.

- 게임 모드별 프로필 생성 / 수정 / 삭제
- Quest 상태 계산, 수동 완료/완료 취소, 필요한 경우에만 영구 실패 처리/취소
- Hideout 시설별 현재 레벨 입력
- 미래 Quest + 미래 Hideout 기준 Needed Items 계산
- FIR / Non-FIR 실제 보유량 입력
- 더 이상 필요하지 않은 초과 보유량 `정리 필요` 계산 및 변화 알림
- 여러 Item ID를 허용하는 Quest 제출 목표의 그룹 단위 진행도
- 최신 Ammo 성능 / 수급처 비교
- 모드별 온라인 Game Content 안전 업데이트

지도와 Scanner는 현재 핵심 흐름 밖의 후속 기능입니다.

---

## 최우선 제품 철학

준현 헬퍼는 생각하는 AI가 아니라 **개발자가 명시적으로 설계한 규칙을 동일하게 실행하는 결정론적 도구**입니다.

```text
온라인 Tarkov 데이터
→ 다운로드
→ 형식/의미 검증
→ canonical Game Content 변환
→ candidate DB
→ DB/read-back 검증
→ active content 교체
→ User Progress와 결합
→ 순수 규칙으로 결과 계산
→ Desktop 표시
```

### 반드시 지키는 원칙

- 일반 패치 때 GPT가 데이터를 다시 수작업으로 해석하지 않음
- 같은 입력에는 같은 결과
- 모르는 외부 데이터 의미를 추측하지 않음
- 미입력을 0 / false / 기본 완료 상태로 몰래 바꾸지 않음
- 안전한 판정이 불가능하면 `Indeterminate`, `판단 보류`, 또는 업데이트 실패
- **Game Content와 User Progress를 분리**
- Game Content update가 user.db를 덮어쓰지 않음
- Current Quest / Needed Items / Cleanup 같은 파생 결과는 별도 진실의 원천으로 저장하지 않음
- UI는 Quest/Item 규칙을 다시 구현하지 않고 Core 계산 결과를 표시
- 사용자가 필요한 아이템을 잘못 버리게 하는 false-positive cleanup을 특히 금지
- 데이터에서 자동으로 알 수 있는 진행 사실을 다시 사용자에게 입력시키지 않음
- 인게임에서 쉽게 확인 가능하고 핵심 목적에 필요하지 않은 기능은 유지보수 비용을 이유로 추가하지 않을 수 있음

---

## 현재 책임 경계

1. **Core**
   - canonical Game/User types
   - Quest availability / failure / future reachability
   - Needed Items / FIR / cleanup / flexible hand-in 계산
2. **Infrastructure**
   - json.tarkov.dev / edition source 읽기
   - import / validation
   - content.db 안전 교체
   - user.db 저장
3. **Application**
   - 사용자 명령 한 건을 저장하고 Core를 다시 계산하는 얇은 조정
4. **Desktop**
   - WPF 표시 / 사용자 입력 전달

현재 의도적으로 사용하지 않음:

- ORM / EF Core
- DI container
- 별도 backend
- 범용 rule engine
- runtime AI/GPT
- 거대한 event bus
- 기능별 중복 데이터베이스

---

## 데이터 원천

### 1차 Game Content — `json.tarkov.dev`

현재 사용:

- tasks
- hideout
- items
- traders
- maps — 현재는 Quest 맵 참조/필터용 최소 정보
- barters
- crafts

지원 모드:

- `regular`
- `pve`
- `pvp-season`

표시는 한국어 `ko` 우선, 필요한 경우 영어 fallback.

### 보조 원천 — edition rules only

TarkovTracker `tarkov-data-overlay`의 `editions` 정보만 사용:

- Edition ID / 표시명
- `exclusiveTaskIds`
- `excludedTaskIds`

전체 community correction overlay를 자동 적용하지 않습니다.

---

## 로컬 저장

기본 Desktop 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
```

### User Progress

```text
user.db
```

프로필 하나가 한 Tarkov 캐릭터/진행 컨텍스트입니다.

저장하는 사실:

- Profile ID / GameMode
- level / faction / edition / prestige
- 사용자가 명시적으로 입력한 trader LL / standing
- completed Quest IDs
- 정말 필요한 경우에만 explicit permanent failed Quest IDs
- 명시적으로 입력한 Hideout station levels
- Item FIR / Non-FIR 보유량

저장하지 않는 파생 결과:

- Quest Current / Locked / Unavailable / Indeterminate
- 자동 추론 failure
- 미래 Quest reachability
- Needed Items
- Cleanup Items
- 유동 제출 진행도
- 다음 Hideout upgrade 계산
- Ammo 필터/정렬 상태

### Game Content

모드별 독립 저장:

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

업데이트 실패는 user.db에 영향을 주지 않습니다.

---

## Profile — 구현 완료 범위

상단 Profile UI:

- Profile dropdown
- `프로필 수정`
- `프로필 삭제`
- `새 프로필`

### 생성

지원 GameMode 중 아직 없는 모드를 고르고 명시적으로 생성합니다.

입력:

- level
- faction
- edition
- prestige
- 필요한 trader LL / standing

한 GameMode당 현재 프로필 하나만 둡니다.

### 수정

설정 값만 바꿉니다.

수정해도 다음 진행 사실은 보존:

- completed / failed Quest
- Hideout levels
- Inventory

### 삭제 / 전체 초기화

별도의 `Reset engine`을 만들지 않습니다.

**프로필 삭제 = 그 GameMode 캐릭터의 User Progress 전체 삭제**입니다.

삭제되는 것:

- 프로필 설정
- Quest 완료/실패
- Hideout levels
- trader progress
- Inventory

삭제되지 않는 것:

- 다운로드된 Game Content
- 다른 GameMode 프로필

완전 새 진행을 시작하려면 삭제 후 같은 모드 프로필을 다시 생성합니다.

삭제는 되돌릴 수 없는 작업이므로 Desktop에서 경고 확인 후 실행합니다.

---

## Quest — 구현 완료 범위

### 사용자 진행 방식

로그 자동 추적을 사용하지 않습니다.

사용자가 게임에서 Quest를 완료하면 `완료`를 누르고, 실수했다면 `완료 취소`합니다.

별도의 Accept 버튼은 없습니다. 확인 가능한 해금 조건을 충족한 미완료 Quest는 준현 헬퍼에서 Current/진행 중으로 간주합니다.

### 정상 상태

- `Current` — 진행 중
- `Locked` — 미래에 충족 가능한 조건 때문에 잠김
- `Unavailable` — 현재 캐릭터/확정 분기상 영구 수행 불가
- `Completed` — 완료

`Indeterminate`는 정상 게임 상태가 아니라 **판정 문제**입니다.

예:

- 필요한 profile 값 미입력
- 지원하지 않는 availability 의미
- 참조 데이터 문제

정상 상태 목록과 분리해 표시합니다.

### 현재 판정 규칙

- player level
- faction
- edition
- prestige
- trader standing
- trader loyalty
- prerequisite Quest Active / Complete / Failed
- disabled
- explicit permanent failure
- 다른 Quest 완료에서 자동 추론되는 taskStatus failure
- prerequisite가 영구 불가일 때 후속 Quest의 Unavailable 전파

### 실패 / 분기

공식 근거: `docs/QUEST_FAILURE_ANALYSIS.md`

2026-08-08 regular live source 기준:

- Quest 510개
- task prerequisite relation 607개
- Failed 포함 prerequisite 24개
- Failed-only prerequisite 4개
- failConditions 50개 / 38 Quest
- 다른 Quest 완료에서 자동 추론 가능한 taskStatus failure 35개 / 23 Quest

따라서 큰 수동 Branch Manager를 만들지 않습니다.

- 자동으로 알 수 있는 failure는 completed Quest facts에서 계산
- 분기 결과 전에는 가능한 성공/실패 미래 경로를 유지
- 결과가 확정되면 불가능 경로를 `Unavailable`
- restartable raid failure는 permanent 상태로 저장하지 않음
- `restartable = false`이고 실제 발생 여부를 앱이 자동으로 알 수 없는 희귀 failure만 `실패 처리 / 실패 취소` 제공

### 시간 지연 해금

실제 게임의 완료 시각을 앱이 알 수 없으므로 `AvailableAfter` 기반 지연은 현재 해금 판정에서 제외합니다.

### Quest 상세

진행에 필요한 정보 중심:

- 목표
- 제출 아이템
- 해금/잠김 이유
- 선행 Quest
- 상태/판정 이유
- Wiki
- 완료 / 완료 취소
- 해당되는 경우만 실패 / 실패 취소

**Quest 보상 전체는 구현하지 않음.**

결정 근거: `docs/QUEST_REWARD_DECISION.md`

인게임에서 쉽게 확인 가능하고 핵심 기능에 필요하지 않아 별도 canonical reward model/importer/UI 유지 비용을 만들지 않습니다.

---

## Hideout — 구현 완료 범위

사용자가 시설별 현재 레벨을 직접 입력합니다.

- `미입력`과 `Lv.0`은 다름
- 미입력을 Lv.0으로 추측하지 않음
- Hideout 화면은 명시된 현재 레벨의 **바로 다음 upgrade**를 표시
- Item 계획은 현재 레벨보다 높은 **모든 미래 upgrade material**을 합산

현재 레벨이 미입력인 시설은 미래 범위를 확정할 수 없으므로 그 시설 관련 보유 Item의 cleanup을 보수적으로 보호합니다.

---

## Future Needed Items / Item — 구현 완료 범위

이 기능이 준현 헬퍼의 핵심 제작 이유입니다.

정의:

> 지금 당장 필요한 물건이 아니라, 현재 캐릭터가 앞으로 사용할 가능성이 있는 물건을 미리 모으고, 패치나 진행 변화로 더 이상 필요하지 않은 보유품을 안전하게 정리하도록 돕는다.

공식 상세: `docs/NEEDED_ITEMS_EXPERIENCE.md`

### 미래 필요량에 포함

- Current Quest 제출 아이템
- 레벨/상인/프레스티지 등 미래 충족 가능한 Locked Quest
- 아직 닫히지 않은 여러 Quest branch
- 불명확한 데이터 의미는 `IndeterminatePotential`로 보수적 포함
- 명시된 Hideout current level 이후 모든 미래 level material

### 제외

- Completed Quest
- 진영 / edition / disabled 등으로 영구 불가 Quest
- 완료/실패가 확정되어 닫힌 branch와 후속 경로
- 이미 지난 Hideout level material

### Inventory

사용자가 FIR / Non-FIR을 직접 입력합니다.

Inventory는 User Progress의 독립 사실이므로 Game Content update가 삭제하지 않습니다.

### `정리 필요`

`InventorySurplusCalculator`가 미래 필요량을 만족하고도 남는 **안전한 초과분만** 계산합니다.

- 필요 0 / 보유 > 0도 표시
- FIR minimum을 먼저 보호
- 남는 FIR은 unrestricted requirement 충족에 사용할 수 있음
- 안전한 FIR / Non-FIR cleanup 수량을 따로 계산
- Game Content에서 Item metadata가 사라져도 user.db에 보유량이 있으면 stable Item ID로 계속 노출

### Item 탭 분류

- `필요`
- `전체`
- `정리 필요`
- `충분`
- `판단 보류`

### cleanup 판단 보호

자동으로 안전하다고 증명할 수 없는 후보는 정리 가능으로 말하지 않습니다.

현재 대표 보호 원인:

- Hideout current level 미입력
- 유동 Quest 제출 후보

### 유동 제출 요구

공식 분석: `docs/FLEXIBLE_QUEST_ITEMS_ANALYSIS.md`

2026-08-08 regular live source에서 필수 `giveItem` 187건 중 다중 Accepted Item ID 목표는 3건.

별도 사용자 선택을 저장하지 않습니다.

예:

```text
A / B / C 중 합계 5개 제출
보유 A2 + B1 + C0
→ 합산 3 / 5
→ 2개 남음
```

FIR 목표라면 허용 후보의 FIR 보유량만 FIR 충족에 사용합니다.

Item 탭에서:

- Quest 이름
- 합산 보유 / 필요
- 남은 수량
- FIR 진행
- 허용 후보 Item 이름

을 그룹 단위로 표시합니다.

그룹이 끝나기 전 후보별 cleanup 수량은 독립적으로 안전하다고 볼 수 없으므로 보호합니다.

### 변화 알림

다음 변화 전후의 Cleanup Plan을 비교합니다.

- Quest 완료 / 완료 취소
- Quest 실패 / 실패 취소
- Hideout level 변경
- Game Content update

새로 정리 가능한 보유품이 생기거나 수량이 늘면 사용자에게 알리고 `정리 필요` 탭으로 연결합니다.

알림을 닫아도 실제 Inventory 초과가 해결되기 전까지 정리 필요 항목은 유지됩니다.

---

## Ammo — 구현 완료 범위

Ammo는 선택 GameMode의 최신 Game Content를 읽는 **read-only 비교 기능**입니다.

Desktop:

- 이름 검색
- caliber dropdown
- 정렬 가능한 비교 표
- 선택 Ammo의 수급처 상세

표시 사실:

- damage
- projectile count
- penetration
- armor damage
- initial speed
- fragmentation chance
- accuracy modifier
- recoil modifier

수급처:

- TraderPurchase
- TraderBarter
- HideoutCraft

가능하면 다음 원천 사실도 표시:

- trader / station level
- 가격 / 재료
- 구매 제한
- craft duration
- output quantity
- Quest unlock

자체 armor effectiveness / tier / recommendation 점수는 만들지 않습니다.

여러 projectile은 임의 합산 평가 대신 `damage × projectile count` 의미를 그대로 보여줍니다.

Ammo는 별도 User Progress를 저장하지 않습니다.

---

## 대형 패치 내구성 — VERIFIED

공식 회귀 계약: `docs/MAJOR_UPDATE_RESILIENCE.md`

2026-08-08 실제 저장 경계를 사용한 시나리오 테스트 추가:

1. Quest required Item A → B + A metadata 삭제
2. required count 10 → 4
3. 새 edition exclusion
4. Hideout future material A → B
5. flexible accepted items A/B → B/C
6. invalid candidate update

검증하는 핵심 약속:

- content.db는 새 패치로 교체 가능
- user.db 사실은 그대로 유지
- 새 Game Content + 같은 User Progress에서 결과만 다시 계산
- 필요 없어진 기존 보유품은 사라지지 않고 `정리 필요`
- invalid candidate는 active content와 user.db를 건드리지 않음
- 정상 교체 전 active는 previous snapshot으로 보존

### 최신 Windows 검증

Windows Server 2025 / .NET SDK 10.0.302:

- Desktop Release build: **0 warnings / 0 errors**
- 전체 tests: **134 passed / 0 failed / 0 skipped**

---

## 지도

후속 기능입니다.

현재 `json.tarkov.dev`의 maps는 Quest map ID/name과 일부 game facts에는 쓸 수 있지만, 우리가 원하는 유지 가능한 interactive map의 기준 이미지 + unified coordinates + full POI + asset/license 문제를 한 번에 해결하지 않습니다.

따라서:

- 기존 Tarkov-Helper 지도 자산/좌표를 공식 설계로 승계하지 않음
- 검증되지 않은 community map 이미지를 제품에 임의 포함하지 않음
- 지도 공급원 문제로 core release를 막지 않음
- Map은 Core에 의존하는 downstream consumer로 설계

현재 공식 `docs/MAP_SOURCE_ANALYSIS.md` 파일은 아직 만들지 않았습니다. 필요할 때 공급원 조사 결과와 함께 작성합니다.

---

## Scanner

후속 기능입니다.

목표 구조:

```text
scan
→ Item ID 식별
→ 기존 Item / Needed Items / Ammo 조회
```

Scanner가 별도 Quest/Item requirement DB나 독립 계산 규칙을 갖지 않습니다.

게임 메모리 hook/injection, 입력 자동화, anti-cheat 우회는 제품 방향이 아닙니다.

---

## 현재 완료된 핵심 UI

상단:

- `퀘스트`
- `은신처`
- `아이템`
- `탄약`
- 상태 텍스트
- Profile dropdown
- `프로필 수정`
- `프로필 삭제`
- `새 프로필`
- `데이터 업데이트`

각 기능은 같은 선택 Profile / 같은 active Game Content를 공유하되, 서로의 내부 상태를 직접 수정하지 않습니다.

진행 사실 변경 후 필요한 workspace를 Core에서 다시 계산합니다.

---

## 현재 남은 주요 작업

### 첫 실사용 버전 전에

1. **사용자 제품 검토**
   - 각 탭/버튼/필터/상태/예외 규칙을 실제 사용 관점에서 사용자와 상세 검토
   - 불편/누락/의도 불일치를 확정 후 수정
2. 실제 Desktop UI polish
   - 검토에서 발견된 가독성/상호작용 문제 중심
   - 기능 없이 장식만 늘리는 작업은 피함
3. 배포 준비
   - Windows publish/package
   - 실제 사용자 PC에서 실행 가능한 형태
   - user.db 보존을 포함한 버전 업데이트 정책
   - 첫 실행 / 네트워크 실패 / content 복구 smoke test

### 후속 기능

4. 지도 — 공급원/asset/coordinate 검증 후
5. Scanner — 기존 Item system의 downstream consumer로

현재 목표가 아님:

- 기존 Tarkov-Helper 자동 migration
- Quest reward 전체 모델
- 로그 기반 Quest 자동 완료
- runtime AI

---

## 다음 작업

**사용자에게 현재 준현 헬퍼의 전체 기능 원리와 Desktop 사용 흐름을 세부적으로 설명하고 제품 검토를 받습니다.**

설명에서는 최소한 다음을 실제 UI와 규칙 기준으로 다룹니다.

- 상단 공통 UI / Profile lifecycle
- 데이터 업데이트와 복구
- Quest 탭의 상태/필터/완료/분기/판정 문제
- Hideout 탭의 레벨 입력과 미래 계산
- Item 탭의 분류/FIR/보유량/정리 필요/유동 제출/판단 보류
- Ammo 탭의 비교/수급처
- 대형 패치 발생 시 실제 데이터 흐름
- 각 기능에서 의도적으로 자동화하지 않는 것과 이유
