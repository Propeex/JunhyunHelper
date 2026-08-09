# LEGACY MAP PORT — 기존 Tarkov Helper 지도/미니맵 이식

기록일: **2026-08-09**

상태: `CONFIRMED / LEGACY SYSTEM ANALYSIS IN PROGRESS`

## 사용자 확정 결정

현재 JunhyunHelper의 Map presentation 방향은 중단하고, **기존 `Propeex/Tarkov-Helper`의 지도 기능을 기준으로 이식**합니다.

범위에는 일반 지도뿐 아니라 **MiniMap 전체 기능도 포함**합니다.

사용자가 명시한 핵심 조건은 단순 파일 복사가 아니라, 가져오는 과정에서 기존 구현을 완전히 이해하고 필요한 수정을 거쳐 이식하는 것입니다.

## 이식 범위

기존 구현의 다음 요소를 하나의 Map 시스템으로 분석합니다.

- 정적 SVG 지도 자산
- `map_configs.json`의 지도별 크기 / aliases / 좌표 transform / floor 정의
- screenshot filename X/Y/Z + 방향 파싱
- game coordinate → map surface 변환
- player marker / trail
- map 선택 / wheel zoom / pan / reset
- floor 선택 / 자동 floor detection / SVG floor visibility
- extract / 일반 marker / Quest marker / custom marker
- game log 기반 Map 자동 전환
- 일반 Map과 MiniMap의 Map/floor/자동 추적 상태 공유
- always-on-top MiniMap
- MiniMap player-follow / fixed view
- MiniMap zoom / pan / opacity / click-through
- MiniMap floor 이동 / 자동 floor 복귀
- MiniMap configurable hotkeys
- MiniMap window size/position/settings persistence
- Map 탭을 벗어났을 때 MiniMap runtime 유지
- 앱 종료/영구 페이지 교체 시 구독과 native window 정리

## 반드시 이해하고 수정해야 하는 기존 문제

legacy 코드를 그대로 복사하지 않습니다. 특히 과거 PR에서 확인된 다음 회귀 원인을 다시 만들지 않습니다.

- Map 탭 재진입 시 반복 초기화와 동기 Dispatcher 대기로 UI가 멈추던 문제
- MiniMap 설정 초기화 이벤트가 click-through native state를 반대로 뒤집던 문제
- 일반 탭 전환의 `Unloaded`가 MiniMap까지 종료하던 문제
- MiniMap 탭 유지용 전역 `RadioButton.Checked` handler가 `InitializeComponent()` 중 실행되어 앱이 시작 직후 종료되던 문제
- floor 미감지 상태와 기본층 fallback 의미가 서로 달랐던 시기별 동작 차이
- 일반 Map과 MiniMap의 floor/marker 상태가 분리되어 어긋나던 문제
- 닫힌 WPF MiniMap Window 인스턴스를 다시 `Show()`하려던 lifecycle 문제
- async settings save 순서가 사용자 조작 순서를 역전할 수 있던 문제

## JunhyunHelper에서 유지할 경계

legacy UX를 기준으로 이식하되 새 제품의 저장/서비스 경계는 유지합니다.

- Quest / Hideout / Item / Ammo Game Content update는 기존 JunhyunHelper pipeline을 유지합니다.
- `user.db`와 Map/MiniMap 사용자 설정은 Game Content와 분리합니다.
- Map 자산과 좌표 보정 데이터는 사용자가 명시적으로 legacy 사용을 승인한 별도 presentation/runtime 자산으로 취급합니다.
- 오래된 legacy DB row나 하드코딩된 Quest/marker 데이터를 현재 Game Content의 권위 사실로 승격하지 않습니다.
- 현재 online Map artwork provider 실험(RE3MR/Wiki/Tarkov.dev presentation)은 legacy Map 이식 완료 후 기본 표시 경로에서 제거합니다.

## 1차 확인된 legacy 구조

### 지도 자산 / config

legacy package는 다음을 배포합니다.

```text
Assets/DB/Maps/*.svg
Assets/DB/Data/*.json
Assets/DB/Icons/Markers/*.svg
```

`map_configs.json`은 map별로 다음을 정의합니다.

```text
key
svgFileName
imageWidth / imageHeight
aliases
playerMarkerTransform (2D affine 2x3)
svgBounds
floors[]
```

현재 확인한 Map:

- Woods
- Customs
- Shoreline
- Interchange
- Reserve
- Lighthouse
- Streets of Tarkov
- Factory
- Ground Zero
- The Lab
- The Labyrinth
- Terminal

### 위치 추적

legacy `MapTrackerService`는:

```text
screenshot FileSystemWatcher
→ ScreenshotCoordinateParser
→ EftPosition
→ MapCoordinateTransformer
→ ScreenPosition
→ player marker / trail / floor detection / MiniMap
```

으로 연결됩니다.

플레이어 위치는 map별 `playerMarkerTransform`이 있으면 이를 최우선으로 사용합니다.

### MiniMap

legacy MiniMap은 단순 축소판이 아니라 독립 runtime입니다.

```text
OverlayMiniMapService
→ OverlayMiniMapWindow
→ MapTrackerService 공유
→ map/floor/marker/player state 공유
→ GlobalKeyboardHookService
```

window는 borderless / transparent / Topmost이며 위치/크기/opacity/zoom/click-through/view mode/hotkey를 저장합니다.

## 구현 원칙

이식은 다음 순서로 진행합니다.

1. legacy dependency / asset inventory 완성
2. 좌표계와 floor 처리 수학 검증
3. MiniMap lifecycle / hotkey / click-through 흐름 검증
4. JunhyunHelper 현재 Map 시스템에서 재사용 가능한 user progress / Quest navigation 경계 분리
5. legacy asset/config을 새 저장소에 명시적으로 편입
6. Main Map을 legacy behavior 기준으로 교체
7. MiniMap을 legacy behavior 기준으로 교체
8. Map ↔ MiniMap shared state 복원
9. 과거 회귀를 automated test로 고정
10. Windows x64 실제 화면에서 좌표/층/MiniMap 검증

## 완료 기준

사용자 관점에서 기존 Tarkov Helper의 지도 사용 경험을 대체해야 합니다.

최소 검증:

- 각 legacy SVG가 동일한 map/floor 구조로 표시
- wheel zoom / pan / reset
- screenshot player marker 정합
- direction marker 정합
- floor 수동/자동 전환
- Map/MiniMap floor 동기화
- MiniMap player-follow / fixed view
- MiniMap click-through
- MiniMap size/position/opacity/zoom 저장
- MiniMap hotkeys
- Map 탭 밖에서도 MiniMap 유지
- 앱 종료 시 hook/window/service 정리
- 기존 Quest/Hideout/Item/Ammo와 user progress 회귀 없음

이 문서는 분석이 진행되는 동안 실제 dependency map과 이식 결과를 계속 갱신합니다.
