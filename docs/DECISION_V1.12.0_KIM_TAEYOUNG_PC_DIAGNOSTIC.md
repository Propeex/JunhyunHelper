# Decision — 김태영 PC 진단

날짜: 2026-08-31  
상태: **CONFIRMED / v1.12.1 PUBLIC VERIFIED**

## 사용자 문제

사용자가 함께 Tarkov를 플레이하는 김태영의 PC에서는 본인이 직접 보는 게임 화면은 정상인데 Discord 화면 송출/스크린샷 계열 결과가 비정상적으로 밝아 내용을 보기 어려운 현상이 있다. 준현 헬퍼 Scanner도 이 환경에서 정상적으로 작동하지 않을 가능성이 있다.

사용자는 자신의 노트북과 데스크탑 두 환경에서 Scanner를 검증했으며, 먼저 Scanner 일반 알고리즘을 해당 한 PC에 맞춰 바꾸기보다 **김태영 PC의 display/capture 환경을 진단해서 원인을 판단**하기를 원한다.

김태영은 PC 설정을 직접 조사하기 어려우므로 진단은 사용자 입력을 최소화하고 자동 수집해야 한다.

## 사용자 확정 Flow

v1.12.1부터 정상 성공 경로는 다음으로 고정한다.

```text
메인 헤더 좌측 프로필 이미지 클릭
→ “혹시 김태영 본인?”
→ 예
→ 별도 indeterminate progress bar 표시
→ 로컬 PC/Scanner/capture 진단
→ Desktop ZIP 생성
→ “진단 완료.\n파일을 hyune4784@naver.com 으로 보내주세요.”
→ 기본 브라우저에서 https://mail.naver.com/v2/new 열기
→ 종료
```

- `아니오`는 아무 작업도 하지 않는다.
- 정상 시작 확인창에는 사용자 지정 문구 외의 설명을 추가하지 않는다.
- 정상 완료창에도 사용자 지정 두 문장 외의 파일명/경로/설명을 추가하지 않는다.
- ZIP은 자동 업로드하지 않는다.
- 네이버 웹메일에 파일을 자동 첨부하거나 이메일을 자동 발송하지 않는다.
- 기본 브라우저의 네이버 메일 쓰기 페이지만 연다. 웹메일 DOM/UI를 자동 조작하지 않는다.
- 사용자는 생성된 ZIP을 직접 첨부하고 발송한다.

## 진단 목표

진단 ZIP만 보고 다음을 구분할 수 있어야 한다.

1. Windows/HDR/GPU/driver/display/color/capture 환경 자체 문제
2. Discord/OBS/GPU overlay 등 capture 경로와의 상호작용
3. Tarkov window/capture 방식 특이점
4. JunhyunHelper Scanner capture/OCR/runtime 문제
5. 증거 부족으로 추가 진단이 필요한 경우

Scanner를 바꾸기 위한 근거가 아니라 **PC 환경 문제인지 Scanner 호환성 문제인지 먼저 분리하는 evidence bundle**이다.

## 수집 범위

Scanner/capture 결과에 영향을 줄 가능성이 있는 정보를 가능한 한 폭넓게 남긴다.

### System / display

- Windows version/build/architecture
- .NET/process architecture
- logical processor count
- display count
- 각 display bounds / working area / primary / bits-per-pixel
- virtual screen bounds
- system DPI
- remote-session 여부
- monitor model/status/resolution

### GPU / HDR / color

- GPU model/manufacturer
- driver version/date/model/status
- current resolution/refresh/bpp
- dxdiag의 HDR Support
- Display Color Space
- Color Primaries
- Display Luminance
- monitor/native mode/output type
- DirectX/graphics capability 관련 진단 필드

### Capture interaction candidates

전체 process inventory를 덤프하지 않고 Scanner/capture에 실제로 영향을 줄 수 있는 allowlist만 확인한다.

예:

- Discord
- OBS
- NVIDIA Share / Overlay / container
- AMD Radeon software / capture service
- RTSS / MSI Afterburner
- Xbox/Game Bar
- SteelSeries capture/overlay
- Medal
- Overwolf
- Lossless Scaling
- EscapeFromTarkov

존재 여부와 가능한 범위의 version만 기록하고 설치 경로는 기록하지 않는다.

### Scanner

- Scanner display settings snapshot
- runtime status / active capture mode
- catalog count/mode/timestamp
- 기존 Scanner support bundle
- Scanner performance/log evidence

### Visual evidence

- 각 Windows display screen copy
- Tarkov window가 있으면 exact client screen-copy
- 같은 Tarkov client에 대한 PrintWindow 결과
- 각 이미지의 dimensions
- mean RGB
- mean/min/max luminance
- highlight clipping 비율
- near-black 비율

이 비교는 “사용자가 보는 화면은 정상인데 capture만 과도하게 밝음” 증상을 capture path별로 분리하기 위한 핵심 evidence다.

## Privacy / security boundary

진단 목적과 무관한 식별/secret 정보는 수집하지 않는다.

명시적 제외:

- Windows 사용자 이름
- 컴퓨터 이름
- IP 주소
- MAC 주소
- 네트워크 목록
- 환경변수 전체 dump
- token / password / credential
- 임의의 전체 process 목록
- application install path

단, **화면 캡처 자체에는 진단 시 실제 화면에 보이는 내용이 포함될 수 있다.** 이 사실은 ZIP README에 유지한다.

ZIP은 로컬 Desktop에만 생성한다.

## Failure contract

한 probe가 실패했다고 전체 진단을 버리지 않는다.

- 각 optional probe는 fail-soft
- 실패한 probe 이름/예외 종류/비민감 메시지는 `probe-errors.txt`에 기록
- 핵심 ZIP 작성 자체가 실패할 때만 진단 전체 실패로 처리
- partial evidence도 가능한 한 보존
- 정상 성공 UX의 고정 문구 계약을 깨지 않도록 browser compose launch 실패는 내부 diagnostic log에만 기록한다.

## 2026-08-31 사용자 노트북 smoke evidence

사용자가 v1.12.0에서 생성한 실제 진단 ZIP을 검토했다.

- ZIP CRC 정상
- expected top-level evidence 11개 생성
- `probe-errors.txt = none`
- display capture / luminance stats 정상
- nested Scanner support bundle 정상
- Scanner/catalog snapshot 정상
- 실행 당시 Tarkov가 없어 `EscapeFromTarkov window not found.`가 기록됨
- allowlist 대상 관련 프로세스가 실행 중이지 않아 관련 프로세스 목록이 비어 있었음

따라서 exporter 자체는 실제 사용자 노트북에서 정상 동작했다. 이 샘플은 김태영 실제 PC의 밝기 문제 원인 증거는 아니다.

## v1.12.1 public verification

```text
exact product source/tag target:
07a808f187e59f1b2b4b62ca6a947ccbed9baeaa
PR: #239 — MERGED
PR exact-head CI: 33350561623 — SUCCESS
PR exact-head Shutdown Race: 33350561588 — SUCCESS
PR exact-head Documentation Consistency: 33350561628 — SUCCESS
exact-main CI: 33350742745 — SUCCESS
exact-main Shutdown Race: 33350742733 — SUCCESS
exact-main Documentation Consistency: 33350742720 — SUCCESS
Release workflow: 33350893047 — SUCCESS
release id: 379473487
483 passed / 0 failed / 0 skipped
```

GitHub latest release, `v1.12.1` tag, release target, exact product source가 일치하며 공개 ZIP/checksum asset의 digest도 release workflow 검증값과 일치한다.

## 구현 authority

- `src/JunhyunHelper.Desktop/MainWindow.xaml`
- `src/JunhyunHelper.Desktop/MainWindow.KimTaeyoungDiagnostic.cs`
- `src/JunhyunHelper.Desktop/Scanner/KimTaeyoungPcDiagnosticExporter.cs`
- `src/JunhyunHelper.Desktop/Scanner/ScannerSupportBundleExporter.cs`
- `tests/JunhyunHelper.Tests/Maintenance/V120QuestDiagnosticsUiContractTests.cs`

## 향후 판단

김태영의 실제 ZIP을 받은 뒤 결과를 분석한다.

- 정상적인 사용자 환경에서 재현 가능한 capture 차이라면 Scanner compatibility 개선 후보로 취급한다.
- 해당 PC의 비정상 설정/driver/HDR/capture 환경 문제라면 Scanner 전체 동작을 왜곡하지 않고 PC 수정 방법을 사용자에게 안내한다.
- 한 사람의 샘플만으로 Scanner global threshold/normalization을 완화하지 않는다.
