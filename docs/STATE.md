# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `SECOND USABILITY PASS MERGED / USER TESTING NEXT`

첫 실사용 피드백 1~13은 구현/병합 완료되었습니다.

첫 통합 Windows 빌드를 실제 사용한 뒤 받은 **2차 실사용 피드백 1~7**도 PR #36으로 구현·검증·main 병합 완료했습니다.

상세 결정: `docs/SECOND_USABILITY_PASS.md`

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

- 일반적인 데이터 변경은 프로그램이 같은 importer/변환 규칙으로 다시 DB를 만들 수 있어야 함
- 같은 입력에는 같은 결과
- 외부 데이터 의미를 모르면 추측하지 않음
- Game Content와 User Progress를 분리
- Game Content 업데이트가 `user.db`를 덮어쓰지 않음
- Needed Items / cleanup / Quest 상태 같은 파생 결과를 진실의 원천으로 저장하지 않음
- 안전한 cleanup을 증명할 수 없으면 보호
- UI는 Core 규칙을 임의 재구현하지 않음

---

## 기술 기준

- .NET 10
- C#
- WPF Desktop
- SQLite
- SkiaSharp — 외부 이미지 decode/PNG normalize
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

---

## 데이터 원천

### 1차 Game Content

`json.tarkov.dev`

- items
- item category metadata
- traders
- maps
- tasks
- hideout
- barters
- crafts
- Ammo raw stats

지원 GameMode:

- regular
- pve
- pvp-season

### 보조 원천

- TarkovTracker `tarkov-data-overlay`: editions 정보만
- Escape from Tarkov Wiki Ballistics: Ammo Armor Class 1~6의 명시된 0~6 effectiveness만 optional enrichment

Wiki enrichment 실패는 기본 Game Content 업데이트 실패가 아닙니다.

---

## Game Content update

안전 순서:

1. 기존 active 유지
2. 온라인 원천 다운로드
3. canonical 변환
4. 관계/필수 값 검증
5. candidate DB 작성
6. candidate read-back 검증
7. 성공 시에만 active 교체
8. 실패 시 기존 active와 `user.db` 유지

진행률 UI는 실제 source 완료 수와 실제 pipeline 단계만 사용합니다.

### Content snapshot schema

현재 schema: **v2**

v2에서 canonical `GameItem`에 Tarkov item category metadata가 추가되었습니다.

이전 테스트 빌드의 v1 `content.db`는 새 빌드 실행 시 온라인 데이터로 자동 재구축됩니다.

`user.db`는 별도이므로 다음 User Progress는 유지됩니다.

- Profile
- Quest 완료/실패
- Hideout level
- Trader 진행
- FIR / Non-FIR Inventory

---

## Profile

구현 완료:

- 한 GameMode당 프로필 하나
- Profile dropdown 내부 새 프로필
- 프로필 수정 안에 삭제
- Player level / Prestige: `- / 값 / +`
- 일반 Trader: LL 중심
- Fence: standing 0.1 단위
- 필요한 비-Fence standing만 고급 입력

설정 수정/데이터 업데이트는 User Progress를 보존합니다.

---

## Quest

Core가 계산하는 상태:

- Current
- Locked
- Unavailable
- Completed
- Indeterminate — 내부 진단 상태

### residual Indeterminate 정책 — PR #36

사용자 화면에서 끝까지 남은 `Indeterminate` Quest는 **Current(진행 중)** 으로 취급합니다.

- Core diagnostic reason은 보존
- 확정 가능한 Locked/Unavailable은 변경하지 않음
- Application 제품 경계에서 residual Indeterminate만 Current로 승격
- 사용자가 완료 처리 가능
- 별도 `판정 문제` 목록으로 사용 흐름을 막지 않음

### Quest 상세 UI — PR #36

제출 Item은 문자열 dump가 아니라 card/list로 표시합니다.

각 Item:

- icon
- 이름
- 수량
- FIR
- 유동 제출 후보 표시

연결:

- Quest Item 클릭 → Item 상세
- 선행 Quest 클릭 → 해당 Quest 상세

navigation은 표시 이름이 아니라 stable ID를 사용합니다.

### 기존 구현

- 별도 Accept 버튼 없음
- 수주 가능 Quest는 Current로 간주
- 완료/완료 취소
- 필요한 희귀 영구 실패만 실패/실패 취소
- Trader/Map 게임식 filter order
- Ground Zero / Ground Zero 21+는 Quest filter에서 하나의 Ground Zero 그룹

---

## Hideout

- 미입력 = Lv.0
- `- / 현재 레벨 / +`
- 상세는 바로 다음 upgrade
- Needed Items는 현재 레벨보다 높은 모든 미래 upgrade material
- canonical station image

---

## Needed Items / Item

핵심 목적:

> 앞으로 사용할 가능성이 있는 아이템을 미리 모으고, 더 이상 필요하지 않은 실제 보유품만 안전하게 정리하도록 돕습니다.

Inventory:

- FIR / Non-FIR User Progress
- Game Content 업데이트와 독립

cleanup:

- 미래 필요량 충족 후 남는 안전한 초과분만 표시
- FIR 요구 보호
- 유동 제출 후보 보호
- 안전성을 증명하지 못하면 판단 보류

### 유동 제출 분리 — PR #36

유동 제출 때문에만 목록에 들어온 후보는 일반 Item 목록에서 제외합니다.

별도 `유동 제출 보기`에서 모든 후보에 계속 접근할 수 있습니다.

고정 필요/실제 보유에도 관련 있는 후보는 일반 목록에 남을 수 있습니다.

### Item 종류 분류 — PR #36

`json.tarkov.dev`의 `item.categories` + `itemCategories.normalizedName`을 importer가 읽습니다.

Desktop 상위 분류:

```text
무기
무기 부품
장비
탄약
의약품
식량/음료
물물교환
열쇠
정보
특수 장비
퀘스트 아이템
화폐
지도
기타
```

알 수 없는 미래 category는 누락하지 않고 `기타`로 표시합니다.

### Item → Quest — PR #36

Item 필요 출처의 Quest 또는 유동 제출 Quest를 클릭하면 해당 Quest 상세로 이동합니다.

Quest 상세에서 선택한 Item이 현재 Needed 목록에 없더라도 canonical Item에 존재하면 reference detail을 열 수 있습니다.

---

## 이미지 cache — PR #36 수정 완료

기존 문제:

- canonical icon URL은 존재하지만 WebP 계열 source를 WPF가 PC codec 환경에 따라 decode하지 못해 아이콘이 보이지 않을 수 있었음

현재 pipeline:

```text
canonical URL
→ download bytes
→ SkiaSharp decode
→ 크기/유효성 검증
→ PNG normalize
→ image-cache
→ WPF
```

대상:

- Item
- Hideout
- Ammo
- Quest Item

이미지 실패는 계속 non-fatal입니다.

---

## ScrollBar — PR #36 수정 완료

- vertical/horizontal 전체 template 교체
- native arrow chrome 제거
- rounded dark track/thumb
- hover/drag accent

---

## Ammo

구현 완료:

- 구경 중심 비교
- 열 표시/숨김
- 관통력 오름차순 → 피해량 → 이름
- 최소 수급 경로 요약
- 상세 전체 수급 경로
- item icon
- Wiki Ballistics Class 1~6 0~6 effectiveness optional enrichment
- unknown은 `?`, 임의 heuristic 없음

상세: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

---

## Map / Scanner

상단 탭과 `준비 중` placeholder만 있습니다.

실제 기능은 아직 후속 범위입니다.

---

## 2차 실사용 피드백 상태

| 번호 | 요구 | 상태 |
|---:|---|---|
| 1 | 아이콘 미표시 수정 | 구현/검증/병합 완료 (PR #36) |
| 2 | residual 판정불가 Quest를 진행 중 처리 | 구현/검증/병합 완료 (PR #36) |
| 3 | ScrollBar 디자인 수정 | 구현/검증/병합 완료 (PR #36) |
| 4 | 유동 제출 후보 별도 분류 | 구현/검증/병합 완료 (PR #36) |
| 5 | Tarkov식 Item 종류 분류 | 구현/검증/병합 완료 (PR #36) |
| 6 | Quest Item icon/list UI | 구현/검증/병합 완료 (PR #36) |
| 7 | Quest ↔ Item / 선행 Quest 상호 이동 | 구현/검증/병합 완료 (PR #36) |

검증 checkpoint:

- Windows Release Desktop build 성공
- 전체 automated test 성공
- Windows x64 publish/ZIP 성공
- documentation-inclusive CI 성공
- unresolved review thread 없음

---

## 현재 다음 작업

1. PR #36 코드가 포함된 새 Windows x64 테스트 빌드를 사용자에게 전달
2. 기존 테스트 데이터로 v1 → v2 content 자동 재구축 확인
3. 실제 PC에서 아이콘/스크롤/분류/상호 이동 사용성 확인
4. 추가 실사용 피드백 반영
5. 새 요구가 없다면 Map 실제 기능과 Scanner 실제 기능의 제품 요구사항 정의
