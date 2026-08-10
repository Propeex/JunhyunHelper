# Map / MiniMap hotkey follow-up — 2026-08-10

상태: `PR #69 MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## 사용자 실사용 피드백

PR #68 Windows 검증 결과 나머지 항목은 정상 동작했고 다음 세 항목만 후속 수정 대상으로 확인됐다.

1. 확대/축소 단축키가 Main Map에서는 동작하지만 MiniMap에는 적용되지 않았다.
2. 층 드롭다운 수동 전환은 정상이나 위층/아래층 단축키 전환이 동작하지 않았다.
3. MiniMap 우측 하단의 legacy mouse-resize 표시/그립은 제품에 불필요하며 제거한다.

추가 확인 사항:

- Map 단축키는 JunhyunHelper 창 활성 상태에 한정하지 않는다.
- `EscapeFromTarkov` 또는 `EscapeFromTarkov_BE`가 foreground인 실제 게임 플레이 중에도 전역 단축키가 동작해야 한다.

## 구현

### 공유 zoom / floor hotkey

`JunhyunMapHotkeyService`의 실행 경로를 다음처럼 수정했다.

```text
Zoom In / Out
→ Main Map JunhyunZoomIn/Out
→ active MiniMap ZoomIn/Out

Floor Up / Down
→ Main Map original CmbFloorSelect selection route
→ active MiniMap MoveFloorUp/Down
```

MiniMap은 transplanted subsystem이 이미 제공하는 `ZoomIn`, `ZoomOut`, `MoveFloorUp`, `MoveFloorDown` API를 그대로 사용한다.

### persisted hotkey runtime authority

키 입력 실행 시 legacy overlay settings만 조회하지 않고 `%LocalAppData%/JunhyunHelper/map-product-settings.json`의 JunhyunHelper-owned hotkey를 직접 우선 조회한다.

따라서 UI에 저장된 키와 runtime dispatcher가 late legacy initialization 때문에 달라지는 경로를 제거했다.

### Main Map floor route

Main Map floor hotkey는 별도 floor 구현을 만들지 않고 실제 정상 동작이 확인된 원본 `CmbFloorSelect.SelectedIndex`를 변경한다.

불필요한 `Visibility == Visible` gate를 제거해 WPF visibility 상태 때문에 hotkey가 무시되는 경로를 없앴다. `SelectionChanged`는 마우스로 드롭다운을 선택할 때와 동일한 제품 pipeline을 실행한다.

### 게임 foreground

전역 keyboard hook은 다음 foreground process를 허용한다.

- `EscapeFromTarkov`
- `EscapeFromTarkov_BE`
- `JunhyunHelper`
- `TarkovHelper`

따라서 게임 플레이 중 사용이 제품 기준이다.

### MiniMap mouse resize 제거

JunhyunHelper MiniMap은 hotkey 조작 정책으로 고정했다.

- `ResizeMode = NoResize`
- transplanted XAML의 우측 하단 resize Path 제거
- MiniMap 크기 조절은 기존 size increase/decrease hotkey만 사용

## PR / 검증

```text
PR: #69 Fix MiniMap zoom, floor hotkeys, and resize grip
merge: 24a9bcb5c89ce30067b84427b7df7ec755aaa9de
final head: 0753febeab62d1a41921c285d7e0ed2a4df0ab94
CI: 31350388320
artifact: 9048751983
artifact digest: sha256:20521163d31bf58c8dbf25b12fe5a93f1195df6182aa7c003c40df3519e4d99b
```

최종 CI:

- Desktop Release build: success
- automated tests: success
- Windows x64 self-contained publish: success
- Startup + exact Map subsystem smoke: success
- graceful Main Window close + process exit: success
- ZIP creation/upload: success

## Windows 사용자 검증

- 게임이 foreground인 상태에서 Map/MiniMap zoom in/out hotkey
- 게임이 foreground인 상태에서 floor up/down hotkey
- Main Map과 MiniMap이 같은 floor 방향으로 변경되는지
- MiniMap 우측 하단 resize grip이 사라졌는지
- MiniMap 가장자리를 마우스로 드래그해도 resize되지 않는지
- MiniMap size increase/decrease hotkey는 계속 정상 동작하는지
