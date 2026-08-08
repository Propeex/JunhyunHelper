# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `THIRD USABILITY PASS IMPLEMENTED / FINAL PR VERIFICATION`

현재 작업:

```text
branch: agent/usability-third-pass
PR: #37
```

상세 요구/구현: `docs/THIRD_USABILITY_PASS.md`

첫 실사용 피드백 1~13은 병합 완료되었고, 2차 실사용 피드백 1~7도 PR #36으로 병합 완료되었습니다. 현재는 사용자의 3차 실사용 피드백을 구현한 PR #37의 최종 문서 포함 CI와 테스트 빌드를 마무리하는 단계입니다.

---

## 최우선 제품 원칙

준현 헬퍼는 패치마다 GPT가 데이터를 다시 읽어 수작업으로 넣는 프로그램이 아닙니다.

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

- 일반적인 데이터 변경은 같은 importer/변환 규칙으로 자동 재구축
- 외부 데이터 의미를 모르면 추측하지 않음
- Game Content와 User Progress 분리
- Game Content update가 `user.db`를 덮어쓰지 않음
- 파생 결과를 권위 데이터로 저장하지 않음
- 안전한 cleanup을 증명하지 못하면 보호
- UI는 Core 규칙을 임의로 재구현하지 않음

---

## 기술 기준

- .NET 10
- C#
- WPF Desktop
- SQLite
- SkiaSharp — 외부 image decode/PNG normalize
- Core / Infrastructure / Application / Desktop 4계층
- 별도 backend 없음
- runtime AI/GPT 없음

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

현재 Content snapshot schema: **v2**

v2는 canonical `GameItem` category metadata를 포함합니다. v1 content DB는 온라인 재구축되며 `user.db`는 유지됩니다.

---

## 데이터 원천

### 1차

`json.tarkov.dev`

- items + item categories
- traders
- maps
- tasks
- hideout
- barters
- crafts
- Ammo raw stats

GameMode:

- regular
- pve
- pvp-season

### 보조

- TarkovTracker `tarkov-data-overlay`: editions only
- Tarkov Wiki Ballistics: Ammo 비교 membership + Armor Class 1~6 명시 0~6 effectiveness

Wiki는 raw Ammo 성능의 대체 원천이 아닙니다. 장애/구조 이상은 기본 Game Content나 User Progress를 손상시키지 않습니다.

---

## Profile

현재 구조:

- 한 GameMode당 프로필 하나
- Profile dropdown 내부 `새 프로필`
- `프로필 수정` 안에 삭제
- Player level / Prestige: `- / 값 / +`
- **Fence reputation**: 상단 주요 진행값, 0.1 단위
- 핵심 Trader LL: 게임식 순서
- Lightkeeper/BTR Driver/미래 비핵심 Trader: `특별` 섹션
- 실제 Quest 판정에 필요한 비-Fence standing만 advanced 입력

핵심 Trader 순서(Fence 제외):

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

Core `Indeterminate`는 diagnostic 상태로 유지하지만, 모든 지원 판정을 적용한 뒤에도 남은 residual Indeterminate는 Application 경계에서 **진행 중**으로 표시합니다. 확정 Locked/Unavailable은 변경하지 않습니다.

Quest 상세:

- `위키`
- 목표
- 제출 Item card/list
- Item icon / 이름 / 수량 / 인레이드 여부 / 유동 제출 후보
- 선행 Quest card
- Quest Item 클릭 → Item 상세
- 선행 Quest 클릭 → Quest 상세
- stable ID navigation

Trader/Map filter는 검증된 게임식 순서를 사용합니다. Ground Zero 21+는 Quest filter에서 Ground Zero와 그룹화하되 canonical ID는 보존합니다.

---

## Hideout

- 미입력 = Lv.0
- `- / 현재 레벨 / +`
- 상세는 바로 다음 upgrade
- Needed Items는 현재 level 이후 모든 미래 upgrade material
- station image
- **다음 upgrade 재료는 Item card/list**
  - icon
  - 이름
  - 수량
  - 인레이드 여부

---

## Needed Items / Item

핵심 목적:

> 앞으로 필요할 가능성이 남아 있는 Item을 미리 모으고, 더 이상 필요하지 않은 실제 보유품만 안전하게 정리하도록 돕는다.

포함:

- Current Quest
- 미래 도달 가능한 Locked Quest
- 아직 닫히지 않은 Quest branch
- 안전하게 제외할 수 없는 잠재 요구
- 현재 Hideout level 이후 미래 upgrades

Inventory는 `user.db`의 독립 User Progress입니다.

사용자 표시 용어:

```text
FIR 의미 → 인레이드
Non-FIR 의미 → 일반
```

내부 `Fir/NonFir` 필드명은 호환성을 위해 유지할 수 있습니다.

### 일반 Item 목록 — PR #37

한 row에서:

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

을 직접 비교합니다.

기존 우측 status badge는 제거했습니다.

상세 주 정보:

- 인레이드 필요 N개
- 일반 필요 N개

보유 입력:

- 인레이드 `− / 값 / +`
- 일반 `− / 값 / +`
- +/- 클릭 즉시 저장
- 직접 숫자 입력 + 명시적 저장도 유지

종류 dropdown은 현재 view/search/status filter에 실제 row가 존재하는 종류만 표시합니다. 기본 `필요` 화면에서 필요 Item이 모두 사라진 종류는 dropdown에서도 사라집니다.

### 유동 제출 — PR #37

별도 `유동 제출 보기`를 유지하되 **Quest별 그룹/card**으로 표시합니다.

- Quest 이름 → Quest 상세
- 후보 Item → Item 상세
- 후보 보유량 표시
- 계산은 objective 후보 합계
- 후보 하나를 임의 선택하지 않음
- cleanup은 목표 종료 전 보수적 보호

---

## Ammo — PR #37 포함 최신 상태

읽기 전용 비교 기능입니다.

- 이름 검색 없음
- 구경 dropdown
- 표시 열 선택
- 상세는 숨긴 column 정보도 유지
- 항상 penetration 오름차순 → damage → name
- 최소 수급 경로 summary
- 상세 전체 acquisition
- canonical item image

### 비교 대상

healthy Wiki Ballistics enrichment가 있으면 **현재 Wiki 표와 안전하게 매칭된 탄약만** 표에 표시합니다.

- Wiki에 없는 장난/미사용/비교 대상 외 Ammo 제외
- 그 Ammo만 포함하던 caliber도 dropdown에서 제외
- hard-coded allowlist 없음
- Wiki 장애/비정상 시 기본 Ammo를 임시 표시하고 source 상태 명시

### 구경 표시

raw caliber ID는 보존하고 Desktop은 일반적으로 쓰는 cartridge 이름을 표시합니다.

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

Wiki Ballistics의 명시된 0~6 값만 사용합니다. 자체 heuristic은 없습니다.

6개 cell은 왼쪽부터 Class 1→6이며 **cell 안에는 effectiveness 값만** 표시합니다.

```text
6  6  6  5  3  2
```

작은 armor class 숫자는 중복 표시하지 않습니다.

---

## ScrollBar — PR #37

2차 수정에서 native template을 교체했지만 Width/Height를 모두 고정해 vertical ScrollBar 자체 높이가 작아지는 문제가 있었습니다.

현재:

- vertical: viewport 높이를 stretch, 폭만 12
- horizontal: viewport 너비를 stretch, 높이만 12
- full track + normal thumb
- dark rounded style
- native arrow chrome 없음

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
- Hideout station
- Hideout material Item
- Ammo
- Quest Item

이미지 실패는 non-fatal입니다.

---

## Map / Scanner

상단 탭과 `준비 중` placeholder만 있습니다. 실제 기능은 후속 요구사항 확정 전까지 구현하지 않습니다.

---

## 3차 실사용 피드백 상태 — PR #37

| # | 요구 | 상태 |
|---:|---|---|
| 1 | ScrollBar 전체 track/정상 thumb | 구현 + Windows CI 통과 |
| 2 | 유동 제출 Quest별 그룹 | 구현 + Windows CI 통과 |
| 3 | Hideout 재료 list/card | 구현 + Windows CI 통과 |
| 4 | conventional Ammo caliber | 구현 + Windows CI 통과 |
| 5 | Wiki Ballistics 미등록 Ammo 제외 | 구현 + Windows CI 통과 |
| 6 | FIR → 인레이드 표시 | 구현 + Windows CI 통과 |
| 7 | Item 네 수량/간결 상세/+− 즉시 저장/dynamic category | 구현 + Windows CI 통과 |
| 8 | Quest Wiki → 위키 | 구현 + Windows CI 통과 |
| 9 | Fence 상단 + 핵심/특별 Trader | 구현 + Windows CI 통과 |
| 10 | Armor efficiency cell class 숫자 제거 | 구현 + Windows CI 통과 |

코드 verification checkpoint:

```text
commit 3bb437d7e04fb9fc453c6da00ba5ee756b5f7f48
GitHub Actions 31271990036
Release Desktop build: success
full tests: success
Windows x64 publish/package: success
```

---

## 현재 다음 작업

1. PR #37 문서 변경 포함 최종 CI 확인
2. review thread 최종 확인
3. PR #37 main 병합
4. 새 Windows x64 테스트 package 무결성 확인 후 사용자에게 전달
5. 실제 사용 결과를 다음 피드백으로 반영

새 실사용 피드백이 없다면 이후 큰 기능은 Map 실제 기능과 Scanner 실제 기능의 제품 요구사항 정의입니다.
