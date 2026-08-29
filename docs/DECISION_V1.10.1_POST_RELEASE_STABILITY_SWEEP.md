# v1.10.1 Post-release Stability Sweep

Status: **MAINTENANCE VERIFIED / TEST + PUBLISHED-EXE CONTRACT**  
Date: **2026-08-29 KST**  
Initial audit base: `8afc3326df2d65657caf5211a932082fe3b60d3f`  
Latest non-documentation maintenance head: `22701e5419bca2995d442599fad646abcd484007`  
Public product source/tag target remains: `c444a1e26793e15c075875159f6605d8a99cf7f9`

## Purpose

v1.10.1 공개 릴리즈와 immutable release evidence 정리가 끝난 뒤, 새 기능이나 동작 변경 없이 장기 안정성 관점의 2차 점검을 수행했다.

이번 점검의 원칙은 다음과 같다.

- 실제 결함 증거가 없는 제품 코드는 정리 목적만으로 변경하지 않는다.
- Scanner recognition acceptance, Game Content fail-closed/LKG, Map/MiniMap semantics는 건드리지 않는다.
- 이미 정상인 lifecycle/disposal 경로는 그대로 유지하되, 중요한 소유권이 테스트에서 빠져 있다면 회귀 계약으로 고정한다.
- 정적 소스 분석만으로 장기 WPF 종료 안정성을 단정하지 않고, 가능한 경우 actual published EXE에서 정상 close 경계를 직접 검증한다.
- test/workflow/docs-only maintenance는 기존 v1.10.1 공개 바이너리를 다시 태그하거나 교체하지 않는다.

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

## Improvement 1 — deterministic disposal ownership contract

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

검증:

```text
PR #220 CI: 33254932421 — SUCCESS
exact-main CI: 33255074971 — SUCCESS
Release immutable verification: 33255208324 — SUCCESS
current deterministic suite after this contract: 440 passed / 0 failed / 0 skipped
```

## Improvement 2 — active-async Main Window close published-EXE gate

정적 ownership contract가 현재 소스의 의도를 고정하더라도, 사용자가 실제로 **비동기 작업 도중 창을 닫는 경우**까지 자동으로 증명하지는 않는다.

이 경계를 제품 코드 변경 없이 검증하기 위해 `.github/workflows/shutdown-race-ci.yml`을 추가했다.

검증은 actual Windows x64 Release publish EXE와 기존 `JUNHYUNHELPER_MAP_SMOKE=1` 경로를 사용한다. Map smoke의 `MapSmoke_PageLoaded`는 여러 rendered Map/Factory/MiniMap/Product UI 검사를 `await`하는 async WPF 경로이므로, full success marker 생성 전 close를 보내면 실제 async lifecycle이 진행 중인 상태의 정상 종료를 재현할 수 있다.

계약:

1. Release publish EXE를 실행한다.
2. Main Window handle이 실제 생성될 때까지 기다린다.
3. async product smoke가 진입할 시간을 주되 full smoke success marker가 아직 없는 것을 확인한다.
4. 정상 `Process.CloseMainWindow()` 경로로 close를 요청한다.
5. 7초 이내 프로세스 종료를 요구한다.
6. exit code `0`을 요구한다.
7. Map smoke diagnostic과 application startup/dispatcher diagnostic이 없어야 한다.

PR 검증:

```text
PR #221 standard CI: 33255650930 — SUCCESS
PR #221 Shutdown Race CI: 33255651032 — SUCCESS
```

exact-main 검증:

```text
maintenance head:
22701e5419bca2995d442599fad646abcd484007
standard CI: 33258220788 — SUCCESS
Shutdown Race CI: 33258220786 — SUCCESS
Release immutable verification: 33258352426 — SUCCESS
```

실제 dedicated runtime gate는 full async smoke가 아직 완료되지 않은 상태에서 Main Window close를 요청했고, 프로세스가 정상 종료 코드 `0`으로 종료되며 smoke diagnostic을 만들지 않는 것을 확인했다.

따라서 현재 증거는 **별도의 global MainWindow lifetime CTS나 모든 async operation으로의 cancellation token 전파를 추가할 필요가 없음을 지지한다.** 그런 구조 변경은 실제 결함이 확인될 때 영향 범위를 분석한 뒤 도입한다.

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

## Release identity and final readback

이번 작업은 **tests + workflow + docs only maintenance**다.

따라서:

- Desktop target version은 `1.10.1` 그대로다.
- `v1.10.1` tag는 계속 `c444a1e26793e15c075875159f6605d8a99cf7f9`를 가리킨다.
- 기존 `Junhyun-Helper.zip`을 재생성·교체하지 않는다.
- main의 이후 test/workflow/docs commit은 v1.10.1 product release source가 아니다.

`33258352426` 성공 뒤 public readback 결과:

```text
latest stable: v1.10.1
release id: 378982127
target/tag commit: c444a1e26793e15c075875159f6605d8a99cf7f9
Junhyun-Helper.zip asset id: 535210900
bytes: 80,540,164
SHA-256: c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
SHA256SUMS.txt asset id: 535210901
SHA-256: d32a6d50b60b512fa446d708d5d8ba75addad854c1e63c51378b318fbd6116c3
```

즉 post-release maintenance CI가 기존 stable release를 변경하지 않았음이 확인됐다.

실제 사용자 PC/Tarkov 환경에서의 v1.10.1 실사용 검증은 별도 사용자 evidence가 들어오기 전까지 pending 상태를 유지한다.
