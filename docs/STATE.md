# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP IMPLEMENTED / WINDOWS USER TESTING / MAP ASSET SELF-HEAL MERGED`

Map 1차 기능은 구현되어 있으며 Windows 실사용 검증 중입니다. 최초 Map 빌드에서 발견된 시작/초기화/자산 복구 문제는 각각 후속 PR로 수정했습니다.

최근 Map 관련 병합:

```text
PR #43 — Map 자동 업데이트 + SVG 라이선스 정책 확정
PR #44 — Map 탭 / Quest marker / 위치 추적 / MiniMap 구현
PR #45 — 대량 loot marker renderer build/persistence 복구
PR #46 — 앱 시작 복구, Map lazy-load, startup diagnostics, self-contained folder package
PR #47 — Map marker CheckBox 초기화 NRE 수정
PR #48 — missing map-cache 자동 복구 + 지도별 부분 복구 + SVG source fallback
```

PR #48 검증:

```text
CI: 31296494454
Release Desktop build: success
full automated tests: success
Windows x64 self-contained publish: success
ZIP creation/upload: success
review threads: none
```

상세 문서:

- `docs/MAP_PRODUCT_DESIGN.md`
- `docs/MAP_IMPLEMENTATION.md`
- `docs/MAP_PERFORMANCE.md`
- `docs/MAP_STARTUP_RECOVERY.md`
- `docs/MAP_UI_INIT_FIX.md`
- `docs/MAP_ASSET_RECOVERY.md`
- `docs/MAP_DATA_SOURCE_ANALYSIS.md`

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
- 유동 제출처럼 프로그램이 실제 사용 사실을 알 수 없는 경우 임의 추정하지 않음
- Map도 수동 좌표 DB가 아니라 온라인 source → canonical 변환 구조를 사용
- Map gameplay data와 Map artwork/layout은 분리하고 각각 안전하게 갱신/복구

---

## 기술 / 배포

- .NET 10 / C# / WPF
- SQLite
- SkiaSharp image decode + PNG normalize
- SharpVectors WPF SVG rendering
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

Map에서 사용하는 WPF/SVG runtime dependency 안정성을 위해 현재 Windows 테스트 전달본은 **self-contained folder ZIP**입니다.

- ZIP 전체를 새 폴더에 압축 해제
- 폴더 안 `JunhyunHelper.exe` 실행
- EXE만 따로 복사해서 실행하지 않음
- 별도 .NET 설치 불필요

CI는 publish 결과에 최소 다음 파일이 존재하는지 확인합니다.

```text
JunhyunHelper.exe
JunhyunHelper.dll
SharpVectors.Converters.Wpf.dll
SharpVectors.Rendering.Wpf.dll
```

앱 시작/dispatcher 예외는 `%LocalAppData%/JunhyunHelper/logs/startup.log`에 기록합니다.

---

## Content schema

현재 **v4**.

- v2: Item category metadata
- v3: Wiki Ballistics membership와 effectiveness 분리
- v4: dynamic Map marker + Quest objective world geometry

이전 Game Content snapshot은 온라인 source에서 자동 재구축합니다. `user.db`는 유지합니다.

---

## Profile

- 한 GameMode당 profile 하나
- Profile dropdown 안 `새 프로필`
- `프로필 수정` 안 삭제
- Player level: `- / 값 / +`
- Prestige: 기본 0, 미입력 없음
- Fence reputation: 상단 주요 진행값
- 핵심 Trader: 게임식 순서
- 기타 Trader: `특별` Expander, 기본 접힘

---

## Quest

사용자 상태:

- 진행 중
- 잠김
- 사용 불가
- 완료

상세 연결:

- Quest Item → Item
- prerequisite Quest → Quest
- `위키`
- Map Quest list/marker → Quest

고정 제출 요구는 Quest 완료와 함께 tracked Inventory에서 자동 차감합니다.

```text
인레이드 필수 → 인레이드만
일반 요구 → 일반 우선, 부족하면 인레이드
```

유동 제출 후보는 실제 어떤 Item을 사용했는지 알 수 없으므로 자동 차감하지 않습니다.

완료 취소 시 실제 자동 소비 ledger가 있으면 복원 여부를 묻습니다.

Quest Map filter:

- Ground Zero / Ground Zero 21+ → `Ground Zero`
- Factory day/night → `Factory`
- canonical Map ID는 보존

---

## Hideout

- 미입력 = Lv.0
- `- / 현재 level / +`
- 다음 upgrade material card/list
- material click → Item
- Item Hideout source click → facility
- upgrade 시 고정 재료 자동 차감
- rollback 시 ledger 기반 정확한 복원 선택

---

## Needed Items / Item

목록:

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

필터:

- 검색
- Item 종류
- 용도: `모든 용도 / 퀘스트용 / 은신처용`
- 필요 상태: `필요 / 전체 / 정리 필요 / 충분 / 판단 보류`

Quest와 Hideout 모두에 필요한 Item은 양쪽 용도 필터에 표시합니다.

유동 제출:

- 별도 view
- Quest별 group
- full-width / left aligned
- 후보 Item → Item
- Quest → Quest
- cleanup 보수적 보호

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

`IMPLEMENTED / WINDOWS USER TESTING`

지도 탭은 앱 시작과 분리되어 있으며 **처음 지도 탭을 열 때 lazy-create**됩니다. Map/SharpVectors 초기화 실패가 발생해도 Quest/Hideout/Items/Ammo는 계속 사용할 수 있습니다.

## 지도 화면

- Map dropdown
- multi-floor selector
- wheel zoom
- drag pan
- 보기 초기화
- 좌측: 선택한 실제 Map의 **현재 진행 중 Quest만**
- Map marker checkboxes는 지도 화면 상단에서 즉시 접근 가능
- attribution 상시 표시

Marker 범위:

- PMC extract
- Scav extract
- shared extract
- transit
- PMC spawn
- Scav spawn
- sniper Scav
- boss
- special AI
- hazard/artillery
- lock
- switch
- stationary weapon
- BTR stop
- loot container
- loose loot
- Quest
- custom user marker
- current player position
- optional trail

Loot container / loose loot는 기본 OFF이며, 켜도 수천 개의 WPF Control을 만들지 않고 `DrawingContext` 기반 bulk layer로 렌더링합니다. MiniMap도 같은 대량 marker 전략을 사용합니다.

## Quest 위치

현재 task 데이터의 `possibleLocations` / `zones`를 canonical Quest objective world geometry로 변환합니다.

- 정확한 위치/zone 있음 → Quest marker/outline 표시
- 정확한 위치 없음 → 좌측 Quest 목록에는 표시하되 `정확한 위치 없음`
- 가짜 좌표 생성 금지
- 좌측 Quest 선택 → 해당 위치 focus
- marker 선택 → Quest와 목표 표시
- `퀘스트 탭에서 보기` → Quest 탭 이동

## 사용자 marker

- 빈 지도 우클릭 → 추가
- 이름 변경
- 색상 변경
- 삭제
- floor ID 포함 저장
- `map-markers.json`
- Game Content/Map update와 독립, 업데이트로 삭제되지 않음

## Screenshot 현재 위치

EFT screenshot 파일명의 좌표/Quaternion을 사용합니다. 이미지 픽셀 OCR은 사용하지 않습니다.

```text
PrintScreen
→ screenshot folder FileSystemWatcher
→ filename X/Y/Z + quaternion parse
→ world position + heading
→ Map coordinate transform
→ current player marker
```

- screenshot path 직접 선택
- 자동 찾기
- 위치에 따른 floor 자동 전환
- floor 판정은 Y 높이뿐 아니라 metadata가 제공하는 X/Z spatial bounds도 사용

## Raid Map 자동 전환

EFT game log의 알려진 Map alias를 감지해 Map/MiniMap을 자동 전환합니다.

- known alias만 사용
- unknown alias를 추측하지 않음

## 이동 경로

- `이동 경로` checkbox
- 기본 OFF
- screenshot 위치가 갱신될 때 path 추가
- `경로 지우기`

## MiniMap

별도 always-on-top window.

- Map의 mini version
- player 위치 갱신 시 player-centered follow
- zoom 가능
- 현재 floor/map/marker/filter 상태와 연동
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

PR #48부터 지도 탭 진입 시 active Map asset을 검증합니다.

```text
active Map asset 정상
→ 즉시 사용

active Map asset 없음/손상
→ 현재 active Game Content만 이용해 Map asset 자동 재다운로드
→ 전체 Game Content를 다시 받을 필요 없음
```

빈 지도 패널에는 `지도 자산 다시 받기` 버튼이 있습니다.

### SVG source fallback

동일 공개 artwork에 대해 다음 source를 fallback으로 사용합니다.

```text
assets.tarkov.dev/maps/svg/<file>
↕
raw.githubusercontent.com/the-hideout/tarkov-dev-svg-maps/.../<file>
```

### 부분 실패

한 Map 실패가 전체 Map candidate를 폐기하지 않습니다.

- 새 Map SVG 성공 → 새 버전
- 실패 + 이전 정상 Map 있음 → 그 Map만 이전 정상본 유지
- 실패 + 이전본 없음 → 그 Map만 일시 제외
- 하나 이상 정상 Map 확보 → 정상 Map들 활성화
- 모든 Map 실패 → 기존 active 있으면 보존, 없으면 명시적 오류 + 재시도 UI

Marker PNG도 실패 시 이전 icon을 재사용하고, 이전 icon도 없으면 기본 marker visual을 사용합니다.

---

## Map에서 최근 해결한 문제

### 앱 실행 무반응

원인 후보였던 Map/SharpVectors를 앱 startup에서 분리했습니다.

- MapPage lazy-create
- guarded `App.OnStartup`
- startup/unhandled exception logging
- self-contained folder package

### Map 탭 최초 진입 NRE

원인:

```text
XAML IsChecked=True
→ InitializeComponent 도중 Checked 발생
→ MarkerToggle_Changed
→ RenderCurrentMap
→ 뒤쪽 Canvas 미생성
→ NullReferenceException
```

해결:

- XAML marker CheckBox의 declarative `IsChecked` 제거
- MapUserSettings를 초기 checkbox state의 authority로 사용
- 초기 설정 적용 중 event re-entry 차단
- XAML 회귀 테스트 추가

### Map asset 없음

원인:

- valid v4 `content.db`가 있으면 missing map-cache만으로 content update가 재실행되지 않음
- 한 SVG 실패가 Map candidate 전체를 폐기할 수 있었음

해결: 위 `Map asset update / recovery`의 self-heal/partial recovery 적용.

---

## Scanner

탭과 `준비 중` placeholder만 있습니다. 실제 기능은 별도 제품 요구사항 확정 전까지 구현하지 않습니다.

향후 Scanner는 정상적인 화면 인식 보조와 실시간 radar/ESP 성격 기능을 명확히 구분해야 하며, Map artwork license의 anti-cheat 조건을 위반하지 않아야 합니다.

---

## 실사용 피드백 상태

- 첫 실사용 피드백: merged
- 2차: PR #36 merged
- 3차: PR #37 merged
- 4차: PR #39 merged
- 5차: PR #41 merged
- Map source/license: PR #43 merged
- Map implementation: PR #44 merged
- Map build recovery: PR #45 merged
- Map startup recovery: PR #46 merged
- Map initial CheckBox crash: PR #47 merged
- Map missing asset self-heal: **PR #48 merged / Windows user verification next**

---

## 현재 다음 작업

1. PR #48 Windows 사용자 환경에서 `map-cache` 자동 복구 확인
2. 실제 SVG 표시 / coordinate alignment 확인
3. marker 위치·아이콘·filter 사용감 확인
4. multi-floor 자동/수동 전환 확인
5. 실제 EFT screenshot filename 위치/방향 검증
6. EFT log raid Map auto-switch 검증
7. MiniMap follow/zoom ergonomics 검증
8. 발견된 실사용 문제를 Map 2차 개선으로 반영
