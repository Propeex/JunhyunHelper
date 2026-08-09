# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `FOURTH USABILITY PASS MERGED / USER TESTING`

4차 실사용 피드백 1~13은 **PR #39로 main 병합 완료**되었고, 최종 Windows CI와 전달용 ZIP 무결성까지 검증되었습니다.

상세 계약: `docs/FOURTH_USABILITY_PASS.md`

검증 checkpoint:

```text
PR #39: merged
final CI: 31289134464
Release Desktop build: success
full automated tests: success
Windows x64 publish/package: success
artifact upload: success
review threads: none
outer artifact ZIP CRC: success
inner Windows ZIP CRC: success
flat delivery ZIP CRC: success
delivery ZIP SHA-256: 65d67a6ed60dbc39b3a51e0e640ca58b636560058ee575fa418f47aa66a831f1
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

유동 제출:

- 별도 view
- Quest별 group
- group/candidate full-width + left aligned
- 후보 Item click → Item
- Quest click → Quest
- cleanup 보수적 보호

종류 dropdown은 현재 view/search/filter에 실제 row가 있는 category만 표시합니다.

평상시 상단 status는:

```text
정리 필요 N
```

만 표시합니다. 작업 중에는 progress/save/error 메시지가 일시적으로 우선합니다.

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

추가 UX:

- caliber 즐겨찾기 toggle
- 즐겨찾기 전용 dropdown
- `ammo-favorites.json` local persistence
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

## Map / Scanner

탭과 `준비 중` placeholder만 있습니다. 실제 기능은 후속 요구사항 확정 전까지 구현하지 않습니다.

---

## 실사용 피드백 상태

- 첫 실사용 피드백: merged
- 2차: PR #36 merged
- 3차: PR #37 merged
- 4차: **PR #39 merged / user testing**

---

## 현재 다음 작업

1. 사용자가 4차 Windows 테스트 빌드를 실제 사용
2. 발견된 오류/불편을 다음 실사용 피드백으로 반영
3. 실사용 안정화 후 Map 실제 기능 / Scanner 실제 기능 요구사항 정의
