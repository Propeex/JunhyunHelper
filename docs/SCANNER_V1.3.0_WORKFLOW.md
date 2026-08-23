# Scanner v1.3.0 — 분석 이미지 / one-shot / 전역 단축키 계약

기준일: 2026-08-23
상태: **CONFIRMED / IMPLEMENTED / RELEASE CANDIDATE**

## 사용자 요구

Scanner 실사용 검증과 문제 분석을 빠르게 반복할 수 있도록 다음 사용자 기능을 제공한다.

1. 실제 recognition에 사용된 최신 원본 캡처를 PNG로 저장할 수 있다.
2. 지속 테스트 모드를 켜지 않고 모든 연결 디스플레이를 한 번만 검사하는 1회 테스트 스캔을 제공한다.
3. 1회 인게임 스캔, 1회 테스트 스캔, Scanner ON/OFF를 각각 전역 단축키로 실행한다.
4. 세 단축키는 Scanner 탭의 하나의 설정 창에서 각각 변경/비활성화할 수 있다.
5. 1회 인게임/테스트 스캔은 Scanner 탭 버튼을 제공하지 않고 단축키로 실행한다.

## 확정 동작

### 인식 이미지 저장

- `인식 이미지` 창은 최신 diagnostic frame을 계속 메모리에서 표시한다.
- 사용자가 `이미지 저장`을 선택했을 때만 PNG를 디스크에 기록한다.
- 저장 대상은 실제 detector에 전달된 원본 frame이며 diagnostic rectangle/text overlay를 합성하지 않는다.
- 자동 screenshot 저장 기능은 추가하지 않는다.
- `로그 삭제`는 메모리 frame과 Scanner 로그를 지우지만 사용자가 명시적으로 export한 PNG는 삭제하지 않는다.

이는 v1.2.x의 "raw screenshot을 자동으로 디스크에 남기지 않는다"는 개인정보/진단 원칙을 유지하면서, **사용자가 명시적으로 선택한 분석용 export만 허용**하는 확장이다.

### 1회 스캔

- 인게임 one-shot: `ScannerCaptureMode.TarkovWindow`.
- 테스트 one-shot: `ScannerCaptureMode.DisplayTest`.
- 둘 다 기존 Scanner Lab v3.8 detector → title ROI → ko-KR OCR/current-catalog visual recovery → conservative matcher → presentation pipeline을 그대로 사용한다.
- one-shot 때문에 continuous Scanner/Test 설정을 영구 변경하지 않는다.
- one-shot 전 continuous loop를 실제 종료까지 기다리고, 완료 후 최신 사용자 상태가 같은 mode를 여전히 요청할 때만 복구한다.
- one-shot은 scan-time network refresh를 시작하지 않는다.

### 전역 단축키

기본값:

```text
1회 인게임 스캔  Ctrl+Shift+F10
1회 테스트 스캔  Ctrl+Shift+F11
Scanner ON/OFF    Ctrl+Shift+F12
```

- 각 명령은 개별 global hotkey registration을 사용한다.
- Scanner 탭에 들어와 있지 않아도 MainWindow lifetime 동안 등록된다.
- 각 단축키는 변경 또는 비활성화할 수 있다.
- 하나의 gesture를 둘 이상의 Scanner 명령에 동시에 할당할 수 없다.
- hotkey callback은 중복 실행을 방지하고 기존 one-shot coordinator gate를 사용한다.

## 설정 호환성

Scanner display settings schema를 v3 → v4로 올린다.

- v3의 기존 `OneShotHotkey` 사용자 값은 v4 `OneShotTarkovHotkey`로 우선 보존한다.
- 사용자가 기존 one-shot을 비활성화했다면 그 상태도 보존한다.
- 기존 사용자 키가 새 F11/F12 기본값과 겹치면 기존 키를 바꾸지 않고 새 명령에만 비충돌 fallback을 배정한다.
- fresh install의 기본값은 F10/F11/F12를 유지한다.
- `user.db` migration 없음.
- Game Content schema 변경 없음.

## 변경하지 않는 Scanner 계약

- Windows `ko-KR` OCR primary.
- current official Korean full-item catalog가 identity 권위.
- detector/OCR/visual confidence threshold 변경 없음.
- top1/top2 ambiguity margin 변경 없음.
- ambiguous/low-confidence는 fail closed.
- 최고 상점가 = 유효한 non-flea RUB 판매가 최댓값.
- 플리마켓 평균가 = positive `avg24hPrice`.
- 필요한 개수 = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`.
- game memory read / DLL injection / packet interception 없음.
- scan-time HTTP 없음.

## 버전

새 사용자 기능을 포함하므로 `docs/VERSIONING.md` 정책에 따라 **v1.3.0 MINOR** 릴리즈다.
