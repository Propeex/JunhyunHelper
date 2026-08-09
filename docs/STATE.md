# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP IMPLEMENTED / WINDOWS USER TESTING / MAP READABILITY PASS VALIDATION`

Map 1차 기능은 구현되어 Windows 실사용 검증 중입니다. 시작·초기화·지도 자산 복구·Windows 파일 잠금 문제는 해결됐고, 실제 첫 지도 화면에서 확인된 **층 이름 표시 버그와 낮은 지도 시인성**을 현재 readability pass에서 수정/검증 중입니다.

현재 Map 관련 흐름:

```text
PR #43 — Map 자동 업데이트 + SVG 라이선스 정책
PR #44 — Map 탭 / Quest marker / 위치 추적 / MiniMap
PR #45 — 대량 loot marker renderer 안정화
PR #46 — Map lazy-load / startup diagnostics / self-contained package
PR #47 — 초기 marker CheckBox NRE 수정
PR #48 — missing map-cache self-heal / 지도별 부분 복구 / SVG source fallback
PR #50 — Windows FileShare.None 다운로드 검증 실패 수정
readability pass — floor Name 표시 + high-contrast readable SVG presentation (현재 검증 중)
```

상세 문서:

- `docs/PRODUCT.md`
- `docs/ARCHITECTURE.md`
- `docs/MAP_PRODUCT_DESIGN.md`
- `docs/MAP_IMPLEMENTATION.md`
- `docs/MAP_PERFORMANCE.md`
- `docs/MAP_STARTUP_RECOVERY.md`
- `docs/MAP_UI_INIT_FIX.md`
- `docs/MAP_ASSET_RECOVERY.md`
- `docs/MAP_DATA_SOURCE_ANALYSIS.md`
- `docs/MAP_VISIBILITY_ANALYSIS.md`

---

## 최우선 제품 원칙

준현 헬퍼는 패치마다 GPT가 새 게임 데이터를 다시 해석해 수작업으로 넣는 프로그램이 아닙니다.

```text
온라인 Tarkov 데이터
→ 다운로드
→ 외부 형식 검증
→ canonical model 변환
→ candidate SQLite / presentation candidate
→ 검증
→ active 교체
→ User Progress와 결합
→ 파생 결과 계산
→ Desktop 표시
```

- 일반적인 데이터 내용 변화는 같은 importer/변환 규칙으로 자동 재구축
- 의미를 모르는 외부 데이터는 추측하지 않음
- Game Content와 `user.db` 분리
- update 실패가 기존 정상 Game Content/User Progress를 손상시키지 않음
- runtime AI/GPT 없음
- 프로그램이 실제 사용 사실을 알 수 없는 유동 제출 등은 임의 추정하지 않음
- Map gameplay data와 Map artwork/layout은 분리하고 독립적으로 갱신/복구
- Map도 수동 좌표 DB가 아니라 온라인 source → canonical 변환 구조를 사용

---

## 기술 / 저장 / 배포

- .NET 10 / C# / WPF
- SQLite
- Core / Infrastructure / Application / Desktop
- SkiaSharp image decode + PNG normalize
- SharpVectors WPF SVG rendering

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
map-cache/active/
map-cache/candidate/
map-cache/previous/
map-settings.json
map-markers.json
map-bulk-marker-settings.json
ammo-favorites.json
logs/startup.log
```

### Windows test package

현재 Windows 테스트 전달본은 **self-contained folder ZIP**입니다.

- ZIP 전체를 새 폴더에 압축 해제
- 폴더 안 `JunhyunHelper.exe` 실행
- EXE만 따로 분리해 실행하지 않음
- 별도 .NET 설치 불필요

CI publish 검증 최소 파일:

```text
JunhyunHelper.exe
JunhyunHelper.dll
SharpVectors.Converters.Wpf.dll
SharpVectors.Rendering.Wpf.dll
```

앱 시작/dispatcher 예외는 `%LocalAppData%/JunhyunHelper/logs/startup.log`에 기록합니다.

### Content schema

현재 **v4**.

- v2: Item category metadata
- v3: Wiki Ballistics membership와 effectiveness 분리
- v4: dynamic Map marker + Quest objective world geometry

이전 Game Content snapshot은 온라인 source에서 자동 재구축합니다. `user.db`는 유지합니다.

---

## Profile

- 한 GameMode당 profile 하나
- Profile dropdown 안 `새 프로필`
- `프로필 수정`에서 삭제
- Player level: `- / 값 / +`
- Prestige 기본 0
- Fence reputation 상단 주요 진행값
- 핵심 Trader 게임식 순서
- 기타 Trader는 `특별` Expander, 기본 접힘

---

## Quest

사용자 상태:

- 진행 중
- 잠김
- 사용 불가
- 완료

연결:

- Quest Item → Item
- prerequisite Quest → Quest
- `위키`
- Map Quest list/marker → Quest

고정 제출 요구는 Quest 완료와 함께 tracked Inventory에서 자동 차감합니다.

```text
인레이드 필수 → 인레이드만
일반 요구 → 일반 우선, 부족하면 인레이드
```

유동 제출 후보는 실제 사용 Item을 알 수 없으므로 자동 차감하지 않습니다.

완료 취소 시 실제 자동 소비 ledger가 있으면 복원 여부를 묻습니다.

Quest Map grouping:

- Ground Zero / Ground Zero 21+ → `Ground Zero`
- Factory day/night → `Factory`
- canonical Map ID는 보존

---

## Hideout

- 미입력 = Lv.0
- `- / 현재 level / +`
- 다음 upgrade material card/list
- material → Item
- Item Hideout source → facility
- upgrade 시 고정 재료 자동 차감
- rollback 시 ledger 기반 정확한 복원 선택

---

## Needed Items / Item

목록 수량:

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

필터:

- 검색
- Item 종류
- 용도: `모든 용도 / 퀘스트용 / 은신처용`
- 필요 상태: `필요 / 전체 / 정리 필요 / 충분 / 판단 보류`

Quest와 Hideout 모두에 필요한 Item은 두 용도 필터 모두에 표시합니다.

유동 제출은 Quest별 별도 group이며 후보 Item/Quest로 이동할 수 있고 cleanup은 보수적으로 보호합니다.

---

## Ammo

raw 성능: `json.tarkov.dev`

Wiki Ballistics 보조 사실:

1. 현재 비교 표 membership
2. Armor Class 1~6의 0~6 effectiveness

Wiki source가 healthy하면 현재 Wiki 등록 Ammo만 비교합니다. effectiveness 매칭 실패만으로 등록 Ammo를 제거하지 않습니다.

즐겨찾기:

- 현재 caliber `☆/★ 즐겨찾기`
- `ammo-favorites.json`
- favorite list는 selection ComboBox가 아니라 shortcut popup
- 같은 favorite를 언제든 다시 눌러 이동 가능

---

## 일반 이미지

Game Content update 성공 후 제품에서 사용하는 icon을 선다운로드합니다.

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

# Map

## 제품 상태

`IMPLEMENTED / WINDOWS USER TESTING / READABILITY PASS`

Map 탭은 앱 startup과 분리되어 **처음 Map 탭을 열 때 lazy-create**됩니다. Map/SharpVectors 초기화 실패가 발생해도 Quest/Hideout/Items/Ammo는 계속 사용할 수 있습니다.

## 지도 화면

- Map dropdown
- multi-floor selector
- wheel zoom
- drag pan
- 보기 초기화
- 좌측: 선택한 실제 Map의 **현재 진행 중 Quest만**
- Map marker checkbox를 지도 화면에서 즉시 접근
- attribution 상시 표시

Marker 범위:

- PMC / Scav / shared extract
- transit
- PMC / Scav spawn
- sniper Scav
- boss / special AI
- hazard / artillery
- lock / switch
- stationary weapon
- BTR stop
- loot container / loose loot
- Quest
- custom user marker
- current player position
- optional trail

Loot container / loose loot는 기본 OFF이며 수천 개의 WPF Control 대신 `DrawingContext` 기반 bulk layer로 렌더링합니다. MiniMap도 동일한 전략을 사용합니다.

## Quest 위치

현재 task 데이터의 `possibleLocations` / `zones`를 canonical Quest objective world geometry로 변환합니다.

- 정확한 위치/zone 있음 → Quest marker/outline
- 정확한 위치 없음 → 좌측 Quest 목록에는 표시하되 `정확한 위치 없음`
- 가짜 좌표 생성 금지
- 좌측 Quest 선택 → 위치 focus
- marker 선택 → Quest/목표 표시
- `퀘스트 탭에서 보기` → Quest 탭 이동

## 사용자 marker

- 빈 지도 우클릭 → 추가
- 이름 변경
- 색상 변경
- 삭제
- floor ID 포함 저장
- `map-markers.json`
- Game Content/Map update와 독립

## Screenshot 현재 위치

EFT screenshot 파일명의 좌표/Quaternion을 사용하며 이미지 OCR은 사용하지 않습니다.

```text
PrintScreen
→ screenshot folder FileSystemWatcher
→ filename X/Y/Z + quaternion parse
→ world position + heading
→ Map coordinate transform
→ current player marker
```

- screenshot path 직접 선택 / 자동 찾기
- 위치에 따른 floor 자동 전환
- floor 판정은 Y height + metadata spatial bounds 사용

## Raid Map 자동 전환

EFT game log의 알려진 Map alias만 감지해 Map/MiniMap을 자동 전환합니다. unknown alias는 추측하지 않습니다.

## 이동 경로

- `이동 경로` checkbox
- 기본 OFF
- screenshot 위치 갱신 시 path 추가
- `경로 지우기`

## MiniMap

별도 always-on-top window.

- Map의 mini version
- player 위치 갱신 시 player-centered follow
- zoom 가능
- 현재 floor/map/marker/filter 상태 연동
- Map 탭 버튼으로 ON/OFF

---

## Map 데이터 원천

### gameplay / Quest geometry

- `json.tarkov.dev/<game-mode>/maps`
- task `possibleLocations`
- task `zones`

### layout metadata

Tarkov.dev 공개 `src/data/maps.json`:

- transform
- coordinate rotation
- bounds / svgBounds
- zoom range
- SVG layer
- floor height/spatial extents
- attribution

### artwork

`the-hideout/tarkov-dev-svg-maps`

- CC BY-NC-SA 4.0
- 비상업적 준현 헬퍼에서 사용하기로 사용자 확정
- attribution 표시
- cheat/radar/ESP 용도 금지 조건 준수

---

## Map asset update / recovery

Map gameplay facts와 Map presentation assets를 분리합니다.

```text
Game Content update
→ canonical Map gameplay/Quest geometry 갱신
→ Map layout/SVG candidate 갱신
→ 검증
→ active Map assets 교체
```

Map presentation asset은 `map-cache/active / candidate / previous` 구조입니다.

### self-heal

PR #48부터 Map 탭 진입 시 active Map asset을 검증합니다.

```text
active Map asset 정상
→ 즉시 사용

active Map asset 없음/손상
→ 현재 active Game Content로 Map asset만 자동 재다운로드
```

빈 지도 패널에는 `지도 자산 다시 받기` 버튼이 있습니다.

### SVG source fallback

```text
assets.tarkov.dev/maps/svg/<file>
↕
raw.githubusercontent.com/the-hideout/tarkov-dev-svg-maps/.../<file>
```

한 Map 실패가 다른 Map을 막지 않습니다. 새 asset 실패 시 이전 정상본이 있으면 해당 Map만 이전본을 유지하고, 이전본도 없으면 해당 Map만 일시 제외합니다.

Marker PNG 실패 시 이전 icon을 재사용하고 없으면 기본 marker visual을 사용합니다.

### Windows download lifecycle

PR #50부터:

```text
FileShare.None writer로 쓰기
→ Flush
→ writer/input dispose
→ validator 재오픈
→ SVG/XML 또는 PNG signature 검증
```

Windows의 파일 공유 잠금 규칙을 회귀 테스트로 보호합니다.

---

## Map readability

첫 실제 화면에서 확인된 문제:

1. Floor ComboBox가 `MapFloorDefinition { ... }` 전체 record를 표시함.
2. Customs 구조 geometry는 존재하지만 upstream SVG의 `land #1f5054`, `building #1a2632` 등 저대비 palette 때문에 전체 지도에서 건물이 지형에 묻힘.
3. upstream SVG는 기능형 schematic이며 완성형 커뮤니티 지도처럼 지명/랜드마크 텍스트가 촘촘한 자산은 아님.

현재 readability pass 방향:

- Floor selector는 `Name`만 표시
- 원본 SVG는 변경하지 않음
- 표시 시 `readable-v1` derivative 생성
- building/floor/cement/road/fence/map border 대비 강화
- geometry/viewBox는 그대로 유지하여 marker calibration 불변
- Map과 MiniMap 동일 presentation 사용
- readable derivative 생성은 UI thread 밖에서 수행
- 연속 Map/floor 전환 시 최신 요청만 화면에 적용
- 변환 실패 시 원본 SVG로 fallback

이번 고대비 보정 후에도 실제 레이드용 구조 파악이 부족하면 **더 상세하고 라이선스가 명확한 artwork source**를 별도로 조사합니다. 불명확한 커뮤니티 이미지는 임의로 패키징하지 않습니다.

상세: `docs/MAP_VISIBILITY_ANALYSIS.md`

---

## Map에서 최근 해결한 문제

### 앱 실행 무반응

- MapPage lazy-create
- guarded startup / dispatcher diagnostics
- self-contained folder package

### Map 탭 최초 진입 NRE

원인: XAML `IsChecked=True`가 `InitializeComponent` 도중 이벤트를 발생시켜 미생성 Canvas를 렌더링함.

해결: declarative `IsChecked` 제거, MapUserSettings를 초기 상태 authority로 사용, event re-entry 차단, XAML 회귀 테스트.

### Map asset 없음

valid v4 `content.db`가 있어도 missing `map-cache`를 자동 복구하도록 수정하고, 지도별 부분 복구를 적용했습니다.

### 모든 Map SVG 다운로드 실패

원인: exclusive writer가 살아 있는 상태에서 validator가 같은 파일을 다시 열어 Windows에서 거부됨.

해결: writer dispose 후 검증 + Windows FileShare.None 회귀 테스트.

---

## Scanner

탭과 `준비 중` placeholder만 있습니다. 실제 기능은 별도 제품 요구사항 확정 전까지 구현하지 않습니다.

향후 Scanner는 정상적인 화면 인식 보조와 실시간 radar/ESP 성격 기능을 구분해야 하며 Map artwork license의 anti-cheat 조건을 위반하지 않아야 합니다.

---

## 실사용 피드백 상태

- 1차: merged
- 2차: PR #36 merged
- 3차: PR #37 merged
- 4차: PR #39 merged
- 5차: PR #41 merged
- Map source/license: PR #43 merged
- Map implementation: PR #44 merged
- Map runtime recovery: PR #45–#50 merged
- Map readability: **구현 / Windows 검증 중**

---

## 현재 다음 작업

1. readability pass Windows build/test/publish 검증 완료
2. Windows 사용자 화면에서 floor Name 표시와 `readable-v1` 구조물 시인성 확인
3. 실제 SVG/marker coordinate alignment 확인
4. multi-floor 수동/자동 전환 확인
5. 실제 EFT screenshot filename 위치/방향 검증
6. EFT log raid Map auto-switch 검증
7. MiniMap follow/zoom 사용감 검증
8. 고대비 SVG로도 구조 파악이 부족하면 라이선스가 명확한 상세 artwork source 조사
9. 발견된 실사용 문제를 Map 2차 개선으로 반영
