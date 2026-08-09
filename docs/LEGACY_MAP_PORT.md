# LEGACY MAP PORT — 기존 Tarkov Helper 지도/미니맵 이식

기록일: **2026-08-09**

상태: `IMPLEMENTED / AUTOMATED VERIFICATION PASSED / WINDOWS VISUAL VALIDATION PENDING`

## 사용자 확정 결정

현재 JunhyunHelper의 RE3MR / Wiki / Shebuka 중심 Map presentation 실험은 기본 표시 경로에서 중단합니다.

**기존 `Propeex/Tarkov-Helper`의 지도 artwork와 그 artwork에 맞는 좌표 보정 방식을 기준으로 이식**합니다. 범위에는 일반 지도뿐 아니라 **MiniMap**도 포함합니다.

단순 파일 복사가 아니라 legacy 시스템의 자산, 좌표식, floor 처리, 위치 추적, MiniMap runtime과 과거 회귀 원인을 분석한 뒤 JunhyunHelper의 데이터/저장 경계에 맞게 다시 구현합니다.

---

## 1. 이해한 legacy 시스템

### 지도 자산/config

legacy package의 핵심 자산:

```text
Assets/DB/Maps/*.svg
Assets/DB/Data/map_configs.json
Assets/DB/Icons/Markers/*.svg
```

`map_configs.json`의 map별 핵심 의미:

```text
key / aliases
svgFileName
imageWidth / imageHeight
playerMarkerTransform = [a,b,c,d,tx,ty]
floors[] = SVG layer ID / display name / order / default
```

플레이어와 API world coordinate는 legacy SVG에 맞는 2D affine 식으로 변환합니다.

```text
surfaceX = a * worldX + b * worldZ + tx
surfaceY = c * worldX + d * worldZ + ty
```

JunhyunHelper에서는 surface 크기가 다르더라도 legacy SVG 원본 width/height에 대한 비율로 정규화해서 같은 위치를 유지합니다. 역변환도 구현하여 custom marker 생성에 사용합니다.

### 위치 추적

```text
EFT PrintScreen
→ FileSystemWatcher
→ screenshot filename X/Y/Z + quaternion
→ world position + heading
→ legacy affine map transform
→ player marker / trail / floor detection / MiniMap
```

OCR, memory read, packet/radar 방식은 사용하지 않습니다.

### Floor

- 화면에 보이는 floor layer ID는 legacy SVG 기준을 사용합니다.
- 실제 자동 floor 판정 범위는 현재 Tarkov.dev online metadata의 X/Z bounds + Y height를 활용합니다.
- legacy layer 이름과 최신 online floor를 이름/위치 의미로 매칭하고, 정확한 online extent가 없는 legacy floor는 수동 선택만 허용합니다.

### MiniMap

legacy MiniMap의 핵심 UX를 새 runtime에 이식했습니다.

- borderless / transparent / Topmost
- 위치/크기/투명도/zoom 저장
- player tracking / fixed view
- player follow center
- middle-button pan
- mouse wheel zoom
- marker/player inverse scaling
- click-through
- `Ctrl+Shift+M` click-through 탈출
- NumPad +/- zoom hotkey
- PageUp/PageDown floor hotkey
- Map과 동일 floor/artwork/marker/player state 사용
- Map 탭을 벗어나도 window 유지

---

## 2. JunhyunHelper에서 유지한 경계

legacy에서 가져오는 것은 **Map presentation/calibration system**입니다.

다음은 JunhyunHelper의 현재 canonical 시스템을 그대로 유지합니다.

- Quest / Hideout / Item / Ammo online update
- current Quest progress
- dynamic Map gameplay marker facts
- Quest `possibleLocations` / `zones`
- user marker
- Game Content와 User Progress 분리
- `active / candidate / previous` update safety

오래된 legacy Quest/marker DB row를 현재 게임 사실로 사용하지 않습니다.

즉:

```text
현재 온라인 gameplay/Quest facts
+ 현재 온라인 floor spatial metadata
+ legacy Tarkov-Helper artwork/calibration pair
→ JunhyunHelper Map / MiniMap
```

---

## 3. 지도 업데이트 대응

사용자가 가장 중요하게 지정한 요구사항입니다.

legacy SVG와 transform을 서로 독립적으로 최신화하지 않습니다.

```text
GitHub Propeex/Tarkov-Helper main
→ 현재 commit SHA 확정
→ 동일 SHA의 map_configs.json 다운로드
→ config schema/값 검증
→ 동일 SHA의 SVG URL 구성
→ current Tarkov.dev floor extents와 결합
→ candidate download/validation
→ active 교체
```

따라서 upstream이 바뀌어도 **지도 그림과 좌표 보정식은 항상 동일 repository revision의 한 쌍**입니다.

검증 내용:

- commit SHA 형식
- map config root/maps
- map key / alias
- safe `.svg` filename
- image width/height
- 6-element finite affine transform
- floor layer metadata
- SVG XML validation
- 전체 candidate directory validation

최신 legacy bundle을 받거나 검증할 수 없으면 고정된 마지막 known-good legacy bundle로 fallback합니다. 이미 active인 정상 지도는 update 실패로 삭제하지 않습니다.

Map refresh 조건:

- Map active asset 없음/손상
- Game Content Map/marker fingerprint 변경
- 사용자의 `데이터 업데이트` 성공
- Map ingestion pipeline version 변경
- 마지막 성공 확인 후 24시간 경과
- 수동 Map asset refresh

현재 pipeline version:

```text
legacy-tarkov-helper-map-minimap-v2-atomic-upstream
```

---

## 4. 현재 구현 범위

### Main Map

- legacy Tarkov Helper SVG artwork
- legacy affine coordinate transform
- legacy artwork aspect ratio
- floor selector + legacy SVG layer visibility
- wheel zoom / drag pan / reset
- current Quest list
- current online Map markers
- Quest markers/zones
- custom marker
- screenshot current position + heading
- trail
- raid-log Map auto switch
- MiniMap toggle

### MiniMap

- same legacy artwork and floor as Main Map
- same online/current markers and Quest marker state
- player position / heading / trail
- player tracking / fixed view
- zoom / pan / reset
- opacity
- click-through
- floor up/down and detected-floor restore
- window position/size/settings persistence
- Map tab 전환과 독립된 visible runtime

---

## 5. 과거 legacy 회귀에서 반영한 방어

legacy 코드를 그대로 복사하지 않았습니다.

특히 다음 과거 문제를 다시 만들지 않는 구조로 정리했습니다.

- Map 재진입 때 동기 Dispatcher 대기로 UI freeze
- MiniMap click-through 설정과 native style 상태 역전
- 일반 탭 `Unloaded`가 MiniMap/추적을 같이 종료
- 전역 RadioButton handler가 `InitializeComponent()` 중 실행되어 startup crash
- 닫힌 WPF Window를 다시 `Show()`
- 비동기 설정 저장 순서 역전
- Map/MiniMap floor 상태 분리
- 새 artwork만 적용되고 좌표 보정은 이전 revision인 혼합 상태

---

## 6. 자동 검증

현재 PR #61 계열 검증에서 다음이 통과했습니다.

- Desktop Release build
- full automated tests
- legacy affine world ↔ surface round trip
- legacy artwork aspect ratio
- current online spatial floor extents + legacy floor layer merge
- 최신 GitHub commit/config 기반 atomic legacy bundle 적용
- 최신 bundle 실패 시 pinned known-good fallback
- Windows x64 self-contained publish
- ZIP creation/upload

---

## 7. Windows 실사용 최종 검증

자동 테스트만으로 지도는 완료 처리하지 않습니다.

다음 사용자 화면 검증이 남아 있습니다.

1. Ground Zero를 포함한 legacy SVG가 기존 Tarkov Helper와 같은 시각 구조로 보이는지
2. 각 Map의 width/aspect/crop이 정상인지
3. extract / Quest / current-position marker가 artwork에 맞는지
4. multi-floor 수동 전환과 screenshot 자동 floor 판정
5. MiniMap player tracking / fixed view / zoom / pan
6. MiniMap click-through와 `Ctrl+Shift+M` 복귀
7. MiniMap이 Map 탭 밖에서도 유지되는지
8. 데이터 업데이트 후 새 legacy source revision 확인/실패 fallback이 사용자 progress에 영향을 주지 않는지

이 검증 결과를 기준으로 2차 수정합니다.
