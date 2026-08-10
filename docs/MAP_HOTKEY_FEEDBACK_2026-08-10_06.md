# Map / MiniMap hotkey 실사용 재수정 — 2026-08-10

상태: `PR #70 MERGED / ENHANCED MINIMAP SMOKE PASSED / WINDOWS USER VALIDATION NEXT`

## 사용자 실사용 결과

PR #69 빌드에서 다음 문제가 계속 확인됐다.

1. Main Map floor hotkey는 MiniMap OFF 상태에서는 동작하지만 MiniMap을 켜면 망가짐.
2. zoom hotkey는 Main Map만 변경되고 MiniMap은 변경되지 않음.
3. MiniMap 우측 하단 legacy resize grip이 계속 보임.
4. 전달한 GitHub Actions artifact ZIP 안에 실제 배포 ZIP이 한 번 더 들어있는 중첩 ZIP 형태였음.

## 실제 원인

### legacy overlay hotkey 재등록

JunhyunHelper product hook이 zoom/floor를 소유하도록 했지만, transplanted `OverlayMiniMapService`가 MiniMap 초기화 및 settings 변경 때 `SyncHotkeys()`를 호출하여 old `GlobalKeyboardHookService`의 zoom/floor key를 다시 채우고 있었다.

따라서 MiniMap이 켜진 뒤에는 product hook과 legacy direct overlay hook이 같은 physical key를 동시에 처리할 수 있었다.

### MiniMap action dispatch 경로

PR #69에서는 MiniMap zoom/floor 전달을 `JunhyunMiniMapProductRegistry` weak reference에 의존했다. 첫 show/recreate lifecycle에서 registry 상태에 의존하지 않도록 실제 window owner인 `OverlayMiniMapService`를 authoritative dispatch 경로로 사용하도록 변경했다.

### resize grip 적용 시점

기존 no-resize 정책은 SourceInitialized 시점에만 적용했다. visible window의 Loaded 시점에도 다시 적용하여 WPF native resize와 transplanted custom bottom-right `Path`를 모두 제거한다.

### 중첩 ZIP

CI는 실제 배포 파일 `JunhyunHelper-win-x64.zip`을 artifact로 업로드한다. GitHub Actions artifact download 자체가 다시 ZIP wrapper를 생성하므로 connector에서 받은 artifact는 다음 구조였다.

```text
artifact wrapper.zip
└─ JunhyunHelper-win-x64.zip
```

사용자 전달 시에는 wrapper를 제거하고 내부 실제 배포 ZIP만 제공해야 한다.

## PR #70 구현

- `JunhyunMapHotkeyService`가 overlay visibility/settings lifecycle마다 legacy zoom/floor direct hook을 0으로 재억제
- product action 실행 직전에도 legacy direct hook을 다시 억제
- zoom/floor MiniMap action은 `OverlayMiniMapService`로 직접 전달
- 게임 foreground 허용 정책 유지
  - `EscapeFromTarkov`
  - `EscapeFromTarkov_BE`
  - `JunhyunHelper`
  - `TarkovHelper`
- MiniMap no-resize 정책을 SourceInitialized + Loaded에서 적용
- visible `MapContainer`의 legacy resize `Path` 제거

## 강화된 자동 검증

PR #70부터 Startup + Map smoke는 실제 MiniMap을 표시한 뒤 다음을 검사한다.

- MiniMap `ResizeMode == NoResize`
- `MapContainer`에 legacy resize `Path`가 없음
- MiniMap 표시 후 legacy hook의 ZoomIn/ZoomOut/FloorUp/FloorDown key가 모두 0
- MiniMap `ZoomLevel`이 실제 `ZoomIn/ZoomOut` 실행으로 변경됨
- Customs MiniMap floor indicator가 실제 `MoveFloorUp/Down` 실행으로 변경됨
- MiniMap zoom/floor action이 `SettingsChanged`를 발생시킨 뒤에도 legacy hook이 다시 활성화되지 않음
- 기존 Main Map floor selector 실제 SVG 교체 검증 유지
- 정상 Main Window close 후 process exit 검증 유지

## 검증 결과

```text
PR: #70 Fix MiniMap hotkey conflicts and verify live overlay controls
merge: 820efe166f54e40985f2aa4f6d0b8748bb5ce00a
final head: a83d105acf085f60d75207f05390bff2c2971ce0
CI: 31351174477
artifact: 9049033662
artifact wrapper digest: sha256:dcf1c3eb015dc56c7e102b5efba8a751bf451ee72d9d9ab9d5b98254afab8347
```

CI 결과:

- Desktop Release build: success
- automated tests: success
- Windows x64 self-contained publish: success
- enhanced Startup + Main Map + live MiniMap smoke: success
- MiniMap zoom/floor runtime assertions: success
- MiniMap no-resize / grip removal assertions: success
- graceful Main Window close + process exit: success
- ZIP creation/upload: success

## 사용자 전달 패키지 정책

GitHub artifact wrapper를 그대로 사용자에게 전달하지 않는다.

```text
GitHub artifact wrapper
→ 내부 JunhyunHelper-win-x64.zip 추출
→ 내부에 추가 ZIP이 없는지 확인
→ JunhyunHelper.exe 존재 확인
→ 실제 배포 ZIP만 사용자에게 전달
```
