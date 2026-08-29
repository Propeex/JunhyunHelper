# v1.10.1 Post-release Stability Sweep

Status: **MAINTENANCE VERIFIED / TEST-CONTRACT ONLY**  
Date: **2026-08-29 KST**  
Audit base: `8afc3326df2d65657caf5211a932082fe3b60d3f`  
Public product source/tag target remains: `c444a1e26793e15c075875159f6605d8a99cf7f9`

## Purpose

v1.10.1 공개 릴리즈와 immutable release evidence 정리가 끝난 뒤, 새 기능이나 동작 변경 없이 장기 안정성 관점의 2차 점검을 수행했다.

이번 점검의 원칙은 다음과 같다.

- 실제 결함 증거가 없는 제품 코드는 정리 목적만으로 변경하지 않는다.
- Scanner recognition acceptance, Game Content fail-closed/LKG, Map/MiniMap semantics는 건드리지 않는다.
- 이미 정상인 lifecycle/disposal 경로는 그대로 유지하되, 중요한 소유권이 테스트에서 빠져 있다면 회귀 계약으로 고정한다.
- test/docs-only maintenance는 기존 v1.10.1 공개 바이너리를 다시 태그하거나 교체하지 않는다.

## Findings

### 1. Open maintenance backlog

점검 시점의 GitHub open issue 목록에는 미해결 제품 이슈가 없었다. 공식 `CURRENT_STATE.md`도 v1.10.1 release batch에 남은 제품 개발 작업이 없음을 유지한다.

### 2. Program Update startup lifetime

`App.OnStartup`은 프로그램 업데이트 검사를 fire-and-forget으로 시작하지만 `ProgramUpdateCoordinator.CheckAtStartupAsync` 자체가 다음 경계를 소유한다.

- latest-release 조회 실패를 내부에서 기록하고 정상 복귀
- release 없음 또는 owner window가 이미 보이지 않으면 UI 미표시
- 준비/다운로드/updater launch 실패를 내부에서 정리하고 기존 버전을 유지
- App exit에서 coordinator/client dispose

따라서 현재 증거만으로 별도의 cancellation/lifetime 구조 변경을 정당화할 결함은 확인되지 않았다. 현재 동작을 유지한다.

### 3. MainWindow / DesktopServices ownership

현재 실제 종료 경로는 다음과 같다.

```text
MainWindow close
→ MainWindow.OnClosed cleanup
→ base.OnClosed
→ XAML Closed="Window_Closed"
→ DesktopServices.Dispose
→ ScannerCoordinator.Dispose
→ shared HttpClient.Dispose
→ application shutdown
```

이 경로는 explicit XAML event wiring이며 static class-level handler 같은 incidental global lifecycle coupling이 아니다. 따라서 단지 구조를 한 파일로 합치기 위한 리팩터링은 수행하지 않는다.

### 4. Scanner long-lived resources

`ScannerCoordinator.Dispose`는 현재 다음 장기 자원을 정리한다.

- context monitor cancellation/lifetime
- global hotkey service subscription/service
- runtime status subscription/runtime
- OCR disposable boundary
- Mini Scanner overlay
- Scanner catalog

context monitor는 cancellation을 정상 종료로 처리하고, dispose 이후 오류를 제품 상태로 재발행하지 않는다. 현재 수정 증거는 없다.

### 5. Scanner diagnostic retention

`ScannerDiagnosticRetentionService`는 timer를 idempotent하게 dispose한다. 백그라운드 정리 작업은 reviewed/corrupt/unknown ownership 데이터를 자동 삭제하지 않고 fail-closed를 유지한다. 현재 정책 또는 런타임 변경은 하지 않는다.

## Improvement made

런타임 결함 대신 **regression coverage gap**을 확인했다.

기존 `DesktopStartupWiringContractTests`는 page/header initialization ownership은 고정했지만, 제품 종료 시 장기 자원이 실제 ownership chain을 따라 dispose되는지는 고정하지 않았다.

따라서 `ProductLifetime_DisposesOwnedLongLivedServices` 계약 테스트를 추가해 다음을 보호한다.

- MainWindow `Closed` wiring과 `_services.Dispose()`
- `base.OnClosed(e)` 유지
- `DesktopServices`의 Scanner/shared HttpClient dispose
- `ScannerCoordinator`의 monitor/hotkey/runtime/OCR/overlay/catalog cleanup
- `App.OnExit`의 Program Update / Scanner diagnostic retention cleanup
- retention timer disposal

이 변경은 사용자 기능, 실행 의미, 네트워크 정책, 데이터 schema, Scanner recognition, Map/MiniMap 동작을 변경하지 않는다.

## Non-changes

다음은 의도적으로 그대로 유지한다.

- Scanner OCR threshold / matcher / candidate cap / visual recovery acceptance
- Scanner canonical Item ID identity policy
- Scanner capture geometry / reviewed Ground Truth ownership
- Game Content LKG / relationship completeness / fail-closed
- Program Update stable checksum / user consent / mutable-data preservation
- user.db / Content schema compatibility
- Ammo behavior
- Map / Factory / MiniMap semantics and donor pin
- v1.10.1 public tag/release/assets

## Release identity

이번 작업은 **tests + docs only maintenance**다.

따라서:

- Desktop target version은 `1.10.1` 그대로다.
- `v1.10.1` tag는 계속 `c444a1e26793e15c075875159f6605d8a99cf7f9`를 가리킨다.
- 기존 `Junhyun-Helper.zip`을 재생성·교체하지 않는다.
- main의 이후 test/docs commit은 v1.10.1 product release source가 아니다.

실제 사용자 PC/Tarkov 환경에서의 v1.10.1 실사용 검증은 별도 사용자 evidence가 들어오기 전까지 pending 상태를 유지한다.
