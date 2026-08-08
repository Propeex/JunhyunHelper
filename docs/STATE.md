# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `THIRD USABILITY PASS VERIFIED / USER TEST BUILD NEXT`

3차 실사용 피드백 구현은 PR #37에서 완료되었고 Windows CI로 검증되었습니다.

상세 요구/구현: `docs/THIRD_USABILITY_PASS.md`

검증 checkpoint:

```text
PR #37
GitHub Actions 31272266508
Release Desktop build: success
full automated tests: success
Windows x64 publish: success
ZIP/artifact upload: success
review threads: none
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
→ User Progress와 결합
→ 파생 결과 계산
→ Desktop 표시
```

- 일반적인 데이터 내용 변화는 같은 importer/변환 규칙으로 자동 재구축
- 의미를 모르는 외부 데이터는 추측하지 않음
- Game Content와 `user.db` 분리
- update 실패가 기존 정상 Game Content/User Progress를 손상시키지 않음
- Needed Items / cleanup / Quest 상태 같은 파생 결과는 권위 데이터로 저장하지 않음
- 안전한 cleanup을 증명하지 못하면 보호
- runtime AI/GPT 없음

---

## 기술/저장 기준

- .NET 10 / C# / WPF
- SQLite
- SkiaSharp image decode + PNG normalize
- Core / Infrastructure / Application / Desktop 4계층

기본 데이터 루트:

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
```

Content snapshot schema는 현재 **v2**입니다. v1 content DB는 온라인 재구축되며 `user.db`는 유지됩니다.

---

## 데이터 원천

1차 Game Content: `json.tarkov.dev`

- items + categories
- traders
- maps
- tasks
- hideout
- barters
- crafts
- Ammo raw stats

지원 모드:

- regular
- pve
- pvp-season

보조 원천:

- TarkovTracker overlay: edition rules only
- Tarkov Wiki Ballistics: Ammo 비교 membership + Class 1~6의 명시 0~6 effectiveness

Wiki는 raw Ammo 성능의 대체 원천이 아닙니다.

---

## Profile

- 한 GameMode당 프로필 하나
- Profile dropdown 안 `새 프로필`
- `프로필 수정` 안 삭제
- Player level / Prestige: `- / 값 / +`
- Fence reputation: 상단 주요 진행값, 0.1 단위
- 핵심 Trader LL: 게임식 순서
- Lightkeeper / BTR Driver / future non-core traders: `특별`
- 필요한 비-Fence standing만 advanced 입력

핵심 Trader(Fence 제외):

```text
Prapor → Therapist → Skier → Peacekeeper → Mechanic
→ Ragman → Jaeger → Ref
```

---

## Quest

사용자 화면 상태:

- 진행 중
- 잠김
- 사용 불가
- 완료

Core `Indeterminate`는 diagnostic으로 보존하되, 모든 지원 판정 뒤에도 남은 residual Indeterminate는 사용자 화면에서 **진행 중**으로 취급합니다. 확정 Locked/Unavailable은 변경하지 않습니다.

Quest 상세:

- `위키`
- 제출 Item card/list
- icon / 이름 / 수량 / 인레이드 여부 / 유동 제출 후보
- Quest Item 클릭 → Item 상세
- 선행 Quest 클릭 → Quest 상세
- stable ID navigation

Ground Zero 21+는 Quest filter에서 Ground Zero와 그룹화하되 canonical ID는 보존합니다.

---

## Hideout

- 미입력 = Lv.0
- `- / 현재 레벨 / +`
- 상세는 바로 다음 upgrade
- Needed Items는 현재 level 이후 모든 미래 upgrade material
- station image
- 다음 upgrade 재료는 icon/name/수량/인레이드 여부 card/list

---

## Needed Items / Item

핵심 목적:

> 앞으로 필요할 가능성이 남아 있는 Item을 미리 모으고, 더 이상 필요하지 않은 실제 보유품만 안전하게 정리한다.

사용자 표시 용어:

```text
FIR 의미 → 인레이드
Non-FIR 의미 → 일반
```

내부 `Fir/NonFir` 식별자는 저장 호환성을 위해 유지할 수 있습니다.

일반 목록은 한 row에서 다음 네 값을 비교합니다.

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

기존 우측 status badge는 제거했습니다.

상세 주요 요구량:

- 인레이드 필요 N개
- 일반 필요 N개

보유 입력:

- 인레이드 `− / 값 / +`
- 일반 `− / 값 / +`
- +/- 클릭 즉시 저장
- 직접 숫자 입력 + 명시적 저장 유지

종류 dropdown은 현재 view/search/status filter에 실제 row가 있는 종류만 표시합니다.

유동 제출은 별도 view에서 **Quest별 group/card**으로 표시합니다.

- Quest 이름 → Quest 상세
- 후보 Item → Item 상세
- 후보 하나를 임의 선택하지 않음
- objective 후보 보유량 합계로 진행 계산
- cleanup은 목표 종료 전 보수적으로 보호

---

## Ammo

read-only 비교 기능입니다.

- 이름 검색 없음
- caliber dropdown
- 표시 열 선택
- penetration 오름차순 → damage → name
- 최소 acquisition summary + 상세 전체 acquisition
- item image

### 비교 대상

healthy Wiki Ballistics enrichment가 있으면 **현재 Wiki 표와 안전하게 매칭된 탄약만** 표시합니다.

- Wiki 미등록 장난/미사용/비교 대상 외 Ammo 제외
- 그 Ammo만 있던 caliber도 dropdown에서 제외
- hard-coded Ammo allowlist 없음
- Wiki 장애/비정상 시 raw Ammo Game Content를 임시 표시하고 source 상태 명시

### caliber 표시

raw ID는 보존하고 익숙한 cartridge 이름을 표시합니다.

예:

```text
.45 ACP
.357 Magnum
.300 Blackout
.338 Lapua Magnum
.50 AE
.366 TKM
12/70
```

### Armor effectiveness

Wiki Ballistics의 명시된 0~6 값만 사용하고 자체 heuristic은 만들지 않습니다.

6칸은 왼쪽부터 Class 1→6이며 cell 안에는 effectiveness 숫자만 표시합니다.

```text
6  6  6  5  3  2
```

작은 armor class 숫자는 cell 안에 중복 표시하지 않습니다.

---

## ScrollBar

현재 구현:

- vertical: viewport 높이를 stretch, 폭만 12
- horizontal: viewport 너비를 stretch, 높이만 12
- full track + normal thumb
- dark rounded style
- native arrow chrome 없음

이전처럼 ScrollBar 자체 Width/Height를 동시에 고정해 작은 공처럼 보이게 하지 않습니다.

---

## 이미지 cache

```text
canonical URL
→ bytes
→ SkiaSharp decode
→ size/validity check
→ PNG normalize
→ image-cache
→ WPF
```

대상:

- Item
- Hideout station/material
- Ammo
- Quest Item

이미지 실패는 non-fatal입니다.

---

## Map / Scanner

상단 탭과 `준비 중` placeholder만 있습니다. 실제 기능은 후속 요구사항 확정 전까지 구현하지 않습니다.

---

## 실사용 피드백 상태

- 첫 실사용 피드백 1~13: 구현/병합 완료
- 2차 실사용 피드백 1~7: PR #36 구현/검증/병합 완료
- 3차 실사용 피드백 1~10: PR #37 구현/Windows CI 검증 완료

3차 상세: `docs/THIRD_USABILITY_PASS.md`

---

## 현재 다음 작업

1. 검증된 PR #37을 main에 반영
2. Windows x64 artifact를 다운로드해 ZIP 중첩/CRC를 직접 확인
3. 사용자에게 새 테스트 빌드 전달
4. 실제 PC 사용 결과를 다음 피드백으로 반영

새 실사용 피드백이 없다면 이후 큰 기능은 Map 실제 기능과 Scanner 실제 기능의 제품 요구사항 정의입니다.
