# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다. 세부 설계/과거 원인은 링크된 상세 문서를 참조합니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP IMPLEMENTED / MAP READABILITY PASS MERGED / WINDOWS USER TESTING`

Map 1차 기능은 구현되어 Windows 실사용 검증 중입니다. 앱 시작, Map 초기화, 지도 자산 복구, Windows 다운로드 파일 잠금 문제는 해결됐습니다. 첫 실제 지도 화면에서 확인된 **층 선택 표시 버그와 낮은 SVG 시인성**은 PR #53에서 수정·검증·병합했습니다.

최근 Map checkpoint:

```text
PR #43 — Map 자동 업데이트 + SVG 라이선스 정책
PR #44 — Map 탭 / Quest marker / 위치 추적 / MiniMap
PR #45 — 대량 loot marker renderer 안정화
PR #46 — Map lazy-load / startup diagnostics / self-contained package
PR #47 — 초기 marker CheckBox NRE 수정
PR #48 — missing map-cache self-heal / 지도별 부분 복구 / SVG source fallback
PR #50 — Windows FileShare.None 다운로드 검증 실패 수정
PR #53 — floor Name 표시 + high-contrast readable SVG presentation
```

PR #53 검증:

```text
CI: 31299511934
Release Desktop build: success
full automated tests: success
Windows x64 self-contained publish: success
ZIP creation/upload: success
review threads: none
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
- Map gameplay facts와 artwork/layout은 분리하고 독립적으로 갱신/복구
- Map 좌표도 수동 patch DB가 아니라 온라인 source → canonical 변환 구조 사용

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

Content schema는 **v4**이며 dynamic Map marker + Quest objective world geometry를 포함합니다. 이전 snapshot은 온라인 source에서 자동 재구축하고 `user.db`는 유지합니다.

Windows 테스트 전달본은 **self-contained folder ZIP**입니다. ZIP 전체를 새 폴더에 풀고 `JunhyunHelper.exe`를 실행하며, 별도 .NET 설치는 필요 없습니다.

---

## 기존 Core 제품 상태

### Profile

- 한 GameMode당 profile 하나
- 새 프로필 / 프로필 수정 / 삭제
- level, faction, edition, prestige, trader 상태
- Prestige 기본 0
- Fence reputation 별도 주요 진행값

### Quest

- 진행 중 / 잠김 / 사용 불가 / 완료
- Quest Item → Item
- prerequisite Quest → Quest
- Wiki 이동
- Map Quest list/marker → Quest
- 고정 제출 요구는 완료 시 tracked Inventory 자동 차감
- 완료 취소는 실제 소비 ledger 기반 복원 선택
- 유동 제출 후보는 실제 사용 Item을 알 수 없어 자동 차감하지 않음

### Hideout

- 미입력 Lv.0
- 단계별 next-upgrade material
- material ↔ Item/facility 이동
- upgrade 고정 재료 자동 차감
- rollback은 ledger 기반 정확한 복원 선택

### Needed Items / Item

수량:

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

필터:

- 검색
- Item 종류
- 용도: `모든 용도 / 퀘스트용 / 은신처용`
- 필요 상태

### Ammo

- raw 성능: `json.tarkov.dev`
- Wiki Ballistics: 현재 비교 표 membership + Armor Class 1~6 effectiveness
- favorite는 selection 상태가 아니라 shortcut popup
- 같은 favorite를 언제든 다시 눌러 이동 가능

---

# Map

## 사용자 흐름

- Map dropdown
- floor selector
- wheel zoom / drag pan / 보기 초기화
- 좌측: 선택 Map의 **현재 진행 중 Quest만**
- Map 상단에서 marker checkbox 즉시 접근
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
- user marker
- current player position
- optional trail

Loot container / loose loot는 기본 OFF이며 수천 개 WPF Control 대신 `DrawingContext` 기반 bulk layer로 렌더링합니다.

## Quest 위치

Task의 `possibleLocations` / `zones`를 canonical world geometry로 변환합니다.

- 정확한 위치/zone 있음 → marker/outline
- 정확한 위치 없음 → 좌측 목록에는 표시, `정확한 위치 없음`
- 가짜 좌표 생성 금지
- Quest list ↔ marker ↔ Quest 탭 연결

## 사용자 marker

- 빈 지도 우클릭 추가
- 이름/색 변경
- 삭제
- floor ID 포함 저장
- Game Content/Map update와 독립

## Screenshot 현재 위치

EFT screenshot 파일명의 X/Y/Z + quaternion을 파싱합니다. 이미지 OCR은 사용하지 않습니다.

```text
PrintScreen
→ FileSystemWatcher
→ filename 좌표/방향 parse
→ world position
→ Map transform
→ player marker + heading
```

- screenshot path 직접 선택 / 자동 찾기
- 위치 기반 floor 자동 전환
- floor 판정은 height + metadata spatial bounds 사용

## Raid Map 자동 전환

EFT game log의 알려진 Map alias만 사용해 Map/MiniMap을 자동 전환합니다. unknown alias는 추측하지 않습니다.

## 이동 경로

- `이동 경로` checkbox
- 기본 OFF
- screenshot 위치 갱신 시 path 추가
- `경로 지우기`

## MiniMap

별도 always-on-top window입니다.

- 전체 Map의 mini version
- player-centered follow
- zoom
- 현재 floor/map/marker/filter 연동
- Map 탭 버튼으로 ON/OFF

---

## Map 데이터 원천

### gameplay / Quest geometry

- `json.tarkov.dev/<game-mode>/maps`
- task `possibleLocations`
- task `zones`

### layout metadata

Tarkov.dev 공개 `src/data/maps.json`:

- transform / coordinate rotation
- bounds / svgBounds
- zoom
- SVG layer
- floor height/spatial extents
- attribution

### artwork

`the-hideout/tarkov-dev-svg-maps`

- CC BY-NC-SA 4.0
- 준현 헬퍼는 비상업적 사용
- attribution 표시
- radar/ESP/cheat 용도 금지 조건 준수

---

## Map asset update / recovery

```text
Game Content update
→ canonical gameplay/Quest geometry 갱신
→ layout/SVG candidate 갱신
→ 검증
→ active Map assets 교체
```

Map asset은 `active / candidate / previous` 구조입니다.

Map 탭 진입 시 active asset이 없거나 손상되면 현재 Game Content를 이용해 **Map asset만 self-heal**합니다. 빈 지도 패널에서 `지도 자산 다시 받기`도 가능합니다.

SVG source fallback:

```text
assets.tarkov.dev/maps/svg/<file>
↕
raw.githubusercontent.com/the-hideout/tarkov-dev-svg-maps/.../<file>
```

한 Map 실패가 전체를 막지 않으며 이전 정상본이 있으면 해당 Map만 이전본을 유지합니다. Marker PNG는 실패해도 이전 icon 또는 기본 visual로 fallback합니다.

Windows 다운로드는 반드시 writer dispose 후 validator가 파일을 재오픈하며, `FileShare.None` 회귀 테스트로 보호합니다.

---

## Map readability — PR #53

첫 실제 화면에서 확인된 사실:

- floor selector가 record 전체를 출력하던 UI bug가 있었음
- Customs 구조 geometry는 SVG에 실제 존재함
- upstream SVG가 `land #1f5054`, `building #1a2632` 등 저대비 palette라 전체 축척에서 구조물이 묻힘
- upstream SVG는 기능형 schematic이며 촘촘한 지명/랜드마크 텍스트가 있는 완성형 커뮤니티 지도와 성격이 다름

적용된 수정:

- floor selector는 **`Name`만 표시**
- downloaded/source SVG는 그대로 보존
- 화면 표시 때 `readable-v1` derivative 생성
- building / floor / cement / tarmac / road / fence / map border 대비 강화
- geometry/viewBox 불변 → marker calibration 불변
- Map과 MiniMap 동일 readable presentation
- derivative 생성은 WPF UI thread 밖에서 수행
- 빠른 Map/floor 전환 시 최신 요청만 적용
- 변환 실패 시 원본 SVG fallback

이번 사용자 검증에서도 구조/랜드마크 파악이 부족하다고 판단되면 다음 단계에서 **더 상세하면서 redistribution/derivative 라이선스가 명확하고 좌표 calibration 가능한 artwork source**를 조사합니다. 라이선스가 불명확한 커뮤니티 이미지는 임의로 패키징하지 않습니다.

상세: `docs/MAP_VISIBILITY_ANALYSIS.md`

---

## Scanner

탭과 placeholder만 있습니다. 실제 기능은 별도 제품 요구사항 확정 전까지 구현하지 않습니다. 향후 Map artwork anti-cheat 라이선스와 충돌하는 radar/ESP 성격 기능은 넣지 않습니다.

---

## 현재 다음 작업

1. Windows 사용자 화면에서 PR #53의 floor `Name` 표시 확인
2. `readable-v1`로 Customs 및 다른 Map 구조물 시인성 확인
3. 실제 marker coordinate alignment 확인
4. multi-floor 수동/자동 전환 확인
5. 실제 EFT screenshot 위치/방향 검증
6. game log Map auto-switch 검증
7. MiniMap follow/zoom 사용감 검증
8. readable-v1로도 부족하면 상세 artwork source 라이선스/정합성 조사
9. 발견된 실사용 문제를 Map 2차 개선으로 반영
