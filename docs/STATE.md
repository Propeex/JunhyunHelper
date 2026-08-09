# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `FIFTH USABILITY FIXES IMPLEMENTED / MAP SOURCE DESIGN`

현재 작업:

```text
branch: agent/fifth-usability-map-source
PR: #41
```

4차 실사용 피드백은 PR #39로 main 병합 완료되어 사용자 테스트 중입니다.

5차에서 새로 확인된 Ammo 즐겨찾기 이동 문제와 Item 용도 필터는 구현 완료했고 Windows CI를 통과했습니다. Map은 실제 기능 구현 전에 장기 유지 가능한 데이터 공급원을 조사했으며, **동적 gameplay/location data source는 확보 가능**한 것으로 확인했습니다. 실제 Map UI와 지도 배경 artwork 선택은 아직 제품 설계 단계입니다.

상세:

- `docs/FIFTH_USABILITY_PASS.md`
- `docs/MAP_DATA_SOURCE_ANALYSIS.md`

5차 구현 checkpoint:

```text
CI: 31290336689
Release Desktop build: success
full automated tests: success
Windows x64 publish/package: success
artifact upload: success
```

---

## 최우선 제품 원칙

준현 헬퍼는 패치마다 GPT가 새 게임 데이터를 다시 해석해 수작업으로 넣는 프로그램이 아닙니다.

```text
온라인 Tarkov 데이터
→ 다운로드
→ 외부 형식 검증
→ canonical model 변환
→ candidate SQLite
→ 검증
→ active content 교체
→ icon 선다운로드
→ User Progress와 결합
→ 파생 결과 계산
→ Desktop 표시
```

- 일반적인 데이터 내용 변화는 같은 importer/변환 규칙으로 자동 재구축
- 의미를 모르는 외부 데이터는 추측하지 않음
- Game Content와 `user.db` 분리
- update 실패가 기존 정상 Game Content/User Progress를 손상시키지 않음
- runtime AI/GPT 없음
- 유동 제출에서 실제 사용 Item처럼 프로그램이 알 수 없는 사실은 임의 추정하지 않음
- Map도 가능한 한 패치 때 수동 좌표 갱신이 아니라 온라인 source → canonical 변환 구조를 사용

---

## 기술 / 저장

- .NET 10 / C# / WPF
- SQLite
- SkiaSharp image decode + PNG normalize
- Core / Infrastructure / Application / Desktop

기본 root:

```text
%LocalAppData%/JunhyunHelper
```

주요 저장:

```text
user.db
content/<game-mode>/content.db
content/<game-mode>/content.candidate.db
content/<game-mode>/content.previous.db
image-cache/
ammo-favorites.json
```

### Content schema

현재 **v3**.

v3에서 Ammo의 current Wiki Ballistics **표 등록 여부**를 Class 1~6 effectiveness와 별도 사실로 저장합니다.

- v2 content snapshot → 온라인 source에서 자동 재구축
- `user.db`는 유지

Map 실제 importer/schema는 아직 추가하지 않았습니다. Map source 설계 확정 뒤 필요하면 다음 content schema에서 추가합니다.

### User Progress

기존 Profile / Quest / Hideout / Trader / Inventory에 더해 자동 inventory bookkeeping용 실제 소비 ledger를 additive JSON field로 저장합니다.

- Quest별 실제 자동 차감량
- Hideout station + target level별 실제 자동 차감량

SQLite table schema는 그대로여서 기존 user.db와 하위 호환됩니다.

Prestige legacy null은 제품상 **0**으로 정규화합니다.

---

## Profile

- 한 GameMode당 profile 하나
- Profile dropdown 안 `새 프로필`
- `프로필 수정` 안 삭제
- Player level: `- / 값 / +`
- Prestige: **기본 0**, 미입력 없음
- Fence reputation: 상단 주요 진행값, 0.1 단위
- 핵심 Trader: 게임식 순서
- 일반 상인 밖 Trader: `특별` Expander, 기본 접힘
- 필요한 비-Fence standing만 `고급` 입력

---

## Quest

사용자 상태:

- 진행 중
- 잠김
- 사용 불가
- 완료

끝까지 남은 Core `Indeterminate`는 Application 제품 경계에서 진행 중으로 보여주되 diagnostic reason은 보존합니다.

상세 연결:

- Quest Item → Item
- prerequisite Quest → Quest
- `위키`

### 완료와 Inventory

고정 제출 요구는 Quest 완료와 함께 tracked Inventory에서 자동 차감합니다.

```text
인레이드 필수 → 인레이드만
일반 요구 → 일반 우선, 부족하면 인레이드
```

유동 제출 후보는 실제 어느 Item을 사용했는지 알 수 없으므로 자동 차감하지 않습니다.

완료 취소 시 소비 ledger가 있으면 복원 여부를 묻습니다.

- 예: 정확한 실제 차감량 복원 + ledger 제거
- 아니오: 차감 유지 + ledger 유지
- 취소: 완료 취소 중단

ledger가 남은 상태에서 다시 완료해도 같은 재료를 중복 차감하지 않습니다.

### Quest Map filter

- Ground Zero / Ground Zero 21+ → `Ground Zero`
- Factory day/night → `Factory`
- canonical Map ID는 변경하지 않음

일반 refresh에서는 scroll 위치를 보존하고, 사용자가 링크 이동을 명시적으로 요청했을 때만 목표 row로 이동합니다.

---

## Hideout

- 미입력 = Lv.0
- `- / 현재 level / +`
- 다음 upgrade material card/list
- material click → Item 상세
- Item 상세 Hideout source click → 해당 facility

업그레이드 시 고정 재료를 Inventory에서 자동 차감하며 Quest와 같은 인레이드/일반 우선순위를 사용합니다.

rollback 시 복원 여부를 묻습니다. 복원하지 않은 ledger는 재업그레이드 중복 차감을 막기 위해 유지합니다.

---

## Needed Items / Item

일반 row:

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

상세:

- 인레이드 필요 N
- 일반 필요 N
- `− / 수량 / +`, +/- 즉시 저장
- 직접 입력 저장
- Quest / Hideout 필요 출처를 동일한 clickable block으로 표시

필터:

- 검색
- Item 종류
- **용도: 모든 용도 / 퀘스트용 / 은신처용**
- 필요 상태: 필요 / 전체 / 정리 필요 / 충분 / 판단 보류

Quest와 Hideout 모두에 필요한 Item은 양쪽 용도 필터 모두에서 표시합니다. flexible Quest candidate도 퀘스트용으로 취급합니다.

유동 제출:

- 별도 view
- Quest별 group
- group/candidate full-width + left aligned
- 후보 Item click → Item
- Quest click → Quest
- cleanup 보수적 보호

---

## Ammo

raw 성능: `json.tarkov.dev`

Wiki Ballistics 보조 사실:

1. 현재 비교 표 등록 여부
2. Armor Class 1~6의 0~6 effectiveness

두 의미를 분리합니다. 등록된 Ammo는 effectiveness를 안전하게 매칭하지 못해 `?`가 되더라도 표에서 제거하지 않습니다.

Wiki source가 healthy하면 현재 Wiki 등록 Ammo만 비교합니다. source 장애/구조 이상이면 raw Ammo를 임시 표시하여 빈 표나 손상된 content를 만들지 않습니다.

대표 caliber label:

```text
.308 Marlin Express
9.3x64mm
9x18mm Makarov
.50 Action Express
12.7x108mm
.45 ACP
.300 Blackout
.338 Lapua Magnum
.366 TKM
12/70
```

### 즐겨찾기

- 현재 caliber `☆/★ 즐겨찾기` toggle
- `ammo-favorites.json` local persistence
- 즐겨찾기 목록은 선택 상태를 가지는 ComboBox가 아니라 **shortcut popup**
- 각 favorite caliber는 button/action이며 누를 때마다 해당 caliber로 이동
- 일반 caliber에서 다른 값을 선택한 뒤에도 같은 favorite를 다시 누르면 정상 이동

추가:

- Ammo acquisition unlock Quest click → Quest
- penetration → damage → name 고정 오름차순

---

## 이미지

Game Content update 성공 후 제품에서 사용하는 icon을 선다운로드합니다.

대상:

- Quest 제출 Item 후보
- Hideout material
- Ammo Item
- Hideout station

동일 Item은 공통 `item-{id}` cache key를 사용합니다.

```text
URL
→ download
→ SkiaSharp decode
→ validation
→ PNG normalize
→ image-cache
→ WPF
```

개별 이미지 실패는 Game Content update 실패가 아닙니다.

---

## Map

현재 Map 탭 자체는 placeholder입니다. 실제 지도 기능은 아직 구현하지 않았습니다.

### 데이터 공급원 조사 결과

동적 gameplay/location data의 우선 source:

```text
json.tarkov.dev/<game-mode>/maps
```

현재 Tarkov.dev 공개 구현에서 확인되는 map data 범위에는 다음이 포함됩니다.

- spawn
- extract
- transit
- boss / spawn location
- lock
- hazard
- loot container / loose loot
- switch
- stationary weapon
- artillery
- BTR stop

지도 표시용 좌표/레이아웃 metadata는 Tarkov.dev 공개 map configuration에서 다음을 얻을 수 있는 구조를 우선 검토합니다.

- bounds
- transform
- coordinate rotation
- zoom range
- floor/layer + height range
- SVG/tile asset reference

Gameplay data와 visual layout을 분리해 canonical model로 변환하는 방향입니다.

### 지도 artwork

`the-hideout/tarkov-dev-svg-maps`는 layered SVG map source를 공개하고 있으나 license가 **CC BY-NC-SA 4.0**입니다.

따라서 attribution / non-commercial / share-alike 의무가 있고 radar·ESP·cheat client·pixel-bot 같은 부정행위 소프트웨어 사용을 명시적으로 금지합니다.

동적 데이터 공급원은 확보 가능하다고 판단하지만, 실제 배경 artwork로 이 자산을 사용할지는 사용자 제품 판단 후 확정합니다. Tarkov.dev site code의 MIT license를 map artwork에 확대 적용하지 않습니다.

상세: `docs/MAP_DATA_SOURCE_ANALYSIS.md`

---

## Scanner

탭과 `준비 중` placeholder만 있습니다. 실제 기능은 제품 요구사항 확정 전까지 구현하지 않습니다.

Map artwork license의 cheating prohibition과 충돌하지 않도록 향후 Scanner는 정상적인 화면 인식 보조와 실시간 레이더/ESP 성격 기능을 명확히 구분해야 합니다.

---

## 실사용 피드백 상태

- 첫 실사용 피드백: merged
- 2차: PR #36 merged
- 3차: PR #37 merged
- 4차: PR #39 merged / user testing
- 5차: **PR #41 — Ammo favorite shortcut + Item 용도 filter 구현 완료 / Map source 분석 완료 / 최종 문서 검증 중**

---

## 현재 다음 작업

1. PR #41 최종 documentation-inclusive CI 확인 및 병합
2. 사용자에게 5차 UX 수정 결과와 Map source 조사 결과 전달
3. Map artwork로 CC BY-NC-SA SVG source 사용 여부를 제품 관점에서 결정
4. Map 실제 사용자 흐름/marker 범위/층 전환/Quest 연동을 확정
5. 확정 후 Map canonical importer + Desktop 구현 시작
